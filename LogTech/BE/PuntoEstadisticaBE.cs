namespace LogTech.BE
{
    // Cómo se agrupan las estadísticas del tablero.
    public enum PeriodoEstadistica
    {
        Diario,
        Mensual,
        Anual
    }

    // Tipo de carga de un viaje según su peso (los rangos están en EstadisticaBLL).
    public enum TipoCarga
    {
        Todas,
        Ligera,
        Media,
        Pesada
    }

    // Un punto de un gráfico: un día, un mes o un año con su cantidad y su plata.
    public sealed class PuntoEstadisticaBE
    {
        public string Clave { get; set; }      // 2026-10-07, 2026-10 o 2026: para unir con lo que trae la base
        public string Periodo { get; set; }    // texto que se muestra: 07/10, 10/2026 o 2026
        public int Cantidad { get; set; }
        public decimal Monto { get; set; }
    }
}
