using System;

namespace LogTech.BE
{
    public sealed class UsuarioBE
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Correo { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Telefono { get; set; }
        public string CuitCuil { get; set; }
        public string RazonSocial { get; set; }
        // Responsable inscripto, Monotributista, Exento o Consumidor final: define si la factura es A o B.
        public string CondicionIva { get; set; }
        public string NumeroLicencia { get; set; }
        public string Rol { get; set; }
        public string Idioma { get; set; }
        public string ClaveHash { get; set; }
        public string ClaveSalt { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public string Estado { get; set; }
    }
}
