using System;
using System.Data.SqlClient;
using System.Linq;
using LogTech.BE;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class Registro : PaginaBase
    {
        private readonly FachadaSeguridad fachada = new FachadaSeguridad();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (GestorSesion.Instancia.HaySesion) Response.Redirect("~/Panel.aspx");
        }

        protected void btnCrearCuenta_Click(object sender, EventArgs e)
        {
            var perfil = ddlPerfil.SelectedValue;
            var cuit = new string((txtCuitCuil.Text ?? string.Empty).Where(char.IsDigit).ToArray());
            var usuario = new UsuarioBE
            {
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                Correo = txtCorreo.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                CuitCuil = cuit,
                CondicionIva = ddlCondicionIva.SelectedValue,
                Usuario = txtUsuario.Text.Trim(),
                Rol = perfil,
                NumeroLicencia = perfil == "Transportista" ? txtLicencia.Text.Trim() : null,
                RazonSocial = perfil == "Dador de carga" || perfil == "Administrador de Flota" ? txtRazonSocial.Text.Trim() : null
            };

            // El formato de cada dato lo valida la BLL; acá solo que las dos claves coincidan.
            if (txtClave.Text != txtConfirmarClave.Text)
            {
                lblMensaje.Text = "Las contraseñas no coinciden.";
                return;
            }

            try
            {
                string mensaje;
                if (!fachada.CrearCuenta(usuario, txtClave.Text, out mensaje))
                {
                    lblMensaje.Text = mensaje;
                    return;
                }
                Response.Redirect("~/Login.aspx?registro=ok");
            }
            catch (SqlException)
            {
                lblMensaje.Text = "No se pudo crear la cuenta. Verificá la conexión con LogTechDB.";
            }
            catch (LogTech.BE.IntegridadException)
            {
                lblMensaje.Text = LogTech.BE.IntegridadException.SistemaNoDisponible;
            }
        }
    }
}
