using System;
using LogTech.BE;

namespace LogTech.BLL
{
    // Estrategia de la Flota SaaS: un mes de abono por cada vehículo que se cobra. Al contratarla y al renovarla son los
    // vehículos activos; al sumar un vehículo, ese uno, hasta que vence el mes ya pagado.
    public sealed class FacturacionFlota : IEstrategiaFacturacion
    {
        private readonly SuscripcionBE suscripcion;
        private readonly int vehiculos;
        private readonly DateTime desde;
        private readonly DateTime hasta;

        public FacturacionFlota(SuscripcionBE suscripcion, int vehiculos, DateTime desde, DateTime hasta)
        {
            this.suscripcion = suscripcion;
            this.vehiculos = vehiculos;
            this.desde = desde;
            this.hasta = hasta;
        }

        public void Calcular(FacturaBE factura)
        {
            factura.Servicio = "Flota SaaS";
            factura.IdSuscripcion = suscripcion.IdSuscripcion;
            factura.Cantidad = vehiculos;
            factura.PrecioUnitario = SuscripcionBLL.PrecioPorPatente;
            factura.Detalle = "Abono Flota SaaS por " + vehiculos + " vehículo(s), del " + desde.ToString("dd/MM/yyyy") + " al "
                + hasta.ToString("dd/MM/yyyy");
        }
    }
}
