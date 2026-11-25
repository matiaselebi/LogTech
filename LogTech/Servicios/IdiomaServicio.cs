using System;
using System.Web;

namespace LogTech.Servicios
{
    public static class IdiomaServicio
    {
        public static string IdiomaActual
        {
            get
            {
                var idioma = Normalizar(Convert.ToString(HttpContext.Current.Session["Idioma"]));
                HttpContext.Current.Session["Idioma"] = idioma;
                return idioma;
            }
        }

        public static bool FueSeleccionado { get { return Convert.ToBoolean(HttpContext.Current.Session["IdiomaSeleccionado"]); } }

        public static void Seleccionar(string idioma)
        {
            HttpContext.Current.Session["Idioma"] = Normalizar(idioma);
            HttpContext.Current.Session["IdiomaSeleccionado"] = true;
        }

        public static void CargarPreferencia(string idioma)
        {
            HttpContext.Current.Session["Idioma"] = Normalizar(idioma);
        }

        private static string Normalizar(string idioma) { return CatalogoIdiomas.Normalizar(idioma); }
    }
}
