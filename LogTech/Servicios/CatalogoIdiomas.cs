using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using Newtonsoft.Json;

namespace LogTech.Servicios
{
    public static class CatalogoIdiomas
    {
        public static string Carpeta { get { return HostingEnvironment.MapPath("~/App_Data/Traducciones"); } }
        public static IDictionary<string, string> Obtener()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(Carpeta, "idiomas.json")));
        }
        public static string Normalizar(string idioma)
        {
            if (idioma == "es") idioma = "es-ES";
            if (idioma == "en") idioma = "en-GB";
            return idioma != null && Obtener().ContainsKey(idioma) ? idioma : "es-ES";
        }
    }
}
