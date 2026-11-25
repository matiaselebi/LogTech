using System;
using System.Data.SqlClient;
using System.Net.Mail;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class RecuperarClave : PaginaBase
    {
        private readonly FachadaSeguridad fachada = new FachadaSeguridad();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (GestorSesion.Instancia.HaySesion) Response.Redirect("~/Panel.aspx");
        }

        protected void btnRecuperar_Click(object sender, EventArgs e)
        {
            var correo = txtCorreo.Text.Trim();
            if (string.IsNullOrWhiteSpace(correo))
            {
                lblMensaje.Text = "Ingresá tu correo electrónico.";
                return;
            }
            try { var direccion = new MailAddress(correo); }
            catch (FormatException)
            {
                lblMensaje.Text = "Ingresá un correo electrónico válido.";
                return;
            }

            try
            {
                var urlBase = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath;
                fachada.SolicitarRecuperacion(correo, urlBase);
                MostrarConfirmacion();
            }
            catch (SqlException)
            {
                lblMensaje.Text = "No se pudo procesar el pedido. Verificá la conexión con LogTechDB.";
            }
            catch (LogTech.BE.IntegridadException)
            {
                lblMensaje.Text = LogTech.BE.IntegridadException.SistemaNoDisponible;
            }
            catch (SmtpException)
            {
                lblMensaje.Text = "No se pudo enviar el correo. Revisá la configuración de correo.";
            }
        }

        private void MostrarConfirmacion()
        {
            lblMensaje.CssClass = "form-success";
            lblMensaje.Text = "Si el correo está registrado, te enviamos un enlace para restablecer la contraseña.";
            txtCorreo.Text = string.Empty;
        }
    }
}
