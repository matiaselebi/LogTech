using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net.Mail;
using System.Web;
using System.Xml.Linq;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-008-039 Generar Factura Corporativa: las facturas electrónicas de lo que cobra LogTech. La estrategia de cada
    // servicio (patrón Strategy) dice qué se factura; esta clase (el contexto) completa los datos fiscales, pide el
    // CAE a ARCA (simulado), arma el PDF y se lo manda al cliente por correo.
    public sealed class FacturacionBLL
    {
        private const int PuntoVenta = 1;
        private const decimal AlicuotaIva = 0.21m;
        // Datos fiscales de LogTech para el encabezado de la factura.
        private const string Emisor = "LogTech S.A.";
        private const string CuitEmisor = "30718502345";
        private const string DomicilioEmisor = "Av. Corrientes 1234, CABA";
        private const string InicioActividades = "01/09/2026";
        private static readonly CultureInfo Argentina = new CultureInfo("es-AR");

        private readonly FacturaDAL facturas = new FacturaDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        // Arma la factura con la estrategia del servicio y la manda a autorizar. Devuelve null si no hay nada para
        // facturar (por ejemplo, un viaje sin comisión ni remito).
        public FacturaBE Emitir(IEstrategiaFacturacion estrategia, int idUsuario)
        {
            var cliente = usuarios.ObtenerPorId(idUsuario);
            var ahora = DateTime.Now;
            var factura = new FacturaBE
            {
                IdUsuario = idUsuario,
                Fecha = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond)),   // la base guarda sin milisegundos
                Tipo = cliente.CondicionIva == "Responsable inscripto" ? "A" : "B",
                PuntoVenta = PuntoVenta,
                Cliente = string.IsNullOrEmpty(cliente.RazonSocial) ? cliente.Nombre + " " + cliente.Apellido : cliente.RazonSocial,
                CuitCliente = cliente.CuitCuil,
                CondicionIva = cliente.CondicionIva,
                Estado = "Pendiente de CAE"
            };
            estrategia.Calcular(factura);
            if (factura.PrecioUnitario <= 0) return null;

            // Los precios incluyen el IVA: se separa el neto. En la factura A se discrimina; en la B se muestra como
            // «IVA contenido» (Ley 27.743).
            factura.Total = factura.Cantidad * factura.PrecioUnitario;
            factura.Neto = Math.Round(factura.Total / (1 + AlicuotaIva), 2);
            factura.Iva = factura.Total - factura.Neto;
            factura.IdFactura = facturas.Alta(factura);
            Autorizar(factura, cliente.Correo);
            return factura;
        }

        // Rutina diaria (ERR022): vuelve a pedirle a ARCA el CAE de las facturas que quedaron pendientes.
        public void ReintentarPendientes()
        {
            foreach (var factura in facturas.ListarPendientes())
                Autorizar(factura, usuarios.ObtenerPorId(factura.IdUsuario).Correo);
        }

        public List<FacturaBE> ListarDelUsuario(int idUsuario)
        {
            return facturas.ListarDelUsuario(idUsuario);
        }

        // Las del mes, para el administrador.
        public List<FacturaBE> ListarDelMes(DateTime mes)
        {
            var desde = new DateTime(mes.Year, mes.Month, 1);
            return facturas.Listar(desde, desde.AddMonths(1));
        }

        // El PDF de una factura emitida, solo para su dueño o el administrador. null si no corresponde.
        public byte[] ObtenerPdf(int idFactura, int idUsuario, bool esAdministrador, string usuario)
        {
            var factura = facturas.Obtener(idFactura);
            if (factura == null || factura.Estado != "Emitida" || (factura.IdUsuario != idUsuario && !esAdministrador)) return null;
            bitacora.Registrar(idUsuario, usuario, "Descarga de la factura " + factura.Comprobante, "Informativa");
            return ArmarPdf(factura);
        }

        public static string NombrePdf(int idFactura)
        {
            return "Factura-LogTech-" + idFactura + ".pdf";
        }

        // ReporteFacturacion.xml (anexo 12.5): las facturas del mes, para importarlas en el sistema contable.
        // Devuelve null si el mes no tiene facturas.
        public byte[] ExportarXml(DateTime mes, int idUsuario, string usuario)
        {
            var lista = ListarDelMes(mes);
            if (lista.Count == 0) return null;
            var raiz = new XElement("FacturacionMensual",
                new XAttribute("Periodo", mes.ToString("yyyy-MM")), new XAttribute("Generado", DateTime.Now.ToString("s")));
            decimal neto = 0, iva = 0, total = 0;
            foreach (var factura in lista)
            {
                raiz.Add(new XElement("Factura",
                    new XElement("IdFactura", factura.IdFactura),
                    new XElement("Comprobante", factura.Comprobante),
                    new XElement("Fecha", factura.Fecha.ToString("s")),
                    new XElement("IdUsuario", factura.IdUsuario),
                    new XElement("Cliente", factura.Cliente),
                    new XElement("CondicionIva", factura.CondicionIva),
                    new XElement("Servicio", factura.Servicio),
                    new XElement("Neto", factura.Neto),
                    new XElement("IVA", factura.Iva),
                    new XElement("MontoTotal", factura.Total),
                    new XElement("CAE", factura.Cae ?? string.Empty),
                    new XElement("Estado", factura.Estado)));
                neto += factura.Neto;
                iva += factura.Iva;
                total += factura.Total;
            }
            raiz.Add(new XElement("Totales", new XAttribute("Cantidad", lista.Count),
                new XAttribute("Neto", neto), new XAttribute("IVA", iva), new XAttribute("MontoTotal", total)));
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), raiz);
            bitacora.Registrar(idUsuario, usuario, "Exportación de ReporteFacturacion.xml de " + mes.ToString("MM/yyyy")
                + " (" + lista.Count + " facturas)", "Informativa");
            return System.Text.Encoding.UTF8.GetBytes(xml.Declaration + Environment.NewLine + xml);
        }

        public static string Pesos(decimal monto)
        {
            return "$ " + monto.ToString("N2", Argentina);
        }

        // Pide el CAE con el número que sigue. Si ARCA no responde, la factura queda pendiente para la rutina diaria.
        private void Autorizar(FacturaBE factura, string correo)
        {
            var numero = facturas.ProximoNumero(factura.Tipo, factura.PuntoVenta);
            DateTime vencimiento;
            var cae = ArcaServicio.SolicitarCae(factura.Tipo, factura.PuntoVenta, numero, factura.Total, out vencimiento);
            if (cae == null)
            {
                bitacora.Registrar(factura.IdUsuario, "sistema", "ARCA no respondió: la factura de " + factura.Servicio.ToLower()
                    + " de " + factura.Cliente + " queda pendiente de CAE", "Advertencia");
                return;
            }
            if (!facturas.Autorizar(factura.IdFactura, numero, cae, vencimiento)) return;
            factura.Numero = numero;
            factura.Cae = cae;
            factura.VencimientoCae = vencimiento;
            factura.Estado = "Emitida";
            bitacora.Registrar(factura.IdUsuario, "sistema", "Factura " + factura.Comprobante + " emitida a " + factura.Cliente
                + " por " + Pesos(factura.Total) + " (" + factura.Servicio + ")", "Informativa");
            Enviar(factura, correo);
        }

        // Como las notificaciones, sale por correo si en Web.config está AvisosPorCorreo = true. El correo es un extra:
        // si falla, la factura igual quedó emitida y se descarga desde la sección Facturas.
        private static void Enviar(FacturaBE factura, string correo)
        {
            if (ConfigurationManager.AppSettings["AvisosPorCorreo"] != "true") return;
            var cuerpo = "<p>Hola, " + HttpUtility.HtmlEncode(factura.Cliente) + ".</p><p>Te enviamos la factura "
                + factura.Comprobante + " de LogTech por " + Pesos(factura.Total) + " (" + HttpUtility.HtmlEncode(factura.Detalle)
                + ").</p><p>También la podés descargar desde la sección Facturas de tu panel.</p>";
            try { CorreoServicio.Enviar(correo, "LogTech: factura " + factura.Comprobante, cuerpo, ArmarPdf(factura), NombrePdf(factura.IdFactura)); }
            catch (SmtpException) { }
            catch (InvalidOperationException) { }
        }

        // La factura en PDF, con el diseño de una factura electrónica argentina: tipo de comprobante en el centro,
        // emisor, cliente, detalle, totales y CAE.
        private static byte[] ArmarPdf(FacturaBE f)
        {
            var pdf = new DocumentoPdf();
            // Encabezado: emisor a la izquierda, comprobante a la derecha y la letra en el medio.
            pdf.Rectangulo(40, 40, 515, 125);
            pdf.Rectangulo(277, 40, 41, 42);
            pdf.Texto(288, 70, f.Tipo, 26, true);
            pdf.Texto(283, 79, f.Tipo == "A" ? "COD. 01" : "COD. 06", 6, true);
            pdf.Linea(297.5f, 82, 297.5f, 165);
            pdf.Texto(50, 68, Emisor, 18, true);
            pdf.Texto(50, 104, "Razón social: " + Emisor, 8);
            pdf.Texto(50, 117, "Domicilio comercial: " + DomicilioEmisor, 8);
            pdf.Texto(50, 130, "Condición frente al IVA: Responsable inscripto", 8);
            pdf.Texto(330, 68, "FACTURA", 18, true);
            pdf.Texto(330, 94, "Punto de venta: " + f.PuntoVenta.ToString("D5") + "     Comp. Nro: " + f.Numero.Value.ToString("D8"), 9, true);
            pdf.Texto(330, 108, "Fecha de emisión: " + f.Fecha.ToString("dd/MM/yyyy"), 8);
            pdf.Texto(330, 121, "CUIT: " + Cuit(CuitEmisor), 8);
            pdf.Texto(330, 134, "Ingresos Brutos: " + Cuit(CuitEmisor), 8);
            pdf.Texto(330, 147, "Fecha de inicio de actividades: " + InicioActividades, 8);

            // Cliente.
            pdf.Rectangulo(40, 173, 515, 46);
            pdf.Texto(50, 190, "CUIT: " + Cuit(f.CuitCliente), 8);
            pdf.Texto(220, 190, "Apellido y nombre / Razón social: " + f.Cliente, 8);
            pdf.Texto(50, 207, "Condición frente al IVA: " + f.CondicionIva, 8);
            pdf.Texto(220, 207, "Servicio: " + f.Servicio, 8);

            // Detalle: una línea por servicio (el texto largo se corta en renglones).
            pdf.Rectangulo(40, 228, 515, 18);
            pdf.Texto(46, 240, "Descripción", 8, true);
            pdf.TextoDerecha(400, 240, "Cantidad", 8, true);
            pdf.TextoDerecha(475, 240, "Precio unit.", 8, true);
            pdf.TextoDerecha(549, 240, "Subtotal", 8, true);
            float y = 262;
            pdf.TextoDerecha(400, y, f.Cantidad.ToString(), 8);
            pdf.TextoDerecha(475, y, Pesos(f.PrecioUnitario), 8);
            pdf.TextoDerecha(549, y, Pesos(f.Total), 8);
            // El espacio de «$ 1.500,00» no se corta (espacio duro): el signo queda pegado al importe.
            foreach (var renglon in Renglones(f.Detalle.Replace("$ ", "$\u00A0"), 62))
            {
                pdf.Texto(46, y, renglon, 8);
                y += 12;
            }
            pdf.Linea(40, y, 555, y);

            // Totales.
            pdf.Rectangulo(40, 620, 515, 80);
            if (f.Tipo == "A")
            {
                pdf.Texto(330, 642, "Importe neto gravado:", 9);
                pdf.TextoDerecha(549, 642, Pesos(f.Neto), 9);
                pdf.Texto(330, 660, "IVA 21 %:", 9);
                pdf.TextoDerecha(549, 660, Pesos(f.Iva), 9);
            }
            else
            {
                pdf.Texto(50, 642, "Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)", 8, true);
                pdf.Texto(50, 656, "IVA contenido: " + Pesos(f.Iva), 8);
                pdf.Texto(330, 660, "Subtotal:", 9);
                pdf.TextoDerecha(549, 660, Pesos(f.Total), 9);
            }
            pdf.Texto(330, 685, "Importe total:", 11, true);
            pdf.TextoDerecha(549, 685, Pesos(f.Total), 11, true);

            // Pie: el CAE que devolvió ARCA.
            pdf.Linea(40, 720, 555, 720);
            pdf.Texto(40, 740, "Comprobante autorizado", 9, true);
            pdf.Texto(40, 753, "ARCA simulado: documento de demostración del TFI, sin validez fiscal.", 7);
            pdf.TextoDerecha(555, 740, "CAE N°: " + f.Cae, 9, true);
            pdf.TextoDerecha(555, 753, "Fecha de vencimiento del CAE: " + f.VencimientoCae.Value.ToString("dd/MM/yyyy"), 8);
            return pdf.Generar();
        }

        // 30718502345 -> 30-71850234-5
        private static string Cuit(string numero)
        {
            return numero.Length == 11 ? numero.Substring(0, 2) + "-" + numero.Substring(2, 8) + "-" + numero.Substring(10) : numero;
        }

        // Corta el texto en renglones de hasta «largo» letras, sin partir palabras.
        private static List<string> Renglones(string texto, int largo)
        {
            var renglones = new List<string>();
            var actual = string.Empty;
            foreach (var palabra in texto.Split(' '))
            {
                if (actual.Length > 0 && actual.Length + 1 + palabra.Length > largo)
                {
                    renglones.Add(actual);
                    actual = string.Empty;
                }
                actual = actual.Length == 0 ? palabra : actual + " " + palabra;
            }
            if (actual.Length > 0) renglones.Add(actual);
            return renglones;
        }
    }
}
