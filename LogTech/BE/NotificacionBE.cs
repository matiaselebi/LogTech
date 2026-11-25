using System;

namespace LogTech.BE
{
    public sealed class NotificacionBE
    {
        public DateTime Fecha { get; set; }
        public string Texto { get; set; }
        public bool Leida { get; set; }
    }
}
