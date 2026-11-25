using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LogTech.Servicios
{
    // Reglas de formato que se repiten en varios formularios. Las usan las clases BLL
    // antes de guardar, así ningún dato inválido llega a la base aunque cambie la pantalla.
    public static class Validador
    {
        // Letras (con acentos), espacios, apóstrofo y guion. Ej.: "María José", "O'Connor".
        public static bool EsNombre(string texto, int largoMaximo)
        {
            return !string.IsNullOrWhiteSpace(texto) && texto.Length >= 2 && texto.Length <= largoMaximo
                && Regex.IsMatch(texto, @"^[\p{L}][\p{L} '\-]*$");
        }

        // Condición frente al IVA de la cuenta (define si la factura es A o B).
        public static bool EsCondicionIva(string texto)
        {
            return texto == "Responsable inscripto" || texto == "Monotributista" || texto == "Exento" || texto == "Consumidor final";
        }

        // Texto libre obligatorio con largo máximo (descripciones, mercadería, razón social).
        public static bool EsTexto(string texto, int largoMaximo)
        {
            return !string.IsNullOrWhiteSpace(texto) && texto.Trim().Length <= largoMaximo;
        }

        // 10 números: código de área sin el 0 y número sin el 15 (ej.: 1144445555).
        public static bool EsTelefono(string texto)
        {
            return texto != null && Regex.IsMatch(texto, @"^\d{10}$");
        }

        public static bool EsCorreo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Length > 100) return false;
            try { return new MailAddress(texto).Address == texto; }
            catch (FormatException) { return false; }
        }

        // Nombre de usuario: 4 a 50 letras, números, punto, guion o guion bajo.
        public static bool EsNombreUsuario(string texto)
        {
            return !string.IsNullOrEmpty(texto) && Regex.IsMatch(texto, @"^[a-z0-9._\-]{4,50}$");
        }

        // CUIT/CUIL: 11 números y el último es el dígito verificador (módulo 11).
        public static bool EsCuit(string numeros)
        {
            if (numeros == null || !Regex.IsMatch(numeros, @"^\d{11}$")) return false;
            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            var suma = 0;
            for (var i = 0; i < 10; i++)
                suma += (numeros[i] - '0') * pesos[i];
            var verificador = 11 - (suma % 11);
            if (verificador == 11) verificador = 0;
            if (verificador == 10) verificador = 9;
            return verificador == numeros[10] - '0';
        }

        // DNI: 7 u 8 números.
        public static bool EsDni(string texto)
        {
            return texto != null && Regex.IsMatch(texto, @"^\d{7,8}$");
        }

        // Patente argentina: formato Mercosur (AB123CD) o anterior (ABC123).
        public static bool EsPatente(string texto)
        {
            return texto != null && Regex.IsMatch(texto, @"^([A-Z]{2}\d{3}[A-Z]{2}|[A-Z]{3}\d{3})$");
        }

        // Retiro de la billetera: CBU de 22 números o alias de 6 a 20 letras, números, puntos o guiones.
        // ponytail: el CBU no controla sus dos dígitos verificadores; agregarlo si se conecta a un banco real.
        public static bool EsCbuOAlias(string texto)
        {
            return texto != null && (Regex.IsMatch(texto, @"^\d{22}$") || Regex.IsMatch(texto, @"^(?=.*[a-zA-Z])[a-zA-Z0-9.\-]{6,20}$"));
        }

        // Licencia de conducir: letras, números y guiones, de 4 a 40.
        public static bool EsLicencia(string texto)
        {
            return texto != null && Regex.IsMatch(texto, @"^[A-Za-z0-9\-]{4,40}$");
        }
    }
}
