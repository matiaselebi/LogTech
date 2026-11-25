using LogTech.BE;

namespace LogTech.BLL
{
    // Observador (CU-008-039): cuando el viaje se entrega con el remito, LogTech le factura al dador la comisión y el
    // remito. Si el pago está en disputa no se factura todavía: se factura si el administrador le paga al transportista.
    public sealed class FacturarViajeObservador : IObservadorViaje
    {
        private readonly FacturacionBLL facturacion = new FacturacionBLL();

        public void Actualizar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario)
        {
            if (viaje.Estado != "Entregado" || !viaje.IdDador.HasValue) return;
            if (viaje.EstadoPago == "En disputa" || viaje.EstadoPago == "Devuelto") return;
            facturacion.Emitir(new FacturacionViaje(viaje), viaje.IdDador.Value);
        }
    }
}
