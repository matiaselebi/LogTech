using LogTech.BE;

namespace LogTech.BLL
{
    // Observador: le avisa al dador de carga cómo avanza su viaje. Si el que cambió el
    // estado fue el propio dador (cancelar la oferta), no hace falta avisarle.
    public sealed class AvisoDadorObservador : IObservadorViaje
    {
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        public void Actualizar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario)
        {
            if (!viaje.IdDador.HasValue || viaje.Estado == "Cancelado") return;
            notificaciones.Avisar(viaje.IdDador.Value,
                "Viaje N° " + viaje.IdViaje + " (" + viaje.Origen + " → " + viaje.Destino + "): " + novedad);
        }
    }
}
