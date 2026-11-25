using LogTech.BE;
using LogTech.DAL;

namespace LogTech.BLL
{
    // Observador: anota cada cambio en la línea de tiempo del viaje (la que ve el monitoreo).
    public sealed class HistorialViajeObservador : IObservadorViaje
    {
        private readonly ViajeDAL viajes = new ViajeDAL();

        public void Actualizar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario)
        {
            viajes.RegistrarHistorial(viaje.IdViaje, viaje.Estado, novedad, latitud, longitud, usuario);
        }
    }
}
