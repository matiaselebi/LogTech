using System;
using System.Xml.Serialization;

namespace LogTech.BE
{
    // Comprobante de entrega. Token = SHA-256 de su contenido: si alguien cambia un dato,
    // el token deja de coincidir y el remito se muestra como alterado.
    [XmlRoot("RemitoDigital")]
    public sealed class RemitoBE
    {
        public int IdRemito { get; set; }
        public int IdViaje { get; set; }
        public string Receptor { get; set; }
        public string DniReceptor { get; set; }
        [XmlIgnore] public string Foto { get; set; }   // ruta interna del archivo: no va en RemitoDigital.xml
        public DateTime FechaEmision { get; set; }
        public string Token { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Mercaderia { get; set; }
        public string Transportista { get; set; }
        public string Dador { get; set; }
        public bool Valido { get; set; }
    }
}
