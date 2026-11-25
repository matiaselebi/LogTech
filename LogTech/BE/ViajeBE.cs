using System;

namespace LogTech.BE
{
    // Una oferta de carga publicada por un dador. Cuando un transportista o una flota la
    // acepta pasa a ser un viaje; la misma fila sigue todo el recorrido hasta la entrega y su pago.
    public sealed class ViajeBE
    {
        public int IdViaje { get; set; }
        public DateTime FechaPedido { get; set; }
        public int? IdDador { get; set; }
        public string Dador { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Mercaderia { get; set; }
        public decimal PesoToneladas { get; set; }
        public DateTime FechaCarga { get; set; }
        public decimal TarifaAcordada { get; set; }
        public decimal Comision { get; set; }
        public string Estado { get; set; }
        public int? IdTransportista { get; set; }
        public string Transportista { get; set; }
        public int? IdVehiculo { get; set; }
        public string Patente { get; set; }
        public int? IdConductor { get; set; }
        public string Conductor { get; set; }
        public bool TieneRemito { get; set; }
        public bool Calificado { get; set; }
        // Solo en el match: kilómetros entre el usuario y el origen de la carga (null = sin ubicación).
        public double? DistanciaKm { get; set; }

        // Pago (CU-005-025). TarifaAcordada es el precio que publica el dador, con todo incluido.
        public string MedioPago { get; set; }          // Efectivo o Tarjeta
        public int? IdTarjeta { get; set; }
        public decimal DescuentoDador { get; set; }    // el 7 % en los 3 primeros viajes de un dador nuevo
        public decimal CargoRemito { get; set; }       // el remito digital ($ 1.500)
        // Pendiente, Retenido, En disputa, Liberado, Devuelto, A cobrar (efectivo entregado sin pagar) o Adelantado (factoring).
        public string EstadoPago { get; set; }
        public DateTime? FechaRemito { get; set; }

        // Efectivo: el dador tiene 30 días desde el remito para pagar en Pago Fácil o Rapipago, con este código.
        public DateTime? VencePago { get { return FechaRemito.HasValue ? FechaRemito.Value.Date.AddDays(30) : (DateTime?)null; } }
        public string CodigoPago { get { return "7410" + IdViaje.ToString("D8"); } }

        // Lo que paga el dador.
        public decimal PrecioDador { get { return TarifaAcordada - DescuentoDador; } }

        // Lo que cobra el transportista: el precio menos la comisión, el descuento del dador y el remito.
        public decimal NetoTransportista { get { return TarifaAcordada - Comision - DescuentoDador - CargoRemito; } }

        // Lo que se queda LogTech: la comisión y el remito.
        public decimal ParaLogTech { get { return Comision + CargoRemito; } }
    }
}
