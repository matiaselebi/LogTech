namespace LogTech.Controles
{
    public partial class MisViajes
    {
        protected global::System.Web.UI.WebControls.Label lblMensaje;
        protected global::System.Web.UI.WebControls.DropDownList ddlVista;
        protected global::System.Web.UI.WebControls.GridView gvViajes;
        protected global::System.Web.UI.WebControls.Panel pnlEstado;
        protected global::System.Web.UI.WebControls.Literal litEstadoViaje;
        protected global::System.Web.UI.WebControls.Literal litEstadoActual;
        protected global::System.Web.UI.WebControls.DropDownList ddlEstadoNuevo;
        protected global::System.Web.UI.WebControls.HiddenField hfLatitud;
        protected global::System.Web.UI.WebControls.HiddenField hfLongitud;
        protected global::System.Web.UI.WebControls.Button btnGuardarEstado;
        protected global::System.Web.UI.WebControls.Button btnCancelarEstado;
        protected global::System.Web.UI.WebControls.Panel pnlIncidencia;
        protected global::System.Web.UI.WebControls.Literal litIncidenciaViaje;
        protected global::System.Web.UI.WebControls.DropDownList ddlTipoIncidencia;
        protected global::System.Web.UI.WebControls.FileUpload fuFotoIncidencia;
        protected global::System.Web.UI.WebControls.TextBox txtDescripcion;
        protected global::System.Web.UI.WebControls.Button btnReportar;
        protected global::System.Web.UI.WebControls.Button btnCancelarIncidencia;
        protected global::System.Web.UI.WebControls.Panel pnlRemito;
        protected global::System.Web.UI.WebControls.Literal litRemitoViaje;
        protected global::System.Web.UI.WebControls.TextBox txtReceptor;
        protected global::System.Web.UI.WebControls.TextBox txtDniReceptor;
        protected global::System.Web.UI.WebControls.FileUpload fuFotoRemito;
        protected global::System.Web.UI.WebControls.Button btnEmitirRemito;
        protected global::System.Web.UI.WebControls.Button btnCancelarRemito;
        protected global::System.Web.UI.WebControls.Panel pnlAsignar;
        protected global::System.Web.UI.WebControls.Literal litAsignarViaje;
        protected global::System.Web.UI.WebControls.Literal litAsignarDetalle;
        protected global::System.Web.UI.WebControls.DropDownList ddlConductor;
        protected global::System.Web.UI.WebControls.DropDownList ddlVehiculo;
        protected global::System.Web.UI.WebControls.Literal litAvisoAsignar;
        protected global::System.Web.UI.WebControls.Button btnAsignar;
        protected global::System.Web.UI.WebControls.Button btnCancelarAsignar;
        protected global::LogTech.Controles.DetalleViaje ucDetalle;
    }
}
