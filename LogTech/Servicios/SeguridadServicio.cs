using System;
using System.Security.Cryptography;
using System.Text;

namespace LogTech.Servicios
{
    public static class SeguridadServicio
    {
        public static string GenerarSalt()
        {
            var bytes = new byte[16];
            using (var generador = RandomNumberGenerator.Create()) generador.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        // Token de 64 caracteres hexadecimales para el enlace de recuperación de contraseña.
        public static string GenerarToken()
        {
            var bytes = new byte[32];
            using (var generador = RandomNumberGenerator.Create()) generador.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        // SHA-256 de salt + clave, en hexadecimal.
        public static string CalcularHashSha256(string clave, string salt)
        {
            var texto = (salt ?? string.Empty) + (clave ?? string.Empty);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        public static bool VerificarClave(string clave, string salt, string hashGuardado)
        {
            return string.Equals(CalcularHashSha256(clave, salt), hashGuardado, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ClaveCumplePolitica(string clave)
        {
            // De 8 a 64 caracteres (64 es el tope de los campos de contraseña).
            if (string.IsNullOrWhiteSpace(clave) || clave.Length < 8 || clave.Length > 64) return false;
            var tieneMayuscula = false;
            var tieneEspecial = false;
            foreach (var caracter in clave)
            {
                if (char.IsUpper(caracter)) tieneMayuscula = true;
                if (!char.IsLetterOrDigit(caracter) && !char.IsWhiteSpace(caracter)) tieneEspecial = true;
            }
            return tieneMayuscula && tieneEspecial;
        }
    }
}
