using System;

namespace LogTech.BE
{
    // Factura electrónica de LogTech (CU-008-039). Los precios incluyen el IVA (21 %).
    public sealed class FacturaBE
    {
        public int IdFactura { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }             // A (cliente responsable inscripto) o B
        public int PuntoVenta { get; set; }
        public int? Numero { get; set; }             // se asigna cuando ARCA la autoriza
        public string Servicio { get; set; }         // Comisión por viaje, Flota SaaS o Premium transportista
        public string Detalle { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public string Cliente { get; set; }          // razón social o nombre y apellido
        public string CuitCliente { get; set; }
        public string CondicionIva { get; set; }
        public int? IdViaje { get; set; }
        public int? IdSuscripcion { get; set; }
        public string Cae { get; set; }
        public DateTime? VencimientoCae { get; set; }
        public string Estado { get; set; }           // Emitida o Pendiente de CAE

        // Como se imprime: «A 00001-00000012».
        public string Comprobante
        {
            get { return Tipo + " " + PuntoVenta.ToString("D5") + "-" + (Numero.HasValue ? Numero.Value.ToString("D8") : "pendiente"); }
        }
    }
}
