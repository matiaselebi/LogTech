using System;

namespace LogTech.BE
{
    [Serializable]   // para guardarlo en el ViewState de DetalleVehiculoCard
    public sealed class VehiculoBE
    {
        public int IdVehiculo { get; set; }
        public int IdPropietario { get; set; }
        public string Propietario { get; set; }
        public string Patente { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal CapacidadToneladas { get; set; }
        public string TipoAcoplado { get; set; }
        public string Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        // Tiene un viaje asignado o en curso (R6).
        public bool EnViaje { get; set; }
        // Flota: el conductor fijo del camión, si tiene.
        public int? IdConductorDesignado { get; set; }
        public string ConductorDesignado { get; set; }
        // Semáforo de su documentación: "Verde" (habilitado), "Amarillo" (en revisión) o "Rojo" (falta, vencida o
        // rechazada). Solo un vehículo en verde puede hacer viajes. MotivoSemaforo dice por qué no está en verde.
        public string Semaforo { get; set; }
        public string MotivoSemaforo { get; set; }
        // Solo en el match del dador: puntaje promedio del dueño, última ubicación que
        // reportó el camión y su distancia al origen de la carga.
        public decimal? Reputacion { get; set; }
        public int CantidadCalificaciones { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public double? DistanciaKm { get; set; }

        public string Descripcion
        {
            get { return Patente + " · " + Marca + " " + Modelo + " (" + CapacidadToneladas.ToString("0.##") + " t)"; }
        }
    }
}
