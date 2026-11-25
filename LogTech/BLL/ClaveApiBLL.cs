using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Claves de API de los dadores (las genera el administrador en Usuarios) y la consulta que hacen con ellas sus sistemas (WS_TrackingViajes).
    // Una clave solo sirve para consultar el seguimiento de los viajes de ese dador: no da acceso a la cuenta.
    public sealed class ClaveApiBLL
    {
        private const string RolDador = "Dador de carga";

        private readonly ClaveApiDAL claves = new ClaveApiDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly ViajeBLL viajes = new ViajeBLL();

        public ClaveApiBE ObtenerActiva(int idUsuario)
        {
            return claves.ObtenerActiva(idUsuario);
        }

        // Devuelve la clave nueva (se muestra una sola vez) o null si el usuario no es dador de carga.
        // La clave anterior deja de funcionar.
        public string Generar(int idUsuario, string usuario)
        {
            var cuenta = usuarios.ObtenerPorId(idUsuario);
            if (cuenta == null || cuenta.Rol != RolDador) return null;
            var clave = SeguridadServicio.GenerarToken();
            claves.Guardar(idUsuario, Hash(clave), clave.Substring(clave.Length - 4));
            bitacora.Registrar(idUsuario, usuario, "Generación de una clave de API para " + cuenta.Usuario, "Advertencia");
            return clave;
        }

        public bool Revocar(int idUsuario, string usuario)
        {
            if (!claves.Revocar(idUsuario)) return false;
            bitacora.Registrar(idUsuario, usuario, "Revocación de la clave de API", "Advertencia");
            return true;
        }

        // WS_TrackingViajes (R46): la clave tiene que existir, estar activa y ser de una cuenta activa, y el viaje
        // tiene que ser de ese dador. Devuelve null y el error si algo de eso falla.
        public SeguimientoBE ConsultarSeguimiento(string clave, int idViaje, out string error)
        {
            error = null;
            var idUsuario = string.IsNullOrWhiteSpace(clave) ? null : claves.UsarClave(Hash(clave.Trim()));
            if (!idUsuario.HasValue)
            {
                error = "La clave de API no es válida o fue revocada.";
                return null;
            }
            var monitoreo = viajes.ConsultarMonitoreo(idViaje, idUsuario.Value);
            if (monitoreo == null || monitoreo.Viaje.IdDador != idUsuario.Value)
            {
                error = "El viaje no existe o no es de esta cuenta.";
                return null;
            }

            var seguimiento = new SeguimientoBE
            {
                IdViaje = monitoreo.Viaje.IdViaje,
                Origen = monitoreo.Viaje.Origen,
                Destino = monitoreo.Viaje.Destino,
                Estado = monitoreo.Viaje.Estado,
                Latitud = monitoreo.Latitud,
                Longitud = monitoreo.Longitud,
                Aviso = monitoreo.Aviso
            };
            // La línea de tiempo viene ordenada por fecha: la última novedad es la del final.
            if (monitoreo.Eventos.Count > 0)
            {
                var ultimo = monitoreo.Eventos[monitoreo.Eventos.Count - 1];
                seguimiento.UltimaNovedad = ultimo.Titulo;
                seguimiento.FechaUltimaNovedad = ultimo.Fecha;
            }
            var cuenta = usuarios.ObtenerPorId(idUsuario.Value);
            bitacora.Registrar(idUsuario, cuenta == null ? null : cuenta.Usuario,
                "Consulta de seguimiento del viaje N° " + idViaje + " por servicio web", "Informativa");
            return seguimiento;
        }

        private static string Hash(string clave)
        {
            return SeguridadServicio.CalcularHashSha256(clave, string.Empty);
        }
    }
}
