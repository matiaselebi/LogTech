using System;

namespace LogTech.BE
{
    // Un punto de la línea de tiempo del viaje: un cambio de estado o una incidencia.
    public sealed class EventoViajeBE
    {
        public DateTime Fecha { get; set; }
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string Usuario { get; set; }
        public bool EsIncidencia { get; set; }
        public int? IdIncidencia { get; set; }   // para ver la foto de la incidencia
        public bool TieneFoto { get; set; }
    }
}
