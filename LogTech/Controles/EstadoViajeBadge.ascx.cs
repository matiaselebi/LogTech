using System.Web.UI;

namespace LogTech.Controles
{
    // Etiqueta de color con el estado de un viaje (componente del análisis, 12.7).
    public partial class EstadoViajeBadge : UserControl
    {
        // Las propiedades van en el ViewState: así el control se sigue viendo después de un postback que no lo vuelve a cargar.
        public string Estado
        {
            get { return (string)ViewState["Estado"]; }
            set { ViewState["Estado"] = value; }
        }

        protected string Clase
        {
            get
            {
                if (Estado == "Pendiente") return "estado-pendiente";
                if (Estado == "Entregado") return "estado-entregado";
                if (Estado == "Cancelado") return "estado-cancelado";
                return "estado-en-curso";
            }
        }
    }
}
