using System;

namespace LogTech.BE
{
    // Suscripción mensual de una cuenta (CU-008-040 a 043): Flota SaaS (por vehículo) o Premium transportista.
    // Se paga con una tarjeta guardada o con el saldo y se renueva sola cada mes hasta que se da de baja.
    public sealed class SuscripcionBE
    {
        public int IdSuscripcion { get; set; }
        public int IdUsuario { get; set; }
        public string Tipo { get; set; }             // Flota SaaS o Premium transportista
        public int? CantidadPatentes { get; set; }   // solo Flota SaaS: los vehículos pagados este mes
        public decimal Monto { get; set; }           // lo que se cobra por mes
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public int? IdTarjeta { get; set; }
        public string Tarjeta { get; set; }          // «Visa terminada en 3704»
        public DateTime Vence { get; set; }          // fin del mes pagado: ahí se renueva o termina
        public bool CancelaAlVencer { get; set; }

        public string Estado
        {
            get { return FechaBaja.HasValue ? "Terminada" : CancelaAlVencer ? "Pendiente de baja" : "Activa"; }
        }
    }
}
