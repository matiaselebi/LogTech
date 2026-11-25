using System;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var haySesion = GestorSesion.Instancia.HaySesion;
            // Con sesión, el botón lleva a "Mi perfil" (el panel ya se abre desde "Inicio" de la barra del panel).
            // El usuario de contingencia no tiene perfil: a él lo lleva al panel.
            var contingencia = GestorSesion.Instancia.Rol == UsuarioContingencia.RolContingencia;
            lnkCuenta.Text = !haySesion ? "Iniciar sesión" : contingencia ? "Mi panel" : "Mi perfil";
            lnkCuenta.NavigateUrl = !haySesion ? "~/Login.aspx" : contingencia ? "~/Panel.aspx" : "~/Panel.aspx?seccion=Perfil";
            // Con sesión, el logo lleva al inicio del panel y los enlaces de la landing pasan al pie.
            lnkMarca.HRef = lnkMarcaPie.HRef = haySesion ? "~/Panel.aspx" : "~/";
            phEnlacesArriba.Visible = !haySesion;
            phEnlacesPie.Visible = haySesion;
            var idioma = IdiomaServicio.IdiomaActual;
            btnEspanol.Visible = idioma != "es-ES";
            btnIngles.Visible = idioma != "en-GB";
            btnPortugues.Visible = idioma != "pt-PT";
            litCambiarIdioma.Text = idioma == "en-GB" ? "Change language" : idioma == "pt-PT" ? "Alterar idioma" : "Cambiar idioma";
            btnEspanol.Text = idioma == "en-GB" ? "Spanish" : "Espanhol";
            btnIngles.Text = idioma == "pt-PT" ? "Inglês" : "Inglés";
            btnPortugues.Text = idioma == "en-GB" ? "Portuguese" : "Portugués";
            lblAvisoIdioma.Text = Convert.ToString(Session["AvisoIdioma"]);
            Session.Remove("AvisoIdioma");
        }

        // Botón para contratar: el transportista (Premium) mientras no la tenga vigente. El dador siempre lo ve: con la
        // Flota SaaS su cuenta pasa a flota. La flota no lo ve: maneja su suscripción desde el menú del panel.
        // Va en PreRender para que desaparezca en el mismo postback en que se contrata.
        protected void Page_PreRender(object sender, EventArgs e)
        {
            var sesion = GestorSesion.Instancia;
            if (sesion.HaySesion && sesion.Rol == "Dador de carga") { lnkSuscripcion.Visible = true; return; }
            if (!sesion.HaySesion || sesion.Rol != "Transportista") return;
            try { lnkSuscripcion.Visible = new FachadaNegocio().ObtenerSuscripcion(sesion.IdUsuario, sesion.Rol) == null; }
            catch (SqlException) { }   // sin base no se muestra
        }

        protected void btnIdioma_Command(object sender, CommandEventArgs e)
        {
            var idioma = CatalogoIdiomas.Normalizar(Convert.ToString(e.CommandArgument));
            if (GestorSesion.Instancia.HaySesion)
            {
                try { new FachadaSeguridad().GuardarIdioma(GestorSesion.Instancia.IdUsuario, idioma); }
                catch (SqlException)
                {
                    // Conserva la sesión aunque falte aplicar la migración de idiomas.
                    Session["AvisoIdioma"] = "El idioma cambió en esta sesión, pero no se pudo guardar en tu cuenta. Revisá la conexión con LogTechDB.";
                }
            }
            IdiomaServicio.Seleccionar(idioma);
            Response.Redirect(Request.RawUrl);
        }
    }
}
