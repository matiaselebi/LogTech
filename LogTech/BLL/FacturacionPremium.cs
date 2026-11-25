using System;
using LogTech.BE;

namespace LogTech.BLL
{
    // Estrategia de la membresía Premium del transportista: un precio fijo por mes.
    public sealed class FacturacionPremium : IEstrategiaFacturacion
    {
        private readonly SuscripcionBE suscripcion;
        private readonly DateTime desde;

        public FacturacionPremium(SuscripcionBE suscripcion, DateTime desde)
        {
            this.suscripcion = suscripcion;
            this.desde = desde;
        }

        public void Calcular(FacturaBE factura)
        {
            factura.Servicio = "Premium transportista";
            factura.IdSuscripcion = suscripcion.IdSuscripcion;
            factura.Cantidad = 1;
            factura.PrecioUnitario = SuscripcionBLL.PrecioPremium;
            factura.Detalle = "Membresía Premium (sin comisión por viaje y alertas de cargas), del " + desde.ToString("dd/MM/yyyy")
                + " al " + desde.AddMonths(1).ToString("dd/MM/yyyy");
        }
    }
}
