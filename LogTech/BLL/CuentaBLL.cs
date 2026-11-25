using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class CuentaBLL
    {
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        // Indica si la cuenta sigue habilitada. La página lo consulta en cada pedido
        // para cortar la sesión si un administrador bloqueó o dio de baja al usuario.
        public bool SigueActiva(int idUsuario)
        {
            return usuarios.ObtenerEstado(idUsuario) == "Activo";
        }

        // Cambia la contraseña de un usuario autenticado. Devuelve false si la contraseña
        // actual no coincide. La política y la confirmación las valida la página.
        public bool CambiarClave(int idUsuario, string usuario, string claveActual, string claveNueva)
        {
            var cuenta = usuarios.ObtenerPorId(idUsuario);
            if (cuenta == null || !SeguridadServicio.VerificarClave(claveActual, cuenta.ClaveSalt, cuenta.ClaveHash))
                return false;

            var salt = SeguridadServicio.GenerarSalt();
            var hash = SeguridadServicio.CalcularHashSha256(claveNueva, salt);
            usuarios.RestablecerClave(idUsuario, hash, salt);
            bitacora.Registrar(idUsuario, usuario, "Cambio de contraseña", "Informativa");
            return true;
        }
    }
}
