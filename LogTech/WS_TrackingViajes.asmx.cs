using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Services.Protocols;
using LogTech.BE;
using LogTech.Fachada;

namespace LogTech
{
    // Servicio web del anexo 12.4: el sistema de un dador (su ERP) consulta el estado y la última ubicación de uno
    // de sus viajes. Se identifica con la clave de API que el administrador le genera al dador en «Ver perfil» (R46).
    [WebService(Namespace = "http://logtech.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class WS_TrackingViajes : WebService
    {
        private readonly FachadaNegocio negocio = new FachadaNegocio();

        [WebMethod(Description = "Estado y última ubicación informada de un viaje del dador dueño de la clave de API.")]
        public SeguimientoBE ObtenerUbicacion(string claveApi, int idViaje)
        {
            string error;
            SeguimientoBE seguimiento;
            try { seguimiento = negocio.ConsultarSeguimiento(claveApi, idViaje, out error); }
            catch (SqlException) { throw new SoapException("No se pudo conectar con la base de datos.", SoapException.ServerFaultCode); }
            if (seguimiento == null) throw new SoapException(error, SoapException.ClientFaultCode);
            return seguimiento;
        }
    }
}
