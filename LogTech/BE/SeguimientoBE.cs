using System;

namespace LogTech.BE
{
    // Lo que devuelve WS_TrackingViajes: estado y última ubicación informada de un viaje del dador.
    public sealed class SeguimientoBE
    {
        public int IdViaje { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Estado { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string UltimaNovedad { get; set; }
        public DateTime? FechaUltimaNovedad { get; set; }
        public string Aviso { get; set; }                 // por ejemplo, si el camión todavía no informó su ubicación
    }
}
