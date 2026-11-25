using System;

namespace LogTech.BE
{
    // Una tarjeta guardada (CU-005-025). No tiene el número completo ni el código de seguridad:
    // la pasarela de pago la representa con un token.
    public sealed class TarjetaBE
    {
        public int IdTarjeta { get; set; }
        public int IdUsuario { get; set; }
        public string Titular { get; set; }
        public string Marca { get; set; }
        public string Ultimos4 { get; set; }
        public string Vencimiento { get; set; }   // MM/AA
        public string Token { get; set; }
        public DateTime FechaAlta { get; set; }

        public string Descripcion { get { return Marca + " terminada en " + Ultimos4; } }
    }
}
