using LogTech.BE;

namespace LogTech.BLL
{
    // Patrón Observer (sección 12.6.3 del análisis): ViajeBLL es el sujeto. Cada vez que un
    // viaje cambia de estado avisa a todos los observadores suscriptos, sin saber qué hace
    // cada uno. Para sumar una reacción nueva alcanza con otra clase que implemente esta interfaz.
    public interface IObservadorViaje
    {
        void Actualizar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario);
    }
}
