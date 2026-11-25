using LogTech.BE;

namespace LogTech.BLL
{
    // Patrón Strategy (sección 12.6 del análisis): cada servicio de LogTech calcula a su manera qué se le factura al
    // cliente. FacturacionBLL (el contexto) arma la factura sin saber de qué servicio se trata: le pide a la
    // estrategia el detalle, la cantidad y el precio. Para facturar un servicio nuevo alcanza con otra estrategia.
    public interface IEstrategiaFacturacion
    {
        // Completa el servicio, el detalle, la cantidad, el precio unitario (con IVA) y de qué viaje o suscripción sale.
        void Calcular(FacturaBE factura);
    }
}
