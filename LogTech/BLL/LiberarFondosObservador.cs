using LogTech.BE;

namespace LogTech.BLL
{
    // Observador (CU-005-025): cuando el viaje se entrega con el remito, liquida el pago. Con tarjeta libera
    // la plata retenida al transportista; en efectivo el viaje queda «A cobrar» hasta que el dador pague en el punto de pago.
    public sealed class LiberarFondosObservador : IObservadorViaje
    {
        private readonly BilleteraBLL billetera = new BilleteraBLL();

        public void Actualizar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario)
        {
            if (viaje.Estado == "Entregado") billetera.Liquidar(viaje);
        }
    }
}
