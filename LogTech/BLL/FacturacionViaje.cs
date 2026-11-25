using LogTech.BE;

namespace LogTech.BLL
{
    // Estrategia del viaje entregado: LogTech le factura al dador lo que se queda del precio, la comisión del 7 %
    // (0 en los 3 primeros viajes de un dador nuevo) y el remito digital.
    public sealed class FacturacionViaje : IEstrategiaFacturacion
    {
        private readonly ViajeBE viaje;

        public FacturacionViaje(ViajeBE viaje)
        {
            this.viaje = viaje;
        }

        public void Calcular(FacturaBE factura)
        {
            factura.Servicio = "Comisión por viaje";
            factura.IdViaje = viaje.IdViaje;
            factura.Cantidad = 1;
            factura.PrecioUnitario = viaje.Comision + viaje.CargoRemito;
            factura.Detalle = "Viaje N° " + viaje.IdViaje + " de " + viaje.Origen + " a " + viaje.Destino + ": comisión "
                + FacturacionBLL.Pesos(viaje.Comision) + " y remito digital " + FacturacionBLL.Pesos(viaje.CargoRemito);
        }
    }
}
