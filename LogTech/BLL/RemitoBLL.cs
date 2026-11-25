using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using System.Globalization;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Remito digital (CU-004-022 y CU-004-023).
    public sealed class RemitoBLL
    {
        private readonly RemitoDAL remitos = new RemitoDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly ViajeBLL viajeBLL = new ViajeBLL();

        // Token = SHA-256 de "fecha|receptor|dni|foto". Si alguien cambia un dato en la base,
        // el token ya no coincide y el remito aparece como alterado.
        public static string CalcularToken(RemitoBE remito)
        {
            var contenido = remito.FechaEmision.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                + "|" + remito.Receptor + "|" + remito.DniReceptor + "|" + remito.Foto;
            return SeguridadServicio.CalcularHashSha256(contenido, string.Empty);
        }

        // CU-004-022. Devuelve null si salió bien, o el texto del error.
        public string Alta(int idViaje, string receptor, string dni, byte[] foto, string nombreFoto, int idUsuario, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdTransportista != idUsuario) return "El viaje no existe o no está a tu cargo.";

            receptor = (receptor ?? string.Empty).Trim();
            dni = (dni ?? string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            if (!Validador.EsNombre(receptor, 100) || !Validador.EsDni(dni) || foto == null || foto.Length == 0)
                return "Completá el nombre y el DNI de quien recibe la carga y adjuntá la foto del remito firmado.";
            // R9 (ERR011).
            if (!ArchivoServicio.EsPdfOJpg(nombreFoto, foto)) return "La foto del remito tiene que ser JPG (o un PDF) de hasta 5 MB.";
            // R10 (ERR013): la carga tiene que haber llegado a destino.
            if (viaje.Estado != "En destino")
                return "El viaje todavía no llegó a destino (estado: " + viaje.Estado + "). Actualizá el estado a \"En destino\" antes de emitir el remito.";

            var ahora = DateTime.Now;
            var remito = new RemitoBE
            {
                IdViaje = idViaje,
                Receptor = receptor,
                DniReceptor = dni,
                Foto = ArchivoServicio.Guardar(foto, nombreFoto, "Remitos"),
                // Sin milisegundos: es lo que guarda la base (DATETIME2(0)) y entra en el token.
                FechaEmision = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond))
            };
            remito.Token = CalcularToken(remito);
            if (!remitos.Alta(remito)) return "El viaje ya tiene remito o dejó de estar en destino.";

            viaje.Estado = "Entregado";
            viajeBLL.Notificar(viaje, "Carga entregada. Remito digital emitido: recibió " + receptor + ".", null, null, usuario);
            bitacora.Registrar(idUsuario, usuario, "Alta del remito digital del viaje N° " + idViaje, "Informativa");
            return null;
        }

        // CU-004-023. Solo lo ven el dador y el transportista del viaje; null si no le corresponde.
        public RemitoBE ConsultarDelViaje(int idViaje, int idUsuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || (viaje.IdDador != idUsuario && viaje.IdTransportista != idUsuario)) return null;
            var remito = remitos.ObtenerDelViaje(idViaje);
            if (remito != null) remito.Valido = CalcularToken(remito) == remito.Token;
            return remito;
        }

        // RemitoDigital.xml (anexo 12.5.3): copia del remito para entregar a terceros o guardar como respaldo ante una
        // disputa. ConsultarDelViaje ya controla que el remito sea del usuario: null si no le corresponde. El XML apunta a
        // su hoja de estilo (RemitoDigital.xslt) para verse como remito en un navegador.
        public byte[] ExportarXml(int idViaje, int idUsuario, string usuario)
        {
            var remito = ConsultarDelViaje(idViaje, idUsuario);
            if (remito == null) return null;
            bitacora.Registrar(idUsuario, usuario, "Exportación del remito del viaje N° " + idViaje + " a XML", "Informativa");
            return Serializar(remito);
        }

        // El mismo XML pasado por RemitoDigital.xslt (XslCompiledTransform): la página imprimible del remito.
        public string Imprimir(int idViaje, int idUsuario, string usuario)
        {
            var remito = ConsultarDelViaje(idViaje, idUsuario);
            if (remito == null) return null;
            var xslt = new XslCompiledTransform();
            xslt.Load(HostingEnvironment.MapPath("~/Xslt/RemitoDigital.xslt"));
            using (var lector = XmlReader.Create(new MemoryStream(Serializar(remito))))
            using (var html = new StringWriter())
            {
                xslt.Transform(lector, null, html);
                bitacora.Registrar(idUsuario, usuario, "Impresión del remito del viaje N° " + idViaje, "Informativa");
                return html.ToString();
            }
        }

        private static byte[] Serializar(RemitoBE remito)
        {
            var configuracion = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
            using (var archivo = new MemoryStream())
            {
                using (var escritor = XmlWriter.Create(archivo, configuracion))
                {
                    escritor.WriteStartDocument();
                    escritor.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"RemitoDigital.xslt\"");
                    new XmlSerializer(typeof(RemitoBE)).Serialize(escritor, remito);
                }
                return archivo.ToArray();
            }
        }

        public List<RemitoBE> Listar(int idUsuario, string texto)
        {
            var lista = remitos.ListarDelUsuario(idUsuario, (texto ?? string.Empty).Trim());
            foreach (var remito in lista) remito.Valido = CalcularToken(remito) == remito.Token;
            return lista;
        }
    }
}
