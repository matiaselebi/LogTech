using System;
using System.Configuration;
using System.Threading;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ValidacionPatrones.Ejecutar();
            // CU-003-021: revisa cada hora si ya corrió la rutina de vencimientos del día.
            reloj = new Timer(RevisarVencimientos, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
        }

        // Certificado digital (punto 10b): todo el sitio va por HTTPS, así la tarjeta, la contraseña y los datos personales
        // viajan cifrados. En desarrollo se usa el certificado gratuito de IIS Express («IIS Express Development
        // Certificate», el mismo que usa Visual Studio en https://localhost:44303); en producción se usa uno de una
        // autoridad certificante (por ejemplo, Let's Encrypt, también gratuito). Si se entra por http, se redirige.
        // PuertoHttps vacío en Web.config = no se fuerza (por ejemplo, si se corre sin SSL).
        void Application_BeginRequest(object sender, EventArgs e)
        {
            var puerto = ConfigurationManager.AppSettings["PuertoHttps"];
            if (Request.IsSecureConnection || string.IsNullOrEmpty(puerto)) return;
            var seguro = new UriBuilder(Request.Url) { Scheme = Uri.UriSchemeHttps, Port = int.Parse(puerto) };
            Response.Redirect(seguro.Uri.ToString(), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // Control de acceso (R35): el menú solo dibuja las secciones que permite el rol. Si alguien fuerza un postback a una que
        // no se dibujó, la validación de eventos de ASP.NET (EnableEventValidation) lo rechaza; acá se anota en la bitácora
        // y se vuelve al panel con un aviso, en lugar de mostrar el error del servidor.
        void Application_Error(object sender, EventArgs e)
        {
            var error = Server.GetLastError();
            var causa = error == null ? null : error.GetBaseException();
            if (!(causa is ArgumentException) || causa.StackTrace == null || !causa.StackTrace.Contains("ValidateEvent")) return;
            Server.ClearError();
            try
            {
                if (GestorSesion.Instancia.HaySesion)
                    new FachadaSeguridad().RegistrarEvento(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario,
                        "Intento de entrar a una sección sin permiso (" + Request.Form["__EVENTTARGET"] + ")", "Advertencia");
            }
            catch (Exception) { }
            Response.Redirect("~/Panel.aspx?sinpermiso=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // Se guarda en un campo para que el recolector de basura no lo elimine.
        private static Timer reloj;
        private static DateTime ultimaRutina = DateTime.MinValue;

        // Rutina diaria (R37). Si la base no responde, no se marca como hecha y se reintenta
        // en la próxima hora (ERR002: "reprogramar el ciclo").
        private static void RevisarVencimientos(object estado)
        {
            if (ultimaRutina == DateTime.Today) return;
            try
            {
                new FachadaNegocio().ProcesarVencimientos();
                ultimaRutina = DateTime.Today;
            }
            catch (Exception)
            {
                // En un hilo de fondo, una excepción sin atrapar tira abajo toda la aplicación.
            }
        }
    }
}
