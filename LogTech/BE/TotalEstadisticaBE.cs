namespace LogTech.BE
{
    // Un total con nombre: una porción de un gráfico de torta o el total de una sección
    // del tablero (por ejemplo "Flota SaaS": 626 suscriptores que pagan $ X por mes).
    public sealed class TotalEstadisticaBE
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Monto { get; set; }
        public decimal Comision { get; set; }   // solo viajes: la parte que cobra LogTech
    }
}
