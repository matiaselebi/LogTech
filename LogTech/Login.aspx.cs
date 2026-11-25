using System;
using System.Data.SqlClient;
using LogTech.BE;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class Login : PaginaBase
    {
        private readonly FachadaSeguridad fachada = new FachadaSeguridad();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && GestorSesion.Instancia.HaySesion)
            {
                Response.Redirect("~/Panel.aspx");
                return;
            }

            AplicarIdioma();
            if (!IsPostBack && Request.QueryString["registro"] == "ok")
                MostrarMensaje("La cuenta fue creada. Ya podés iniciar sesión.", false);
            if (!IsPostBack && Request.QueryString["recuperacion"] == "ok")
                MostrarMensaje("La contraseña fue actualizada. Ya podés iniciar sesión.", false);
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            var usuario = txtUsuario.Text.Trim().ToLowerInvariant();
            var clave = txtClave.Text;
            if (usuario.Length == 0 || clave.Length == 0)
            {
                MostrarMensaje("Ingresá tu usuario o correo y tu contraseña.", true);
                return;
            }
            try
            {
                var resultado = fachada.IniciarSesion(usuario, clave);
                if (resultado.Estado != EstadoAutenticacion.Exitoso)
                {
                    if (resultado.Estado == EstadoAutenticacion.IntegridadComprometida)
                        MostrarMensaje(IntegridadException.SistemaNoDisponible, true);
                    else if (resultado.Estado == EstadoAutenticacion.Bloqueado)
                        MostrarMensaje(resultado.SegundosBloqueo > 0
                            ? "La cuenta está bloqueada. Esperá " + resultado.SegundosBloqueo + " segundos o recuperá la contraseña."
                            : "La cuenta está bloqueada. Contactá al administrador o recuperá la contraseña.", true);
                    else if (resultado.Estado == EstadoAutenticacion.Inactivo)
                        MostrarMensaje("La cuenta está inactiva. Contactá al administrador.", true);
                    // Con un usuario que no existe no hay intentos que contar: solo el mensaje.
                    else if (resultado.IntentosRestantes > 0)
                        MostrarMensaje("Usuario o contraseña incorrectos. Intentos restantes: " + resultado.IntentosRestantes + ".", true);
                    else
                        MostrarMensaje("Usuario o contraseña incorrectos.", true);
                    return;
                }

                var autenticado = resultado.Usuario;
                var esContingencia = autenticado.Rol == UsuarioContingencia.RolContingencia;
                var baseDaniada = resultado.Problemas != null && resultado.Problemas.Count > 0;

                // Con la base dañada no se guarda el idioma: escribiría en Usuarios.
                if (!esContingencia && !baseDaniada && IdiomaServicio.FueSeleccionado)
                {
                    autenticado.Idioma = IdiomaServicio.IdiomaActual;
                    fachada.GuardarIdioma(autenticado.IdUsuario, autenticado.Idioma);
                }
                else
                {
                    IdiomaServicio.CargarPreferencia(autenticado.Idioma);
                }

                GestorSesion.Instancia.Iniciar(autenticado);
                GestorSesion.Instancia.IntegridadComprometida = baseDaniada;
                // La contingencia entra aunque la base esté caída: la bitácora es best-effort.
                try { fachada.RegistrarEvento(autenticado.IdUsuario, autenticado.Usuario, "Inicio de sesión con rol " + autenticado.Rol, "Informativa"); }
                catch (SqlException) { if (!esContingencia) throw; }
                Response.Redirect("~/Panel.aspx");
            }
            catch (SqlException)
            {
                MostrarMensaje("No se pudo conectar con la base de datos LogTechDB.", true);
            }
        }

        private void AplicarIdioma()
        {
            litEtiqueta.Text = "Acceso seguro";
            litTitulo.Text = "Bienvenido de nuevo";
            litBajada.Text = "Iniciá sesión para acceder a la demostración de LogTech.";
            litAcceso.Text = "Iniciar sesión";
            lblUsuario.Text = "Usuario o correo";
            lblClave.Text = "Contraseña";
            btnIngresar.Text = "Iniciar sesión";
        }

        private void MostrarMensaje(string texto, bool esError)
        {
            lblError.Text = texto;
            lblError.CssClass = esError ? "form-error" : "form-success";
        }
    }
}
