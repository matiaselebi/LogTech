using System;

namespace LogTech.BE
{
    // Chofer de una flota. No es usuario del sistema: lo carga el administrador de flota.
    public sealed class ConductorBE
    {
        public int IdConductor { get; set; }
        public int IdFlota { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string NumeroLicencia { get; set; }
        public string Telefono { get; set; }
        public string Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        // Tiene un viaje asignado o en curso (R8).
        public bool EnViaje { get; set; }
        // Semáforo de su documentación, como el de los vehículos: solo en verde puede hacer viajes.
        public string Semaforo { get; set; }
        public string MotivoSemaforo { get; set; }
        public string NombreCompleto { get { return Apellido + ", " + Nombre; } }
    }
}
