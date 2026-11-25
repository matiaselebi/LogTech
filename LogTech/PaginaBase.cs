using System;
using System.IO;
using System.Web.UI;
using LogTech.Servicios;
using Newtonsoft.Json;

namespace LogTech
{
    public class PaginaBase : Page
    {
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var idioma = IdiomaServicio.IdiomaActual;
            if (idioma != "es-ES")
            {
                try
                {
                    var json = JsonConvert.SerializeObject(TraductorServicio.Leer(idioma),
                        new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
                    // Las traducciones guardadas llegan con la página: no hay que esperar a nadie.
                    ClientScript.RegisterClientScriptBlock(GetType(), "cacheIdiomas",
                        "window.traduccionesGuardadas = " + json + ";", true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (JsonException) { }
            }
            // Traduce solo la presentación, sin alterar controles ni ViewState.
            ClientScript.RegisterClientScriptInclude("idiomas", ResolveUrl("~/Scripts/idiomas.js?v=4"));
        }
    }
}
