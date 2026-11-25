using System;
using System.Collections.Generic;

namespace LogTech.BE
{
    // Lo que muestra el monitoreo de un viaje (CU-007-036): el viaje, su línea de tiempo y
    // el punto del mapa. Aviso: texto de ERR020 si no hay una ubicación reciente.
    public sealed class MonitoreoBE
    {
        public ViajeBE Viaje { get; set; }
        public List<EventoViajeBE> Eventos { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string Aviso { get; set; }
        // Solo para viajes en curso: cuándo llegaría a destino y cuántos km le faltan (estimado).
        public DateTime? EntregaEstimada { get; set; }
        public decimal KmRestantes { get; set; }
    }
}
