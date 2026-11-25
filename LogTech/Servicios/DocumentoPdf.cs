using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LogTech.Servicios
{
    // PDF de una hoja A4 armado a mano (formato PDF 1.4), sin librerías externas: alcanza para la factura, que es
    // texto y líneas. Un PDF es texto con objetos numerados (catálogo, páginas, la página, las fuentes y el
    // contenido) y al final una tabla (xref) con la posición en bytes de cada objeto.
    public sealed class DocumentoPdf
    {
        private const float AltoHoja = 842;   // A4: 595 x 842 puntos (1 punto = 1/72 de pulgada)
        // Las fuentes estándar del PDF (Helvetica) usan la codificación de Windows (WinAnsi): acentos y ñ incluidos.
        private static readonly Encoding Windows = Encoding.GetEncoding(1252);
        private readonly StringBuilder contenido = new StringBuilder();

        // x e y se miden desde arriba a la izquierda de la hoja (el PDF mide desde abajo: por eso AltoHoja - y).
        public void Texto(float x, float y, string texto, float tamano = 9, bool negrita = false)
        {
            contenido.AppendFormat(CultureInfo.InvariantCulture, "BT /{0} {1} Tf {2} {3} Td ({4}) Tj ET\n",
                negrita ? "F2" : "F1", tamano, x, AltoHoja - y, Escapar(texto));
        }

        // Alineado a la derecha (para los importes). En Helvetica los números miden 0,556 del tamaño de la letra y los
        // puntos, comas y espacios 0,278; para el resto alcanza con la misma medida que un número.
        public void TextoDerecha(float xDerecha, float y, string texto, float tamano = 9, bool negrita = false)
        {
            float ancho = 0;
            foreach (var letra in texto)
                ancho += (letra == '.' || letra == ',' || letra == ' ' ? 0.278f : 0.556f) * tamano;
            Texto(xDerecha - ancho, y, texto, tamano, negrita);
        }

        public void Linea(float x1, float y1, float x2, float y2)
        {
            contenido.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} m {2} {3} l S\n", x1, AltoHoja - y1, x2, AltoHoja - y2);
        }

        public void Rectangulo(float x, float y, float ancho, float alto)
        {
            contenido.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} re S\n", x, AltoHoja - y - alto, ancho, alto);
        }

        public byte[] Generar()
        {
            var flujo = contenido.ToString();
            var objetos = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
                "<< /Length " + Windows.GetByteCount(flujo) + " >>\nstream\n" + flujo + "endstream"
            };

            var pdf = new MemoryStream();
            var posiciones = new List<long>();
            Escribir(pdf, "%PDF-1.4\n");
            for (var i = 0; i < objetos.Length; i++)
            {
                posiciones.Add(pdf.Length);
                Escribir(pdf, (i + 1) + " 0 obj\n" + objetos[i] + "\nendobj\n");
            }
            var inicioTabla = pdf.Length;
            Escribir(pdf, "xref\n0 " + (objetos.Length + 1) + "\n0000000000 65535 f \n");
            foreach (var posicion in posiciones) Escribir(pdf, posicion.ToString("D10") + " 00000 n \n");
            Escribir(pdf, "trailer\n<< /Size " + (objetos.Length + 1) + " /Root 1 0 R >>\nstartxref\n" + inicioTabla + "\n%%EOF\n");
            return pdf.ToArray();
        }

        private static void Escribir(Stream pdf, string texto)
        {
            var bytes = Windows.GetBytes(texto);
            pdf.Write(bytes, 0, bytes.Length);
        }

        // En el PDF los textos van entre paréntesis: la barra y los paréntesis del texto se escapan con una barra.
        private static string Escapar(string texto)
        {
            return (texto ?? string.Empty).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}
