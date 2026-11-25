using LogTech.DAL;

namespace LogTech.BLL
{
    // Calificación del dador al transportista (CU-007-037). El promedio se ve en los camiones compatibles (CU-001-006).
    public sealed class CalificacionBLL
    {
        private readonly CalificacionDAL calificaciones = new CalificacionDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        // CU-007-037. Devuelve null si salió bien, o el texto del error.
        public string Calificar(int idViaje, int puntaje, string resena, int idDador, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdDador != idDador) return "El viaje no existe.";
            if (viaje.Estado != "Entregado") return "Solo se pueden calificar viajes entregados.";
            // R19 (ERR021).
            if (viaje.Calificado) return "Este viaje ya fue calificado.";
            resena = (resena ?? string.Empty).Trim();
            if (puntaje < 1 || puntaje > 5) return "Elegí un puntaje de 1 a 5 estrellas.";
            if (resena.Length > 500) return "La reseña admite hasta 500 caracteres.";

            if (!calificaciones.Alta(idViaje, idDador, puntaje, resena.Length == 0 ? null : resena)) return "Este viaje ya fue calificado.";
            notificaciones.Avisar(viaje.IdTransportista.Value, "Recibiste " + puntaje + " estrella(s) por el viaje N° " + idViaje
                + " (" + viaje.Origen + " → " + viaje.Destino + ").");
            bitacora.Registrar(idDador, usuario, "Calificación del viaje N° " + idViaje + ": " + puntaje + " estrella(s)", "Informativa");
            return null;
        }
    }
}
