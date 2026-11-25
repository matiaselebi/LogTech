using System.Web.UI;
using LogTech.BE;

namespace LogTech.Controles
{
    // Ficha de un camión (componente del análisis, 12.7): la usan "Mis vehículos" y el match del dador.
    public partial class DetalleVehiculoCard : UserControl
    {
        // Las propiedades van en el ViewState: así el control se sigue viendo después de un postback que no lo vuelve a cargar.
        public VehiculoBE Vehiculo
        {
            get { return (VehiculoBE)ViewState["Vehiculo"]; }
            set { ViewState["Vehiculo"] = value; }
        }

        // El semáforo de la documentación solo se muestra si viene calculado y el vehículo está activo.
        protected bool ConSemaforo
        {
            get { return Vehiculo != null && Vehiculo.Semaforo != null && Vehiculo.Estado == "Activo"; }
        }

        protected string TextoSemaforo
        {
            get
            {
                if (Vehiculo.Semaforo == "Verde") return "Habilitado";
                return Vehiculo.Semaforo == "Amarillo" ? "En revisión" : "Inhabilitado";
            }
        }

        protected string Situacion
        {
            get
            {
                if (Vehiculo == null) return string.Empty;
                if (Vehiculo.Estado == "Inactivo") return "Dado de baja";
                return Vehiculo.EnViaje ? "En viaje" : "Disponible";
            }
        }
    }
}
