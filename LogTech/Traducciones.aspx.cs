using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Services;
using System.Web.UI;
using LogTech.Servicios;
using Newtonsoft.Json;

namespace LogTech
{
    // Servicio que usa Scripts/idiomas.js para pedir los textos de la página que todavía no
    // tienen traducción guardada.
    public class Traducciones : Page
    {
        [WebMethod(EnableSession = true)]
        public static Dictionary<string, string> Obtener(string[] textos)
        {
            if (textos == null || textos.Length > 250) return new Dictionary<string, string>();
            try { return TraductorServicio.Traducir(textos, IdiomaServicio.IdiomaActual); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (JsonException) { }
            return new Dictionary<string, string>();
        }
    }
}
