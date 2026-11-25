using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Conductores de la flota (CU-002-011 a CU-002-014). En el análisis se llaman "transportistas internos".
    // Cada uno se da de alta con su documentación legal (DNI, licencia profesional y antecedentes penales): hasta que
    // LogTech la apruebe, su semáforo está en amarillo y no se le pueden asignar viajes.
    public sealed class ConductorBLL
    {
        private readonly ConductorDAL conductores = new ConductorDAL();
        private readonly DocumentoDAL documentos = new DocumentoDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        public List<ConductorBE> Listar(int idFlota, string texto, string estado)
        {
            var lista = conductores.Listar(idFlota, (texto ?? string.Empty).Trim(), estado);
            PonerSemaforos(lista, documentos.ListarDelUsuario(idFlota));
            return lista;
        }

        // null si no existe o no es de la flota.
        public ConductorBE Obtener(int idConductor, int idFlota)
        {
            var conductor = conductores.Obtener(idConductor);
            if (conductor == null || conductor.IdFlota != idFlota) return null;
            PonerSemaforos(new List<ConductorBE> { conductor }, documentos.ListarDelUsuario(idFlota));
            return conductor;
        }

        // Los que pueden hacer un viaje: activos, con la documentación en verde y libres (o ya asignados a ese viaje,
        // idActual). aviso: por qué no aparecen los demás.
        public List<ConductorBE> ListarParaViaje(int idFlota, int? idActual, out string aviso)
        {
            var libres = new List<ConductorBE>();
            var motivos = new List<string>();
            foreach (var conductor in Listar(idFlota, string.Empty, "Activo"))
            {
                if (conductor.Semaforo != "Verde") motivos.Add(conductor.NombreCompleto + " (" + conductor.MotivoSemaforo.TrimEnd('.') + ")");
                else if (conductor.EnViaje && conductor.IdConductor != idActual) motivos.Add(conductor.NombreCompleto + " está en otro viaje");
                else libres.Add(conductor);
            }
            aviso = motivos.Count == 0 ? null : "No aparecen: " + string.Join("; ", motivos) + ".";
            return libres;
        }

        // CU-002-011. Devuelve null si salió bien, o el texto del error.
        public string Alta(ConductorBE conductor, List<DocumentoBE> documentosNuevos, int idFlota, string usuario)
        {
            conductor.IdFlota = idFlota;
            var error = Validar(conductor);
            if (error != null) return error;
            // R7 (ERR009).
            if (conductores.DniEnUso(conductor.Dni, 0)) return "El DNI " + conductor.Dni + " ya está registrado en la plataforma.";
            // Toda su documentación legal, cada una con su archivo.
            error = DocumentoBLL.RevisarObligatorios(documentosNuevos, DocumentoBLL.DelConductor);
            if (error != null) return error;
            foreach (var documento in documentosNuevos)
            {
                documento.IdUsuario = idFlota;
                documento.Archivo = ArchivoServicio.Guardar(documento.Contenido, documento.NombreArchivo, "Documentos");
            }
            conductores.Alta(conductor, documentosNuevos);
            bitacora.Registrar(idFlota, usuario, "Alta del conductor " + conductor.NombreCompleto + " con su documentación", "Informativa");
            return null;
        }

        // CU-002-012.
        public string Modificar(ConductorBE conductor, int idFlota, string usuario)
        {
            var actual = Obtener(conductor.IdConductor, idFlota);
            if (actual == null || actual.Estado != "Activo") return "El conductor no existe o está dado de baja.";
            conductor.IdFlota = idFlota;
            var error = Validar(conductor);
            if (error != null) return error;
            // R8 (ERR010).
            if (actual.EnViaje) return actual.NombreCompleto + " tiene un viaje asignado o en curso: esperá a que termine para editarlo.";
            // R7 (ERR009), solo si cambió el DNI.
            if (conductor.Dni != actual.Dni && conductores.DniEnUso(conductor.Dni, conductor.IdConductor))
                return "El DNI " + conductor.Dni + " ya está registrado en la plataforma.";
            conductores.Modificar(conductor);
            bitacora.Registrar(idFlota, usuario, "Modificación del conductor " + conductor.NombreCompleto, "Informativa");
            return null;
        }

        // CU-002-013. Baja lógica: queda "Inactivo" (y deja de ser el conductor designado de su camión).
        public string Baja(int idConductor, int idFlota, string usuario)
        {
            var actual = Obtener(idConductor, idFlota);
            if (actual == null || actual.Estado != "Activo") return "El conductor no existe o ya está dado de baja.";
            // R8 (ERR010).
            if (actual.EnViaje) return actual.NombreCompleto + " está en un viaje: tiene que terminarlo antes de darlo de baja.";
            conductores.Inactivar(idConductor);
            bitacora.Registrar(idFlota, usuario, "Baja del conductor " + actual.NombreCompleto, "Informativa");
            return null;
        }

        private static void PonerSemaforos(List<ConductorBE> lista, List<DocumentoBE> deLaFlota)
        {
            foreach (var conductor in lista)
            {
                var suyos = new List<DocumentoBE>();
                foreach (var documento in deLaFlota)
                    if (documento.IdConductor == conductor.IdConductor) suyos.Add(documento);
                string motivo;
                conductor.Semaforo = DocumentoBLL.Semaforo(suyos, DocumentoBLL.DelConductor, out motivo);
                conductor.MotivoSemaforo = motivo;
            }
        }

        // ERR001. Deja el DNI solo con números.
        private static string Validar(ConductorBE conductor)
        {
            conductor.Nombre = (conductor.Nombre ?? string.Empty).Trim();
            conductor.Apellido = (conductor.Apellido ?? string.Empty).Trim();
            conductor.Dni = (conductor.Dni ?? string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            conductor.NumeroLicencia = (conductor.NumeroLicencia ?? string.Empty).Trim();
            conductor.Telefono = (conductor.Telefono ?? string.Empty).Trim();
            if (!Validador.EsNombre(conductor.Nombre, 60) || !Validador.EsNombre(conductor.Apellido, 60))
                return "El nombre y el apellido solo pueden tener letras y espacios.";
            if (!Validador.EsDni(conductor.Dni)) return "El DNI tiene que tener 7 u 8 números.";
            if (!Validador.EsLicencia(conductor.NumeroLicencia)) return "Ingresá un número de licencia válido (letras, números y guiones).";
            if (!Validador.EsTelefono(conductor.Telefono)) return "El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15).";
            return null;
        }
    }
}
