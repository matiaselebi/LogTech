using System;
using System.Data.SqlClient;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class RestablecerClave : PaginaBase
    {
        private readonly FachadaSeguridad fachada = new FachadaSeguridad();

        private string Token { get { return Request.QueryString["token"]; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (GestorSesion.Instancia.HaySesion) Response.Redirect("~/Panel.aspx");

            if (IsPostBack) return;
            try
            {
                if (!fachada.TokenVigente(Token)) MostrarInvalido();
            }
            catch (SqlException)
            {
                lblMensaje.Text = "No se pudo validar el enlace. Verificá la conexión con LogTechDB.";
            }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNuevaClave.Text) || string.IsNullOrWhiteSpace(txtConfirmarClave.Text))
            {
                lblMensaje.Text = "Completá los dos campos.";
                return;
            }
            if (!fachada.ClaveCumplePolitica(txtNuevaClave.Text))
            {
                lblMensaje.Text = "La contraseña debe tener al menos 8 caracteres, una mayúscula y un carácter especial (por ejemplo, ! # $ o @).";
                return;
            }
            if (txtNuevaClave.Text != txtConfirmarClave.Text)
            {
                lblMensaje.Text = "Las contraseñas no coinciden.";
                return;
            }

            try
            {
                if (!fachada.RestablecerClave(Token, txtNuevaClave.Text))
                {
                    MostrarInvalido();
                    return;
                }
                Response.Redirect("~/Login.aspx?recuperacion=ok");
            }
            catch (SqlException)
            {
                lblMensaje.Text = "No se pudo actualizar la contraseña. Verificá la conexión con LogTechDB.";
            }
            catch (LogTech.BE.IntegridadException)
            {
                lblMensaje.Text = LogTech.BE.IntegridadException.SistemaNoDisponible;
            }
        }

        private void MostrarInvalido()
        {
            pnlFormulario.Visible = false;
            pnlInvalido.Visible = true;
        }
    }
}
