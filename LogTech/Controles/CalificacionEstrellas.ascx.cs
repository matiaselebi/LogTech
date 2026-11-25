using System;
using System.Web.UI;

namespace LogTech.Controles
{
    // Puntaje de 1 a 5 dibujado con estrellas (componente del análisis, 12.7).
    public partial class CalificacionEstrellas : UserControl
    {
        // Las propiedades van en el ViewState: así el control se sigue viendo después de un postback que no lo vuelve a cargar.
        public decimal Puntaje
        {
            get { return ViewState["Puntaje"] == null ? 0 : (decimal)ViewState["Puntaje"]; }
            set { ViewState["Puntaje"] = value; }
        }

        // Cuántas calificaciones dan ese promedio (0 = no se muestra, por ejemplo en una reseña suelta).
        public int Cantidad
        {
            get { return ViewState["Cantidad"] == null ? 0 : (int)ViewState["Cantidad"]; }
            set { ViewState["Cantidad"] = value; }
        }

        private int Enteras { get { return (int)Math.Round(Puntaje, MidpointRounding.AwayFromZero); } }
        protected string Llenas { get { return new string('★', Enteras); } }
        protected string Vacias { get { return new string('☆', 5 - Enteras); } }
        protected string Texto
        {
            get
            {
                if (Puntaje == 0) return "Sin calificaciones";
                var texto = Puntaje.ToString("0.0") + " de 5";
                if (Cantidad == 1) return texto + " (1 calificación)";
                return Cantidad > 1 ? texto + " (" + Cantidad + " calificaciones)" : texto;
            }
        }
    }
}
