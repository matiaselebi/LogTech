using System;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class RecuperarClaveBLL
    {
        private const int MinutosVigencia = 60;
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly RecuperacionClaveDAL recuperaciones = new RecuperacionClaveDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        // Genera el enlace y manda el correo. No informa si el correo existe o no.
        public void Solicitar(string correo, string urlBase)
        {
            var usuario = usuarios.ObtenerPorCorreo((correo ?? string.Empty).Trim().ToLowerInvariant());
            if (usuario == null) return;

            var token = SeguridadServicio.GenerarToken();
            recuperaciones.Crear(usuario.IdUsuario, token, DateTime.Now.AddMinutes(MinutosVigencia));

            var enlace = urlBase.TrimEnd('/') + "/RestablecerClave.aspx?token=" + token;
            var cuerpo = "<p>Recibimos un pedido para restablecer tu contraseña de LogTech.</p>" +
                "<p><a href=\"" + enlace + "\">Restablecer mi contraseña</a></p>" +
                "<p>El enlace vence en " + MinutosVigencia + " minutos. Si no lo pediste, ignorá este correo.</p>";
            CorreoServicio.Enviar(correo.Trim(), "Recuperá tu contraseña - LogTech", cuerpo);

            bitacora.Registrar(usuario.IdUsuario, usuario.Usuario, "Solicitud de recuperación de contraseña", "Informativa");
        }

        // Cambia la contraseña y desbloquea al usuario. Devuelve false si el token no sirve.
        public bool Restablecer(string token, string nuevaClave)
        {
            var idUsuario = recuperaciones.ObtenerUsuarioVigente(token);
            if (idUsuario == 0) return false;

            var salt = SeguridadServicio.GenerarSalt();
            var hash = SeguridadServicio.CalcularHashSha256(nuevaClave, salt);
            usuarios.RestablecerClave(idUsuario, hash, salt);
            recuperaciones.MarcarUsado(token);

            var nombre = usuarios.ObtenerNombreUsuario(idUsuario);
            bitacora.Registrar(idUsuario, nombre, "Contraseña actualizada y usuario desbloqueado", "Informativa");
            return true;
        }

        public bool TokenVigente(string token)
        {
            return recuperaciones.ObtenerUsuarioVigente(token) != 0;
        }
    }
}
