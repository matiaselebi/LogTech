using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class RegistrarUsuario
    {
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        public bool Ejecutar(UsuarioBE usuario, string clave, out string mensaje)
        {
            if (usuario == null)
            {
                mensaje = "No se recibieron los datos del usuario.";
                return false;
            }

            usuario.Usuario = (usuario.Usuario ?? string.Empty).Trim().ToLowerInvariant();
            usuario.Correo = (usuario.Correo ?? string.Empty).Trim().ToLowerInvariant();
            mensaje = Validar(usuario, clave);
            if (mensaje != null) return false;

            if (usuarios.Existe(usuario.Usuario, usuario.Correo))
            {
                mensaje = "El nombre de usuario o el correo ya están registrados.";
                return false;
            }

            usuario.ClaveSalt = SeguridadServicio.GenerarSalt();
            usuario.ClaveHash = SeguridadServicio.CalcularHashSha256(clave, usuario.ClaveSalt);
            usuario.IdUsuario = usuarios.Registrar(usuario);
            bitacora.Registrar(usuario.IdUsuario, usuario.Usuario, "Creación de cuenta con rol " + usuario.Rol, "Informativa");
            mensaje = string.Empty;
            return true;
        }

        // Devuelve null si los datos están bien, o el texto del error (ERR001).
        private static string Validar(UsuarioBE usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellido)
                || usuario.Correo.Length == 0 || string.IsNullOrWhiteSpace(usuario.Telefono)
                || string.IsNullOrWhiteSpace(usuario.CuitCuil) || usuario.Usuario.Length == 0
                || string.IsNullOrWhiteSpace(usuario.Rol))
                return "Completá todos los campos obligatorios.";
            if (!Validador.EsNombre(usuario.Nombre, 60) || !Validador.EsNombre(usuario.Apellido, 60))
                return "El nombre y el apellido solo pueden tener letras y espacios.";
            if (!Validador.EsCorreo(usuario.Correo)) return "Ingresá un correo electrónico válido.";
            if (!Validador.EsTelefono(usuario.Telefono)) return "El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15).";
            if (!Validador.EsCuit(usuario.CuitCuil)) return "El CUIT o CUIL no es válido: revisá los 11 números.";
            if (!Validador.EsCondicionIva(usuario.CondicionIva)) return "Elegí tu condición frente al IVA.";
            if (!Validador.EsNombreUsuario(usuario.Usuario))
                return "El nombre de usuario debe tener entre 4 y 50 letras, números, puntos o guiones.";
            if (usuario.Rol != "Transportista" && usuario.Rol != "Dador de carga" && usuario.Rol != "Administrador de Flota")
                return "Seleccioná un perfil válido.";
            if (usuario.Rol == "Transportista" && !Validador.EsLicencia(usuario.NumeroLicencia))
                return "Ingresá un número de licencia válido (letras, números y guiones).";
            if ((usuario.Rol == "Dador de carga" || usuario.Rol == "Administrador de Flota") && !Validador.EsTexto(usuario.RazonSocial, 120))
                return "Ingresá la razón social (hasta 120 caracteres).";
            if (!SeguridadServicio.ClaveCumplePolitica(clave))
                return "La contraseña debe tener al menos 8 caracteres, una mayúscula y un carácter especial (por ejemplo, ! # $ o @).";
            return null;
        }
    }
}
