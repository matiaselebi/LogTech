using System;
using System.Configuration;
using LogTech.BE;

namespace LogTech.Servicios
{
    // Cuenta de emergencia definida en Web.config. Sirve para entrar cuando la base
    // de datos está caída o dañada y hay que restaurar un backup. No consulta la base.
    public static class UsuarioContingencia
    {
        public const string RolContingencia = "Contingencia";

        private static string Usuario_ { get { return ConfigurationManager.AppSettings["ContingenciaUsuario"]; } }
        private static string Salt { get { return ConfigurationManager.AppSettings["ContingenciaClaveSalt"]; } }
        private static string Hash { get { return ConfigurationManager.AppSettings["ContingenciaClaveHash"]; } }

        public static bool Coincide(string identificador, string clave)
        {
            if (string.IsNullOrEmpty(Usuario_) || string.IsNullOrEmpty(Salt) || string.IsNullOrEmpty(Hash))
                return false;

            var ingresado = (identificador ?? string.Empty).Trim();
            return string.Equals(ingresado, Usuario_, StringComparison.OrdinalIgnoreCase)
                && SeguridadServicio.VerificarClave(clave, Salt, Hash);
        }

        public static UsuarioBE Usuario
        {
            get
            {
                return new UsuarioBE
                {
                    IdUsuario = 0,
                    Usuario = Usuario_,
                    Nombre = "Usuario",
                    Apellido = "de contingencia",
                    Rol = RolContingencia,
                    Idioma = "es-ES",
                    Estado = "Activo"
                };
            }
        }
    }
}
