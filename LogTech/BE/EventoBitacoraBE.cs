using System;

namespace LogTech.BE
{
    public sealed class EventoBitacoraBE
    {
        public long IdEvento { get; set; }
        public DateTime Fecha { get; set; }
        // Null en los eventos del historial XML (ahí solo está el nombre de usuario) y en los del sistema.
        public int? IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Descripcion { get; set; }
        public string Criticidad { get; set; }
    }
}
