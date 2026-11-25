using System;

namespace LogTech.BE
{
    // Un movimiento de plata (CU-005-025 y CU-005-026). Monto con signo: positivo entra, negativo sale.
    // AfectaSaldo = false para los pagos con tarjeta del dador y sus devoluciones (no pasan por la billetera).
    public sealed class TransaccionBE
    {
        public int IdTransaccion { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        public decimal Monto { get; set; }
        public bool AfectaSaldo { get; set; }
        public int? IdViaje { get; set; }
        public int? IdTarjeta { get; set; }
        public string Detalle { get; set; }
        public string Comprobante { get; set; }
    }

    // Resumen de la billetera (componente BilleteraResumen).
    public sealed class BilleteraBE
    {
        public decimal Saldo { get; set; }          // lo disponible para retirar
        public decimal EnEscrow { get; set; }       // dador: lo que pagó y está retenido; transportista: lo que va a cobrar
    }
}
