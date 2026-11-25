using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LogTech.Servicios
{
    // Cifrado AES-256 de los datos personales (correo, teléfono, CUIT/CUIL, licencia, DNI y CBU). La DAL cifra al guardar
    // y descifra al leer: en la base quedan ilegibles y el resto del sistema trabaja con el dato real.
    // Es determinístico: el IV sale de un HMAC del dato, así el mismo dato cifra siempre igual y se puede buscar con "="
    // (el correo al iniciar sesión, un DNI repetido). La clave está en Web.config (ClaveCifrado).
    public static class CifradoServicio
    {
        private static readonly byte[] ClaveAes;   // cifra
        private static readonly byte[] ClaveIv;    // arma el IV de cada dato

        static CifradoServicio()
        {
            var maestra = Convert.FromBase64String(ConfigurationManager.AppSettings["ClaveCifrado"]);
            using (var sha = SHA256.Create())
            {
                ClaveAes = sha.ComputeHash(Encoding.UTF8.GetBytes("aes|").Concat(maestra).ToArray());
                ClaveIv = sha.ComputeHash(Encoding.UTF8.GetBytes("iv|").Concat(maestra).ToArray());
            }
        }

        // Devuelve el IV y el texto cifrado en Base64 (null queda null).
        public static string Cifrar(string texto)
        {
            if (texto == null) return null;
            var datos = Encoding.UTF8.GetBytes(texto);
            byte[] iv;
            using (var hmac = new HMACSHA256(ClaveIv)) iv = hmac.ComputeHash(datos).Take(16).ToArray();
            using (var aes = Aes.Create())
            using (var cifrador = aes.CreateEncryptor(ClaveAes, iv))
            {
                return Convert.ToBase64String(iv.Concat(cifrador.TransformFinalBlock(datos, 0, datos.Length)).ToArray());
            }
        }

        // El correo se guarda y se busca en minúsculas, sin espacios.
        public static string CifrarCorreo(string correo)
        {
            return correo == null ? null : Cifrar(correo.Trim().ToLowerInvariant());
        }

        // Si el valor no está cifrado (por ejemplo, alguien lo cargó a mano en la base), se devuelve tal cual.
        public static string Descifrar(string cifrado)
        {
            if (cifrado == null) return null;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(cifrado); }
            catch (FormatException) { return cifrado; }
            if (bytes.Length < 32 || bytes.Length % 16 != 0) return cifrado;
            try
            {
                using (var aes = Aes.Create())
                using (var descifrador = aes.CreateDecryptor(ClaveAes, bytes.Take(16).ToArray()))
                {
                    return Encoding.UTF8.GetString(descifrador.TransformFinalBlock(bytes, 16, bytes.Length - 16));
                }
            }
            catch (CryptographicException) { return cifrado; }
        }
    }
}
