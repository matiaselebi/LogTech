using System.Web.Services;
using System.Web.Services.Protocols;
using LogTech.BE;
using LogTech.Fachada;

namespace LogTech
{
    // Servicio web del anexo 12.4: cuánto cobra al momento un transportista si adelanta un cobro con factoring (4 %).
    // Es público (no pide usuario): no muestra datos de nadie. El pedido real se hace desde la Billetera (CU-006-029).
    [WebService(Namespace = "http://logtech.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class WS_FactoringLogistico : WebService
    {
        private readonly FachadaNegocio negocio = new FachadaNegocio();

        [WebMethod(Description = "Comisión del 4 % y monto que se cobra al momento al adelantar un cobro con factoring.")]
        public CotizacionFactoringBE CotizarAdelanto(decimal monto)
        {
            if (monto <= 0 || monto > 100000000)
                throw new SoapException("El monto tiene que ser mayor a 0 y de hasta $ 100.000.000.", SoapException.ClientFaultCode);
            return negocio.CotizarFactoring(monto);
        }
    }
}
