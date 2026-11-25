using System.Collections.Generic;
using System.Web.Services;
using System.Web.Services.Protocols;
using LogTech.BE;
using LogTech.Fachada;

namespace LogTech
{
    // Servicio web del anexo 12.4: los planes de seguro de carga que se pueden contratar desde LogTech (R16), con su precio
    // por mes. Es público (no pide usuario): no muestra datos de nadie. La contratación se hace desde «Seguros» (CU-006-031).
    [WebService(Namespace = "http://logtech.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class WS_EmisionPolizas : WebService
    {
        private readonly FachadaNegocio negocio = new FachadaNegocio();

        [WebMethod(Description = "Planes de seguro de una aseguradora (vacío = todas), con la cobertura por viaje y el precio por mes.")]
        public List<PlanSeguroBE> CotizarPlanes(string aseguradora)
        {
            var planes = new List<PlanSeguroBE>();
            foreach (var nombre in negocio.ListarAseguradoras())
                if (string.IsNullOrEmpty(aseguradora) || nombre == aseguradora) planes.AddRange(negocio.ListarPlanesSeguro(nombre));
            if (planes.Count == 0)
                throw new SoapException("No hay una aseguradora con ese nombre.", SoapException.ClientFaultCode);
            return planes;
        }
    }
}
