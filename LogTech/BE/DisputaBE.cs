using System;

namespace LogTech.BE
{
    // Reclamo del dador sobre un viaje pagado con tarjeta con la plata retenida (CU-005-027).
    public sealed class DisputaBE
    {
        public int IdDisputa { get; set; }
        public int IdViaje { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }       // Abierta, Devuelta al dador o Pagada al transportista
        public string Resolucion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string ResueltaPor { get; set; }
        // Datos del viaje para mostrar.
        public string Recorrido { get; set; }
        public string Dador { get; set; }
        public string Transportista { get; set; }
        public decimal Monto { get; set; }       // lo que pagó el dador
        public string EstadoViaje { get; set; }
    }
}
