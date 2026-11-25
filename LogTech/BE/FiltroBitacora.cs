using System;

namespace LogTech.BE
{
    public sealed class FiltroBitacora
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Usuario { get; set; }
        public string Criticidad { get; set; }
        // true: busca en el historial XML (eventos de más de 30 días) en lugar de la tabla.
        public bool Historial { get; set; }
        // true: Usuario es el nombre completo (se buscó por ID en el historial), no una parte.
        public bool UsuarioExacto { get; set; }
    }
}
