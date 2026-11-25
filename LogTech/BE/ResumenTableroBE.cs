namespace LogTech.BE
{
    // Números del mes actual que muestran las tarjetas del tablero.
    public sealed class ResumenTableroBE
    {
        public int SuscriptoresActivos { get; set; }
        public decimal IngresoMensualSuscriptores { get; set; }
        public int SuscripcionesDelMes { get; set; }
        public int ViajesDelMes { get; set; }
        public decimal MontoViajesDelMes { get; set; }
        public decimal ComisionesDelMes { get; set; }
        public int SegurosActivos { get; set; }           // Protección de carga vigente
        public decimal ComisionSeguros { get; set; }      // el 5 % de la prima de esos seguros, por mes
    }
}
