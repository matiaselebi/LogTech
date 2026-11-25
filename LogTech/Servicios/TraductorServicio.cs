using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogTech.Servicios
{
    // Traducción de la interfaz.
    //   App_Data/Traducciones/en-GB.json y pt-PT.json: las traducciones, hechas una vez a mano.
    //   App_Data/Traducciones/es-ES.json: la lista de textos de la interfaz. Solo esos textos se
    //   traducen, así nunca sale hacia un servicio externo un dato cargado por un usuario.
    // Si a un texto de la lista le falta la traducción, se le pide a MyMemory y se guarda en el
    // .json del idioma: la próxima vez ya no se consulta.
    public static class TraductorServicio
    {
        private const int ConsultasPorPedido = 3;
        private static readonly object bloqueo = new object();
        // Después de un error de MyMemory (o de agotar su cuota) se deja de consultar un rato.
        private static DateTime pausaHasta;

        // Traducciones guardadas del idioma (vacío para el español).
        public static Dictionary<string, string> Leer(string idioma)
        {
            if (idioma == "es-ES") return new Dictionary<string, string>();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Ruta(idioma)))
                ?? new Dictionary<string, string>();
        }

        // Devuelve la traducción de cada texto de la interfaz que se pueda traducir.
        public static Dictionary<string, string> Traducir(IEnumerable<string> textos, string idioma)
        {
            var resultado = new Dictionary<string, string>();
            idioma = CatalogoIdiomas.Normalizar(idioma);
            if (idioma == "es-ES") return resultado;

            lock (bloqueo)
            {
                var interfaz = new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Ruta("es-ES"))));
                var traducciones = Leer(idioma);
                var consultas = 0;
                var nuevas = false;
                foreach (var texto in textos)
                {
                    if (texto == null || !interfaz.Contains(texto)) continue;
                    string traduccion;
                    if (!traducciones.TryGetValue(texto, out traduccion))
                    {
                        if (consultas == ConsultasPorPedido || DateTime.UtcNow < pausaHasta) continue;
                        consultas++;
                        traduccion = ConsultarMyMemory(texto, idioma);
                        if (traduccion == null) continue;
                        traducciones[texto] = traduccion;
                        nuevas = true;
                    }
                    resultado[texto] = traduccion;
                }
                if (nuevas) File.WriteAllText(Ruta(idioma), JsonConvert.SerializeObject(traducciones, Formatting.Indented), Encoding.UTF8);
            }
            return resultado;
        }

        private static string Ruta(string idioma)
        {
            return Path.Combine(CatalogoIdiomas.Carpeta, idioma + ".json");
        }

        // API gratuita de MyMemory (sin cuenta: unos 5000 caracteres por día).
        private static string ConsultarMyMemory(string texto, string idioma)
        {
            try
            {
                var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(texto)
                    + "&langpair=" + Uri.EscapeDataString("es-ES|" + idioma);
                var solicitud = (HttpWebRequest)WebRequest.Create(url);
                solicitud.Timeout = 4000;
                solicitud.ReadWriteTimeout = 4000;
                using (var respuesta = solicitud.GetResponse())
                using (var lector = new StreamReader(respuesta.GetResponseStream()))
                {
                    var json = JObject.Parse(lector.ReadToEnd());
                    if ((bool?)json["quotaFinished"] == true)
                    {
                        pausaHasta = DateTime.UtcNow.AddHours(24);
                        return null;
                    }
                    var traduccion = (int?)json["responseStatus"] == 200 ? (string)json["responseData"]?["translatedText"] : null;
                    return string.IsNullOrWhiteSpace(traduccion) ? null : HttpUtility.HtmlDecode(traduccion);
                }
            }
            catch (WebException) { }
            catch (JsonException) { }
            pausaHasta = DateTime.UtcNow.AddMinutes(5);
            return null;
        }
    }
}
