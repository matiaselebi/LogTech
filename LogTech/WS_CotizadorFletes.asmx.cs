using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Services.Protocols;
using LogTech.BE;
using LogTech.Fachada;

namespace LogTech
{
    // Servicio web del anexo 12.4: tarifa estimada de un viaje y la comisión de LogTech. Es público (no pide
    // usuario): lo usan sistemas de terceros y la misma aplicación al publicar una carga.
    [WebService(Namespace = "http://logtech.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class WS_CotizadorFletes : WebService
    {
        private readonly FachadaNegocio negocio = new FachadaNegocio();

        [WebMethod(Description = "Tarifa estimada de un viaje entre dos ciudades de LogTech, con la comisión del 7 % incluida.")]
        public CotizacionBE CalcularTarifa(string origen, string destino, decimal pesoToneladas)
        {
            string error;
            CotizacionBE cotizacion;
            try { cotizacion = negocio.CotizarFlete(origen, destino, pesoToneladas, out error); }
            catch (SqlException) { throw new SoapException("No se pudo conectar con la base de datos.", SoapException.ServerFaultCode); }
            if (cotizacion == null) throw new SoapException(error, SoapException.ClientFaultCode);
            return cotizacion;
        }
    }
}
