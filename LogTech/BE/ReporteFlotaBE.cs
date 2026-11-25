using System;
using System.Collections.Generic;

namespace LogTech.BE
{
    // Reporte de la flota (CU-008-044 para el administrador de flota): los viajes entregados en el
    // período, lo que cobró la flota y el rendimiento de cada conductor. Puede ser de toda la flota o de un vehículo
    // (también lo usa «Ver detalles» del vehículo).
    public sealed class ReporteFlotaBE
    {
        public int Viajes { get; set; }
        public decimal Toneladas { get; set; }
        public decimal Kilometros { get; set; }
        public decimal Facturado { get; set; }      // lo que pagaron los dadores
        public decimal Comision { get; set; }       // la parte de LogTech: comisión (7 %) y remitos
        public decimal Neto { get; set; }           // lo que cobró la flota
        public int Incidencias { get; set; }
        public int EnCurso { get; set; }            // viajes en curso ahora
        public string ViajeEnCurso { get; set; }    // de un vehículo: el viaje que está haciendo («N° 15355 · Buenos Aires → La Plata, Asignado»)
        // Gastos del mes en LogTech: la Flota SaaS vigente y el seguro activo.
        public decimal GastoSuscripcion { get; set; }
        public decimal GastoSeguro { get; set; }
        public decimal GastosDelMes { get { return GastoSuscripcion + GastoSeguro; } }
        public IList<PuntoEstadisticaBE> PorPeriodo { get; set; }    // viajes y neto por día, mes o año
        public IList<RendimientoConductorBE> Conductores { get; set; }
    }

    // Una fila del reporte de la flota: lo que hizo un conductor en el período.
    public sealed class RendimientoConductorBE
    {
        public string Conductor { get; set; }
        public int Viajes { get; set; }
        public decimal Toneladas { get; set; }
        public decimal Kilometros { get; set; }
        public decimal Neto { get; set; }
        public int Incidencias { get; set; }
        public int EnCurso { get; set; }
        public DateTime? UltimoViaje { get; set; }
    }
}
