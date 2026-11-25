using System;
using System.Text.RegularExpressions;

namespace LogTech.Servicios
{
    // Pasarela de pago SIMULADA (decisión de la entrega final): valida y tokeniza tarjetas (R21), cobra,
    // devuelve, transfiere a un CBU o alias, cobra en puntos de pago (Pago Fácil, Rapipago) y acredita adelantos de factoring. Responde como lo haría una pasarela real, pero no mueve plata.
    // Para usar una real (Mercado Pago, Payway) se reemplaza solo esta clase: el resto del sistema no cambia.
    public static class PasarelaPagoServicio
    {
        // Número de prueba que la pasarela rechaza (ERR023), como hacen las pasarelas reales en su modo de prueba.
        public const string TarjetaRechazada = "4000000000000002";

        // Visa o Mastercard (16 números) según los primeros números; null si es de otra marca.
        public static string Marca(string numero)
        {
            if (numero == null) return null;
            if (Regex.IsMatch(numero, @"^4\d{15}$")) return "Visa";
            if (Regex.IsMatch(numero, @"^(5[1-5]\d{14}|2(2[2-9]|[3-6]\d|7[01])\d{13}|2720\d{12})$")) return "Mastercard";
            return null;
        }

        // Algoritmo de Luhn: el dígito verificador que tienen todas las tarjetas.
        public static bool LuhnValido(string numero)
        {
            if (numero == null || !Regex.IsMatch(numero, @"^\d{13,19}$")) return false;
            var suma = 0;
            for (var i = 0; i < numero.Length; i++)
            {
                var digito = numero[numero.Length - 1 - i] - '0';
                if (i % 2 == 1)
                {
                    digito *= 2;
                    if (digito > 9) digito -= 9;
                }
                suma += digito;
            }
            return suma % 10 == 0;
        }

        // R21: la pasarela valida la tarjeta con el banco y devuelve un token que la representa
        // (el sistema nunca guarda el número completo). null y el motivo si la rechaza.
        public static string Tokenizar(string numero, out string error)
        {
            error = null;
            if (numero == TarjetaRechazada)
            {
                error = "El banco rechazó la tarjeta. Probá con otra.";
                return null;
            }
            return Guid.NewGuid().ToString("N");
        }

        // Cobra a la tarjeta del token. Devuelve el código de autorización.
        public static string Cobrar(string token, decimal monto)
        {
            return Comprobante("AUT");
        }

        // Devuelve a la tarjeta lo cobrado. Devuelve el código de la devolución.
        public static string Devolver(string token, decimal monto)
        {
            return Comprobante("DEV");
        }

        // Transfiere a un CBU o alias. Devuelve el código de la transferencia.
        public static string Transferir(string cbuOAlias, decimal monto)
        {
            return Comprobante("TRF");
        }

        // El dador paga en efectivo en Pago Fácil o Rapipago con el código del viaje. Devuelve el código del pago.
        public static string CobrarEnPuntoDePago(string codigoPago, decimal monto)
        {
            return Comprobante("PF");
        }

        // Si el pago en el punto de pago no se pudo registrar, el punto de pago le devuelve la plata al dador.
        public static string DevolverEnPuntoDePago(string codigoPago, decimal monto)
        {
            return Comprobante("PFD");
        }

        // La entidad de factoring acredita el adelanto en la billetera del transportista.
        public static string AcreditarAdelanto(decimal monto)
        {
            return Comprobante("FAC");
        }

        // Si el adelanto no se pudo registrar, la entidad de factoring lo anula.
        public static string AnularAdelanto(decimal monto)
        {
            return Comprobante("FAA");
        }

        private static string Comprobante(string prefijo)
        {
            return prefijo + "-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
        }
    }
}
