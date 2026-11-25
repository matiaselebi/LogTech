using System;

namespace LogTech.BE
{
    // Un plan de una aseguradora (simulada) que se contrata por mes desde la aplicación (CU-006-031).
    public sealed class PlanSeguroBE
    {
        public int IdPlan { get; set; }
        public string Aseguradora { get; set; }
        public string Nombre { get; set; }
        public decimal CoberturaPorViaje { get; set; }   // valor máximo de la carga que cubre en cada viaje
        public int? ViajesPorMes { get; set; }           // null = sin límite
        public decimal Precio { get; set; }              // por mes
        public string Detalle { get; set; }

        public string TextoViajes { get { return ViajesPorMes.HasValue ? "Hasta " + ViajesPorMes + " viajes" : "Sin límite"; } }
    }

    // El seguro que contrató un usuario (una fila de Suscripciones de tipo «Protección de carga»), con los datos de su plan.
    public sealed class SeguroBE
    {
        public int IdSuscripcion { get; set; }
        public int IdUsuario { get; set; }
        public PlanSeguroBE Plan { get; set; }
        public decimal Monto { get; set; }               // lo que paga por mes
        public DateTime FechaAlta { get; set; }
        public DateTime Vence { get; set; }              // fin del mes pagado
        public bool CancelaAlVencer { get; set; }
        public DateTime? FechaBaja { get; set; }
        public string NumeroPoliza { get; set; }
        public int? IdTarjeta { get; set; }
        public string Tarjeta { get; set; }
        // R39: lo que se suma (o descuenta, si es negativo) en la próxima renovación por un cambio de plan (CU-006-032).
        public decimal AjustePendiente { get; set; }

        public string Estado
        {
            get { return FechaBaja.HasValue ? "Terminado" : CancelaAlVencer ? "Se cancela al vencer" : "Activo"; }
        }
    }
}
