using System;

namespace LogTech.BE
{
    public sealed class DocumentoBE
    {
        public int IdDocumento { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        // Si es de un vehículo (seguro, VTV, cédula verde) o de un conductor de la flota (DNI, licencia, antecedentes).
        public int? IdVehiculo { get; set; }
        public string Patente { get; set; }
        public int? IdConductor { get; set; }
        public string Conductor { get; set; }
        public string Tipo { get; set; }
        public string Archivo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaCarga { get; set; }
        // Solo al subirlo: el archivo elegido en la pantalla (se guarda en disco y queda la ruta en Archivo).
        public byte[] Contenido { get; set; }
        public string NombreArchivo { get; set; }

        // Para las grillas: de qué vehículo o conductor es («-» si es de la cuenta).
        public string DeQuien { get { return Patente ?? Conductor ?? "-"; } }
    }
}
