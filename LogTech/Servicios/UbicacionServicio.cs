using System;

namespace LogTech.Servicios
{
    // Cuentas con coordenadas (R4 y R18).
    public static class UbicacionServicio
    {
        public static bool EsValida(decimal? latitud, decimal? longitud)
        {
            return latitud.HasValue && longitud.HasValue && latitud.Value >= -90 && latitud.Value <= 90
                && longitud.Value >= -180 && longitud.Value <= 180;
        }

        // Distancia en línea recta entre dos puntos de la Tierra (fórmula de Haversine), en km.
        public static double DistanciaKm(decimal latitud1, decimal longitud1, decimal latitud2, decimal longitud2)
        {
            const double RadioTierraKm = 6371;
            var dLat = Radianes((double)(latitud2 - latitud1));
            var dLon = Radianes((double)(longitud2 - longitud1));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Radianes((double)latitud1)) * Math.Cos(Radianes((double)latitud2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return RadioTierraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double Radianes(double grados)
        {
            return grados * Math.PI / 180;
        }
    }
}
