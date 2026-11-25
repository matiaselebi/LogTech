using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;

namespace LogTech
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            // La llamada AJAX debe conservar la ruta del método de página.
            routes.Add(new Route("Traducciones.aspx/Obtener", new StopRoutingHandler()));
            var settings = new FriendlyUrlSettings();
            // No redirigir las llamadas AJAX a métodos .aspx: pueden responder 401.
            // Las direcciones sin extensión siguen funcionando.
            settings.AutoRedirectMode = RedirectMode.Off;
            routes.EnableFriendlyUrls(settings);
        }
    }
}
