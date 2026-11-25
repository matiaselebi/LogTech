using System;

namespace LogTech.BE
{
    // Cuánto se cobra al momento al adelantar un cobro (WS_FactoringLogistico): el monto, la comisión del 4 % y lo que se recibe.
    public sealed class CotizacionFactoringBE
    {
        public decimal Monto { get; set; }
        public decimal Comision { get; set; }
        public decimal MontoAdelantado { get; set; }
    }

    // Pedido de adelanto del cobro de un viaje en efectivo (CU-006-029 y 030).
    public sealed class SolicitudFactoringBE
    {
        public int IdSolicitud { get; set; }
        public int IdViaje { get; set; }
        public int IdTransportista { get; set; }
        public DateTime Fecha { get; set; }
        public decimal MontoViaje { get; set; }        // lo que cobraría el transportista
        public decimal Comision { get; set; }          // 4 % para LogTech
        public decimal MontoAdelantado { get; set; }   // lo que recibe al momento
        public int Puntaje { get; set; }
        public string Estado { get; set; }             // Aprobada o Rechazada
        public string Motivo { get; set; }
        public string Recorrido { get; set; }
    }

    // Scoring del factoring (0 a 100): reputación hasta 60, viajes entregados hasta 30 y sin disputas perdidas 10.
    public sealed class PuntajeFactoringBE
    {
        public int Reputacion { get; set; }
        public int Viajes { get; set; }
        public int SinDisputas { get; set; }
        public int Total { get { return Reputacion + Viajes + SinDisputas; } }
    }
}
