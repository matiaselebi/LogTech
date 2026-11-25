namespace LogTech.BE
{
    // Resultado del cotizador de fletes (WS_CotizadorFletes y «Calcular tarifa sugerida» al publicar una carga).
    public sealed class CotizacionBE
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        public decimal PesoToneladas { get; set; }
        public decimal DistanciaKm { get; set; }          // estimada por ruta
        public decimal Tarifa { get; set; }               // lo que paga el dador, con todo incluido
        public decimal Comision { get; set; }             // 7 % para LogTech, incluido en la tarifa
        public decimal CargoRemito { get; set; }          // el remito digital, incluido en la tarifa
        public decimal NetoTransportista { get; set; }    // lo que cobra el transportista
    }
}
