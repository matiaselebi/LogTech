using System;
using System.Collections.Generic;
using System.IO;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Documentación legal (CU-003-016 a CU-003-021): la de la cuenta, la de cada vehículo y la de cada conductor de la flota.
    public sealed class DocumentoBLL
    {
        private const int DiasDeAviso = 30;

        // Lo que tiene que tener aprobado cada vehículo y cada conductor para poder hacer viajes.
        public static readonly string[] DelVehiculo = { "Seguro del vehículo", "VTV", "Cédula verde" };
        public static readonly string[] DelConductor = { "DNI del conductor", "Licencia profesional", "Antecedentes penales" };

        private readonly DocumentoDAL documentos = new DocumentoDAL();
        private readonly VehiculoDAL vehiculos = new VehiculoDAL();
        private readonly ConductorDAL conductores = new ConductorDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        // Qué documentos puede cargar cada perfil: primero los de la cuenta (el primero es el obligatorio para tomar
        // viajes, R3), después los de los vehículos y, la flota, los de sus conductores.
        public static string[] TiposDelRol(string rol)
        {
            if (rol == "Transportista")
                return new[] { "Licencia de conducir", "Constancia de CUIT", "Seguro del vehículo", "VTV", "Cédula verde" };
            if (rol == "Administrador de Flota")
                return new[] { "Habilitación CNRT", "Constancia de CUIT", "Seguro del vehículo", "VTV", "Cédula verde",
                    "DNI del conductor", "Licencia profesional", "Antecedentes penales" };
            return new[] { "Constancia de CUIT" };
        }

        // Los documentos de la cuenta que son obligatorios (R3): sin todos aprobados y vigentes no se toman viajes y el
        // semáforo de la cuenta queda en rojo (o amarillo mientras LogTech los revisa).
        public static string[] ObligatoriosDeLaCuenta(string rol)
        {
            if (rol == "Transportista") return new[] { "Licencia de conducir", "Constancia de CUIT" };
            if (rol == "Administrador de Flota") return new[] { "Habilitación CNRT", "Constancia de CUIT" };
            return new[] { "Constancia de CUIT" };
        }

        // CU-003-019 (propio legajo).
        public List<DocumentoBE> ListarDelUsuario(int idUsuario)
        {
            return documentos.ListarDelUsuario(idUsuario);
        }

        // CU-003-019 (analista): los de todos; estado vacío = todos.
        public List<DocumentoBE> ListarTodos(string estado)
        {
            return documentos.ListarTodos(estado);
        }

        // Semáforo de la cuenta (sin los documentos de los vehículos ni de los conductores, que tienen el suyo): "Rojo"
        // (vencido, rechazado o falta el obligatorio), "Amarillo" (pendiente de revisión o vence en menos de 30 días) o
        // "Verde". detalle explica por qué.
        public string ObtenerSemaforo(int idUsuario, string rol, out string detalle)
        {
            var deLaCuenta = documentos.ListarDelUsuario(idUsuario).FindAll(d => !d.IdVehiculo.HasValue && !d.IdConductor.HasValue);
            // Cada obligatorio, igual que en los vehículos y los conductores: aprobado y vigente, en revisión o falta.
            string motivo;
            var color = Semaforo(deLaCuenta, ObligatoriosDeLaCuenta(rol), out motivo);
            detalle = motivo ?? "Documentación al día.";
            if (color != "Verde") return color;
            // Todo aprobado: avisa en amarillo lo que vence en menos de 30 días.
            foreach (var documento in deLaCuenta)
            {
                if (documento.Estado == "Aprobado" && documento.FechaVencimiento <= DateTime.Today.AddDays(DiasDeAviso))
                {
                    detalle = documento.Tipo + ": vence el " + documento.FechaVencimiento.ToString("dd/MM/yyyy") + ".";
                    return "Amarillo";
                }
            }
            return "Verde";
        }

        // Semáforo de un vehículo o de un conductor con sus documentos. Verde: cada obligatorio tiene uno aprobado y
        // vigente. Amarillo: al que le falta, lo está revisando LogTech. Rojo: alguno falta, venció o fue rechazado; el
        // motivo dice cuál y por qué (es lo que ve el usuario al lado del semáforo).
        public static string Semaforo(List<DocumentoBE> suyos, string[] obligatorios, out string motivo)
        {
            motivo = null;
            var color = "Verde";
            foreach (var tipo in obligatorios)
            {
                DocumentoBE ultimo = null;
                var aprobado = false;
                foreach (var documento in suyos)
                {
                    if (documento.Tipo != tipo) continue;
                    if (documento.Estado == "Aprobado" && documento.FechaVencimiento >= DateTime.Today) aprobado = true;
                    if (ultimo == null || documento.FechaCarga > ultimo.FechaCarga) ultimo = documento;
                }
                if (aprobado) continue;
                if (ultimo != null && ultimo.Estado == "Pendiente")
                {
                    if (color == "Verde") motivo = tipo + ": en revisión.";
                    color = "Amarillo";
                    continue;
                }
                if (ultimo == null) motivo = "Falta cargar: " + tipo + ".";
                else if (ultimo.Estado == "Rechazado") motivo = tipo + ": rechazado por LogTech. " + ultimo.Motivo;
                else motivo = tipo + ": vencido el " + ultimo.FechaVencimiento.ToString("dd/MM/yyyy") + ".";
                return "Rojo";
            }
            return color;
        }

        // CU-003-016. Devuelve null si salió bien, o el texto del error.
        public string Alta(DocumentoBE documento, int idUsuario, string usuario)
        {
            documento.IdUsuario = idUsuario;
            var error = RevisarDatos(documento, idUsuario);
            if (error == null) error = RevisarRepetido(documento, idUsuario);
            if (error == null) error = RevisarVencimientoYArchivo(documento, true);
            if (error != null) return error;
            documento.Archivo = ArchivoServicio.Guardar(documento.Contenido, documento.NombreArchivo, "Documentos");
            documentos.Alta(documento);
            bitacora.Registrar(idUsuario, usuario, "Alta de documentación: " + documento.Tipo + De(documento), "Informativa");
            return null;
        }

        // CU-003-017. El archivo es opcional: si no se adjunta uno nuevo queda el anterior.
        public string Modificar(DocumentoBE documento, int idUsuario, string usuario)
        {
            var actual = documentos.Obtener(documento.IdDocumento);
            if (actual == null || actual.IdUsuario != idUsuario || actual.Estado == "Inactivo") return "El documento no existe.";
            documento.IdUsuario = idUsuario;
            documento.Tipo = actual.Tipo;
            var error = RevisarDatos(documento, idUsuario);
            if (error == null) error = RevisarVencimientoYArchivo(documento, false);
            if (error != null) return error;

            documento.Archivo = actual.Archivo;
            if (documento.Contenido != null && documento.Contenido.Length > 0)
                documento.Archivo = ArchivoServicio.Guardar(documento.Contenido, documento.NombreArchivo, "Documentos");
            documentos.Modificar(documento);
            // El archivo reemplazado ya no se usa. Si no se puede borrar, solo queda ocupando lugar.
            if (documento.Archivo != actual.Archivo)
            {
                try { File.Delete(ArchivoServicio.RutaFisica(actual.Archivo)); }
                catch (IOException) { }
            }
            bitacora.Registrar(idUsuario, usuario, "Modificación de documentación: " + documento.Tipo + De(actual), "Informativa");
            return null;
        }

        // CU-003-018. Baja lógica: queda "Inactivo" y sale del legajo.
        public string Baja(int idDocumento, int idUsuario, string usuario)
        {
            var actual = documentos.Obtener(idDocumento);
            if (actual == null || actual.IdUsuario != idUsuario || actual.Estado == "Inactivo") return "El documento no existe.";
            // Un documento aprobado y vigente no se da de baja: si cambió, se usa «Modificar» y LogTech lo vuelve a revisar.
            if (actual.Estado == "Aprobado" && actual.FechaVencimiento >= DateTime.Today)
                return "Un documento aprobado no se puede dar de baja. Si cambió, usá «Modificar»: LogTech lo vuelve a revisar.";
            // ERR008: no se saca un documento de un camión o de un conductor que está viajando.
            if (actual.IdVehiculo.HasValue)
            {
                var vehiculo = vehiculos.Obtener(actual.IdVehiculo.Value);
                if (vehiculo != null && vehiculo.EnViaje)
                    return "El documento es del vehículo " + vehiculo.Patente + ", que está haciendo un viaje: esperá a que termine.";
            }
            if (actual.IdConductor.HasValue)
            {
                var conductor = conductores.Obtener(actual.IdConductor.Value);
                if (conductor != null && conductor.EnViaje)
                    return "El documento es de " + conductor.NombreCompleto + ", que está haciendo un viaje: esperá a que termine.";
            }
            if (!documentos.CambiarEstado(idDocumento, actual.Estado, "Inactivo", actual.Motivo)) return "El documento cambió recién. Volvé a intentar.";
            bitacora.Registrar(idUsuario, usuario, "Baja de documentación: " + actual.Tipo + De(actual), "Informativa");
            return null;
        }

        // CU-003-020. Aprobar, o rechazar con un motivo (ERR012).
        public string Validar(int idDocumento, bool aprobar, string motivo, int idAnalista, string usuario)
        {
            var actual = documentos.Obtener(idDocumento);
            if (actual == null || actual.Estado != "Pendiente") return "El documento ya no está pendiente de revisión.";
            motivo = (motivo ?? string.Empty).Trim();
            if (!aprobar && (motivo.Length < 5 || motivo.Length > 250)) return "Escribí el motivo del rechazo (entre 5 y 250 caracteres).";

            if (!documentos.CambiarEstado(idDocumento, "Pendiente", aprobar ? "Aprobado" : "Rechazado", aprobar ? null : motivo))
                return "El documento ya no está pendiente de revisión.";
            notificaciones.Avisar(actual.IdUsuario, aprobar
                ? "Tu documento \"" + actual.Tipo + "\"" + De(actual) + " fue aprobado."
                : "Tu documento \"" + actual.Tipo + "\"" + De(actual) + " fue rechazado: " + motivo + ". Cargá uno nuevo desde Documentación.");
            bitacora.Registrar(idAnalista, usuario, (aprobar ? "Aprobación" : "Rechazo") + " de documentación: " + actual.Tipo + De(actual) + " de " + actual.Usuario,
                aprobar ? "Informativa" : "Advertencia");
            return null;
        }

        // CU-003-021 (R37): rutina diaria. Inhabilita lo vencido y avisa lo que vence en 30 días.
        public void ProcesarVencimientos()
        {
            var vencidos = documentos.MarcarVencidos(DateTime.Today);
            foreach (var documento in vencidos)
                notificaciones.Avisar(documento.IdUsuario, "Tu documento \"" + documento.Tipo + "\"" + De(documento) + " venció el "
                    + documento.FechaVencimiento.ToString("dd/MM/yyyy") + " y quedó inhabilitado. Cargá uno nuevo para seguir operando.");

            var porVencer = documentos.MarcarAvisosPorVencer(DateTime.Today);
            foreach (var documento in porVencer)
                notificaciones.Avisar(documento.IdUsuario, "Tu documento \"" + documento.Tipo + "\"" + De(documento) + " vence el "
                    + documento.FechaVencimiento.ToString("dd/MM/yyyy") + ". Renovalo antes para no quedar inhabilitado.");

            bitacora.Registrar(null, "sistema", "Rutina de vencimientos: " + porVencer.Count + " aviso(s) y "
                + vencidos.Count + " documento(s) vencido(s)", "Informativa");
        }

        // Para Archivo.ashx: el archivo, solo para su dueño o para el analista.
        public string RutaArchivo(int idDocumento, int idUsuario, bool esAnalista)
        {
            var documento = documentos.Obtener(idDocumento);
            if (documento == null || (documento.IdUsuario != idUsuario && !esAnalista)) return null;
            return documento.Archivo;
        }

        // Alta del vehículo y del conductor: cada documento obligatorio, una vez, con su vencimiento y su archivo.
        internal static string RevisarObligatorios(List<DocumentoBE> nuevos, string[] obligatorios)
        {
            foreach (var tipo in obligatorios)
            {
                DocumentoBE documento = null;
                foreach (var nuevo in nuevos)
                    if (nuevo.Tipo == tipo) documento = nuevo;
                if (documento == null) return "Falta cargar: " + tipo + ".";
                var error = RevisarVencimientoYArchivo(documento, true);
                if (error != null) return error;
            }
            return nuevos.Count == obligatorios.Length ? null : "Revisá los documentos: hay uno de más.";
        }

        // ERR001 y R9. Vencimiento de mañana a 10 años y archivo PDF o JPG de hasta 5 MB.
        internal static string RevisarVencimientoYArchivo(DocumentoBE documento, bool archivoObligatorio)
        {
            if (documento.FechaVencimiento.Date <= DateTime.Today) return documento.Tipo + ": la fecha de vencimiento tiene que ser posterior a hoy.";
            if (documento.FechaVencimiento.Date > DateTime.Today.AddYears(10)) return documento.Tipo + ": revisá la fecha de vencimiento, es de más de 10 años.";
            var tieneArchivo = documento.Contenido != null && documento.Contenido.Length > 0;
            if (!tieneArchivo) return archivoObligatorio ? documento.Tipo + ": adjuntá el archivo." : null;
            // R9 (ERR011).
            if (!ArchivoServicio.EsPdfOJpg(documento.NombreArchivo, documento.Contenido)) return documento.Tipo + ": el archivo tiene que ser PDF o JPG de hasta 5 MB.";
            return null;
        }

        // ERR001. Los de un vehículo van con uno de sus vehículos activos, los de un conductor con uno de sus conductores
        // activos y los de la cuenta, con ninguno de los dos.
        private string RevisarDatos(DocumentoBE documento, int idUsuario)
        {
            var cuenta = usuarios.ObtenerPorId(idUsuario);
            if (cuenta == null || Array.IndexOf(TiposDelRol(cuenta.Rol), documento.Tipo) < 0) return "Elegí el tipo de documento.";
            var delVehiculo = Array.IndexOf(DelVehiculo, documento.Tipo) >= 0;
            var delConductor = Array.IndexOf(DelConductor, documento.Tipo) >= 0;
            if (delVehiculo)
            {
                var vehiculo = documento.IdVehiculo.HasValue ? vehiculos.Obtener(documento.IdVehiculo.Value) : null;
                if (vehiculo == null || vehiculo.IdPropietario != idUsuario || vehiculo.Estado != "Activo")
                    return "Elegí de cuál de tus vehículos activos es " + documento.Tipo + ".";
            }
            else if (documento.IdVehiculo.HasValue) return documento.Tipo + " no es de un vehículo: dejá «No es de un vehículo».";
            if (delConductor)
            {
                var conductor = documento.IdConductor.HasValue ? conductores.Obtener(documento.IdConductor.Value) : null;
                if (conductor == null || conductor.IdFlota != idUsuario || conductor.Estado != "Activo")
                    return "Elegí de cuál de tus conductores activos es " + documento.Tipo + ".";
            }
            else if (documento.IdConductor.HasValue) return documento.Tipo + " no es de un conductor: dejá «No es de un conductor».";
            return null;
        }

        // Un solo documento de cada tipo por cuenta, vehículo o conductor: si ya hay uno pendiente de revisión o aprobado
        // y vigente, no se carga otro (se usa «Modificar»). Se puede volver a cargar si el anterior fue rechazado o venció.
        private string RevisarRepetido(DocumentoBE documento, int idUsuario)
        {
            foreach (var otro in documentos.ListarDelUsuario(idUsuario))
            {
                if (otro.Tipo != documento.Tipo || otro.IdVehiculo != documento.IdVehiculo || otro.IdConductor != documento.IdConductor) continue;
                if (otro.Estado == "Pendiente" || (otro.Estado == "Aprobado" && otro.FechaVencimiento >= DateTime.Today))
                    return "Ya cargaste " + documento.Tipo + De(otro) + " y está " + (otro.Estado == "Pendiente" ? "pendiente de revisión" : "aprobado")
                        + ". Para cambiarlo, usá «Modificar».";
            }
            return null;
        }

        // Para la bitácora y los avisos: « (AE012IJ)» o « (Díaz, Carlos)».
        private static string De(DocumentoBE documento)
        {
            var quien = documento.Patente ?? documento.Conductor;
            return quien == null ? string.Empty : " (" + quien + ")";
        }
    }
}
