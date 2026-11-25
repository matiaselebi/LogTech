using System;

namespace LogTech.BE
{
    // Clave de API activa de un dador (sin la clave: solo se guarda su SHA-256).
    public sealed class ClaveApiBE
    {
        public int IdClave { get; set; }
        public int IdUsuario { get; set; }
        public string Ultimos4 { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimoUso { get; set; }
    }
}
