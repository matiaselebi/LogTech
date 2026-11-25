using System.Web.UI;

namespace LogTech.Controles
{
    // Estado del legajo en verde, amarillo o rojo (componente del análisis, 12.7).
    public partial class SemaforoDocumentacion : UserControl
    {
        // Las propiedades van en el ViewState: así el control se sigue viendo después de un postback que no lo vuelve a cargar.
        public string Color
        {
            get { return (string)ViewState["Color"]; }
            set { ViewState["Color"] = value; }
        }

        public string Detalle
        {
            get { return (string)ViewState["Detalle"]; }
            set { ViewState["Detalle"] = value; }
        }

        protected string Titulo
        {
            get
            {
                if (Color == "Rojo") return "Documentación con problemas: no podés tomar viajes";
                if (Color == "Amarillo") return "Documentación para revisar";
                return "Documentación en regla";
            }
        }
    }
}
