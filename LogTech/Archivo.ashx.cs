using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.SessionState;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    // Entrega los archivos que subieron los usuarios (documentos, remitos, incidencias y la foto de perfil).
    // Están en App_Data, que IIS no publica: solo salen por acá y solo para quien puede verlos.
    // Uso: Archivo.ashx?tipo=documento|remito|incidencia|foto&id=N
    public class Archivo : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            int id;
            if (!GestorSesion.Instancia.HaySesion || !int.TryParse(context.Request.QueryString["id"], out id))
            {
                context.Response.StatusCode = 403;
                return;
            }

            string ruta;
            try
            {
                if (context.Request.QueryString["tipo"] == "foto")
                {
                    // La foto de perfil: cada uno ve solo la suya.
                    ruta = new FachadaSeguridad().RutaFotoPerfil(GestorSesion.Instancia.IdUsuario);
                }
                else
                {
                    var esAnalista = new FachadaSeguridad().ObtenerPermisosDeUsuario(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Rol)
                        .Contains("Validar documentación");
                    ruta = new FachadaNegocio().ObtenerArchivo(context.Request.QueryString["tipo"], id, GestorSesion.Instancia.IdUsuario, esAnalista);
                }
            }
            catch (SqlException)
            {
                context.Response.StatusCode = 503;
                return;
            }

            var fisica = ruta == null ? null : ArchivoServicio.RutaFisica(ruta);
            if (fisica == null || !File.Exists(fisica))
            {
                context.Response.StatusCode = 404;
                return;
            }
            context.Response.ContentType = ArchivoServicio.TipoContenido(ruta);
            context.Response.AppendHeader("Content-Disposition", "inline; filename=" + Path.GetFileName(ruta));
            context.Response.AppendHeader("X-Content-Type-Options", "nosniff");
            context.Response.TransmitFile(fisica);
        }
    }
}
