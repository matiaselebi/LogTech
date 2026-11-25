using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Vehículos de transportistas y flotas (CU-002-007 a CU-002-010). Cada vehículo se da de alta con su documentación
    // legal (seguro, VTV y cédula verde): hasta que LogTech la apruebe su semáforo está en amarillo y no puede hacer viajes.
    public sealed class VehiculoBLL
    {
        public static readonly string[] TiposAcoplado = { "Sin acoplado", "Acoplado", "Semirremolque" };
        private const decimal CapacidadMaxima = 60;

        private readonly VehiculoDAL vehiculos = new VehiculoDAL();
        private readonly DocumentoDAL documentos = new DocumentoDAL();
        private readonly ConductorDAL conductores = new ConductorDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        public List<VehiculoBE> Listar(int idPropietario, string texto, string estado)
        {
            var lista = vehiculos.Listar(idPropietario, (texto ?? string.Empty).Trim(), estado);
            PonerSemaforos(lista, documentos.ListarDelUsuario(idPropietario));
            return lista;
        }

        // null si no existe o no es del usuario.
        public VehiculoBE Obtener(int idVehiculo, int idPropietario)
        {
            var vehiculo = vehiculos.Obtener(idVehiculo);
            if (vehiculo == null || vehiculo.IdPropietario != idPropietario) return null;
            PonerSemaforos(new List<VehiculoBE> { vehiculo }, documentos.ListarDelUsuario(idPropietario));
            return vehiculo;
        }

        // Los que pueden hacer un viaje de ese peso: activos, con la documentación en verde, libres (o ya asignados a ese
        // viaje, idActual) y que soportan el peso. aviso: por qué no aparecen los demás.
        public List<VehiculoBE> ListarParaViaje(int idPropietario, decimal peso, int? idActual, out string aviso)
        {
            var libres = new List<VehiculoBE>();
            var motivos = new List<string>();
            foreach (var vehiculo in Listar(idPropietario, string.Empty, "Activo"))
            {
                if (vehiculo.Semaforo != "Verde") motivos.Add(vehiculo.Patente + " (" + vehiculo.MotivoSemaforo.TrimEnd('.') + ")");
                else if (vehiculo.EnViaje && vehiculo.IdVehiculo != idActual) motivos.Add(vehiculo.Patente + " está en otro viaje");
                else if (vehiculo.CapacidadToneladas < peso) motivos.Add(vehiculo.Patente + " carga hasta " + vehiculo.CapacidadToneladas.ToString("0.##") + " t");
                else libres.Add(vehiculo);
            }
            aviso = motivos.Count == 0 ? null : "No aparecen: " + string.Join("; ", motivos) + ".";
            return libres;
        }

        // El camión libre y habilitado más grande del usuario (0 si no tiene ninguno): el match de cargas busca hasta ese peso.
        public decimal CapacidadLibreMaxima(int idPropietario)
        {
            decimal maxima = 0;
            foreach (var vehiculo in Listar(idPropietario, string.Empty, "Activo"))
                if (vehiculo.Semaforo == "Verde" && !vehiculo.EnViaje && vehiculo.CapacidadToneladas > maxima) maxima = vehiculo.CapacidadToneladas;
            return maxima;
        }

        // Motor de asignación del dador (CU-001-006): solo los camiones con la documentación en verde.
        public List<VehiculoBE> ListarCompatibles(decimal peso)
        {
            var lista = vehiculos.ListarCompatibles(peso);
            PonerSemaforos(lista, documentos.ListarDeVehiculos());
            var habilitados = new List<VehiculoBE>();
            foreach (var vehiculo in lista)
                if (vehiculo.Semaforo == "Verde") habilitados.Add(vehiculo);
            return habilitados;
        }

        // CU-002-007: el vehículo con su documentación (y, la flota, el cobro de un vehículo más si hace falta). Devuelve
        // null si salió bien, o el texto del error.
        public string Alta(VehiculoBE vehiculo, List<DocumentoBE> documentosNuevos, int idUsuario, string usuario)
        {
            vehiculo.IdPropietario = idUsuario;
            var error = Validar(vehiculo);
            if (error != null) return error;
            // R5 (ERR007).
            if (vehiculos.PatenteEnUso(vehiculo.Patente, 0)) return "La patente " + vehiculo.Patente + " ya está registrada en la plataforma.";
            // Toda su documentación legal, cada una con su archivo.
            error = DocumentoBLL.RevisarObligatorios(documentosNuevos, DocumentoBLL.DelVehiculo);
            if (error != null) return error;

            foreach (var documento in documentosNuevos)
            {
                documento.IdUsuario = idUsuario;
                documento.Archivo = ArchivoServicio.Guardar(documento.Contenido, documento.NombreArchivo, "Documentos");
            }

            // Último control, porque ya cobra: un solo camión del transportista, o la Flota SaaS con el vehículo de más.
            // ponytail: si no se puede cargar, los archivos recién guardados quedan sin usar en App_Data.
            SuscripcionBE cobro;
            TransaccionBE pago;
            TarjetaBE tarjeta;
            var suscripciones = new SuscripcionBLL();
            error = suscripciones.PrepararAltaVehiculo(idUsuario, vehiculo.Patente, out cobro, out pago, out tarjeta);
            if (error != null) return error;
            try
            {
                vehiculos.Alta(vehiculo, documentosNuevos, cobro, pago);
            }
            catch (Exception)
            {
                // No se guardó nada: lo cobrado a la tarjeta se devuelve (con el saldo, el movimiento no llegó a guardarse).
                if (cobro != null) BilleteraBLL.Devolver(tarjeta, SuscripcionBLL.PrecioPorPatente);
                throw;
            }
            bitacora.Registrar(idUsuario, usuario, "Alta del vehículo " + vehiculo.Patente + " con su documentación", "Informativa");
            if (cobro != null) suscripciones.TerminarAltaVehiculo(cobro, vehiculo.Patente, usuario);
            return null;
        }

        // CU-002-008.
        public string Modificar(VehiculoBE vehiculo, int idUsuario, string usuario)
        {
            var actual = Obtener(vehiculo.IdVehiculo, idUsuario);
            if (actual == null || actual.Estado != "Activo") return "El vehículo no existe o está dado de baja.";
            vehiculo.IdPropietario = idUsuario;
            var error = Validar(vehiculo);
            if (error != null) return error;
            // R6 (ERR008): no se tocan los datos de un camión que está haciendo un viaje.
            if (actual.EnViaje) return "El vehículo " + actual.Patente + " tiene un viaje asignado o en curso: esperá a que termine para editarlo.";
            // R5 (ERR007), solo si cambió la patente.
            if (vehiculo.Patente != actual.Patente && vehiculos.PatenteEnUso(vehiculo.Patente, vehiculo.IdVehiculo))
                return "La patente " + vehiculo.Patente + " ya está registrada en la plataforma.";
            vehiculos.Modificar(vehiculo);
            bitacora.Registrar(idUsuario, usuario, "Modificación del vehículo " + vehiculo.Patente, "Informativa");
            return null;
        }

        // CU-002-009. Baja lógica: queda "Inactivo". En la Flota SaaS el mes ya pagado no se devuelve: la próxima
        // renovación cobra un vehículo menos.
        public string Baja(int idVehiculo, int idUsuario, string usuario)
        {
            var actual = Obtener(idVehiculo, idUsuario);
            if (actual == null || actual.Estado != "Activo") return "El vehículo no existe o ya está dado de baja.";
            // R6 (ERR008).
            if (actual.EnViaje) return "El vehículo " + actual.Patente + " está en un viaje: tiene que terminarlo antes de darlo de baja.";
            vehiculos.Inactivar(idVehiculo);
            bitacora.Registrar(idUsuario, usuario, "Baja del vehículo " + actual.Patente, "Informativa");
            return null;
        }

        // Flota: el conductor fijo del camión (idConductor null = ninguno). Un conductor puede ser el designado de un
        // solo camión.
        public string DesignarConductor(int idVehiculo, int? idConductor, int idFlota, string usuario)
        {
            var vehiculo = Obtener(idVehiculo, idFlota);
            if (vehiculo == null || vehiculo.Estado != "Activo") return "El vehículo no existe o está dado de baja.";
            if (idConductor.HasValue)
            {
                var conductor = conductores.Obtener(idConductor.Value);
                if (conductor == null || conductor.IdFlota != idFlota || conductor.Estado != "Activo") return "Elegí uno de tus conductores activos.";
                foreach (var otro in vehiculos.Listar(idFlota, string.Empty, "Activo"))
                    if (otro.IdConductorDesignado == idConductor && otro.IdVehiculo != idVehiculo)
                        return conductor.NombreCompleto + " ya es el conductor designado del " + otro.Patente + ".";
            }
            vehiculos.DesignarConductor(idVehiculo, idConductor);
            bitacora.Registrar(idFlota, usuario, idConductor.HasValue ? "Conductor designado para el vehículo " + vehiculo.Patente
                : "El vehículo " + vehiculo.Patente + " quedó sin conductor designado", "Informativa");
            return null;
        }

        private static void PonerSemaforos(List<VehiculoBE> lista, List<DocumentoBE> documentosDeVehiculos)
        {
            foreach (var vehiculo in lista)
            {
                var suyos = new List<DocumentoBE>();
                foreach (var documento in documentosDeVehiculos)
                    if (documento.IdVehiculo == vehiculo.IdVehiculo) suyos.Add(documento);
                string motivo;
                vehiculo.Semaforo = DocumentoBLL.Semaforo(suyos, DocumentoBLL.DelVehiculo, out motivo);
                vehiculo.MotivoSemaforo = motivo;
            }
        }

        // ERR001. Deja la patente en mayúsculas y sin espacios ni guiones.
        private static string Validar(VehiculoBE vehiculo)
        {
            vehiculo.Patente = (vehiculo.Patente ?? string.Empty).ToUpperInvariant().Replace(" ", string.Empty).Replace("-", string.Empty);
            vehiculo.Marca = (vehiculo.Marca ?? string.Empty).Trim();
            vehiculo.Modelo = (vehiculo.Modelo ?? string.Empty).Trim();
            if (!Validador.EsPatente(vehiculo.Patente)) return "La patente tiene que tener el formato AB123CD o ABC123.";
            if (vehiculo.Marca.Length < 2 || vehiculo.Marca.Length > 40) return "Ingresá la marca (entre 2 y 40 caracteres).";
            if (vehiculo.Modelo.Length < 1 || vehiculo.Modelo.Length > 40) return "Ingresá el modelo (hasta 40 caracteres).";
            if (vehiculo.CapacidadToneladas <= 0 || vehiculo.CapacidadToneladas > CapacidadMaxima)
                return "La capacidad tiene que ser mayor a 0 y de hasta " + CapacidadMaxima + " toneladas.";
            if (Array.IndexOf(TiposAcoplado, vehiculo.TipoAcoplado) < 0) return "Elegí el tipo de acoplado.";
            vehiculo.CapacidadToneladas = Math.Round(vehiculo.CapacidadToneladas, 2);
            return null;
        }
    }
}
