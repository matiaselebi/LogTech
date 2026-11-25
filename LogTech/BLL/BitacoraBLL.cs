using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Hosting;
using System.Xml;
using LogTech.BE;
using LogTech.DAL;

namespace LogTech.BLL
{
    // La tabla BitacoraEventos guarda solo los eventos de los últimos 30 días; los más viejos se pasan a
    // App_Data/BitacoraHistorica.xml, con el mismo formato que la exportación (anexo 12.5).
    public sealed class BitacoraBLL
    {
        private const int DiasEnTabla = 30;
        private static readonly object bloqueo = new object();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();

        private static string RutaHistorial
        {
            get { return HostingEnvironment.MapPath("~/App_Data/BitacoraHistorica.xml"); }
        }

        public void Registrar(int? idUsuario, string usuario, string descripcion, string criticidad)
        {
            bitacora.Registrar(idUsuario, usuario, descripcion, criticidad);
        }

        public IList<EventoBitacoraBE> ObtenerPagina(int pagina, int cantidad, FiltroBitacora filtro, out int total)
        {
            if (!filtro.Historial) return bitacora.ObtenerPagina(pagina, cantidad, filtro, out total);

            // El historial XML guarda el nombre de usuario y no el ID: si se busca por ID, se busca su nombre.
            if (BitacoraDAL.PorId(filtro))
                filtro = new FiltroBitacora { Desde = filtro.Desde, Hasta = filtro.Hasta, Criticidad = filtro.Criticidad, Historial = true,
                    Usuario = usuarios.ObtenerNombreUsuario(int.Parse(filtro.Usuario.Trim())), UsuarioExacto = true };
            var encontrados = new List<EventoBitacoraBE>();
            lock (bloqueo)
            {
                foreach (var evento in LeerHistorial())
                    if (Cumple(evento, filtro)) encontrados.Add(evento);
            }
            total = encontrados.Count;
            var desde = Math.Min(pagina * cantidad, total);
            return encontrados.GetRange(desde, Math.Min(cantidad, total - desde));
        }

        // Pasa al historial XML los eventos de más de 30 días y los borra de la tabla. Se hace al abrir la bitácora.
        public void ArchivarViejos(int? idUsuario, string usuario)
        {
            var limite = DateTime.Today.AddDays(-DiasEnTabla);
            lock (bloqueo)
            {
                var viejos = bitacora.ListarAnteriores(limite);
                if (viejos.Count == 0) return;

                // Si se restauró un backup pueden volver a la tabla eventos que ya estaban en el historial: no se repiten.
                var historial = LeerHistorial();
                var yaArchivados = new HashSet<string>();
                foreach (var evento in historial) yaArchivados.Add(Clave(evento));
                foreach (var evento in viejos)
                    if (!yaArchivados.Contains(Clave(evento))) historial.Add(evento);
                historial.Sort((a, b) => b.Fecha.CompareTo(a.Fecha));

                // Primero se guarda el XML y después se borra de la tabla: si algo falla no se pierde ningún evento.
                File.WriteAllBytes(RutaHistorial, GenerarXml(historial, usuario));
                bitacora.BorrarAnteriores(limite, viejos[0].IdEvento);
                bitacora.Registrar(idUsuario, usuario,
                    "Se pasaron " + viejos.Count + " eventos de más de " + DiasEnTabla + " días al historial XML", "Informativa");
            }
        }

        // HistorialAuditoria_Seguridad.xml (anexo 12.5): todos los eventos que cumplen los filtros de la pantalla,
        // del más nuevo al más viejo. Devuelve null si no hay eventos para exportar.
        public byte[] ExportarXml(FiltroBitacora filtro, int? idUsuario, string usuario)
        {
            int total;
            var eventos = ObtenerPagina(0, int.MaxValue, filtro, out total);
            if (eventos.Count == 0) return null;
            var xml = GenerarXml(eventos, usuario);

            // R1: la exportación queda en la bitácora.
            bitacora.Registrar(idUsuario, usuario, "Exportación de la bitácora a XML (" + eventos.Count + " eventos)", "Informativa");
            return xml;
        }

        private static byte[] GenerarXml(IList<EventoBitacoraBE> eventos, string usuario)
        {
            var archivo = new MemoryStream();
            var opciones = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
            using (var xml = XmlWriter.Create(archivo, opciones))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("AuditoriaSeguridad");
                xml.WriteAttributeString("Generado", DateTime.Now.ToString("s"));
                xml.WriteAttributeString("Usuario", usuario);
                foreach (var evento in eventos)
                {
                    xml.WriteStartElement("Evento");
                    xml.WriteElementString("IdEvento", evento.IdEvento.ToString());
                    xml.WriteElementString("Fecha", evento.Fecha.ToString("s"));
                    xml.WriteElementString("Usuario", evento.Usuario);
                    xml.WriteElementString("Descripcion", evento.Descripcion);
                    xml.WriteElementString("Criticidad", evento.Criticidad);
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            return archivo.ToArray();
        }

        private static List<EventoBitacoraBE> LeerHistorial()
        {
            var eventos = new List<EventoBitacoraBE>();
            if (!File.Exists(RutaHistorial)) return eventos;
            var documento = new XmlDocument();
            documento.Load(RutaHistorial);
            foreach (XmlNode nodo in documento.SelectNodes("/AuditoriaSeguridad/Evento"))
            {
                eventos.Add(new EventoBitacoraBE
                {
                    IdEvento = long.Parse(nodo["IdEvento"].InnerText),
                    Fecha = DateTime.Parse(nodo["Fecha"].InnerText, CultureInfo.InvariantCulture),
                    Usuario = nodo["Usuario"].InnerText,
                    Descripcion = nodo["Descripcion"].InnerText,
                    Criticidad = nodo["Criticidad"].InnerText
                });
            }
            return eventos;
        }

        // Los mismos filtros que aplica BitacoraDAL a la tabla.
        private static bool Cumple(EventoBitacoraBE evento, FiltroBitacora filtro)
        {
            if (filtro.Desde.HasValue && evento.Fecha < filtro.Desde.Value) return false;
            if (filtro.Hasta.HasValue && evento.Fecha >= filtro.Hasta.Value.Date.AddDays(1)) return false;
            if (filtro.UsuarioExacto && !string.Equals(evento.Usuario, filtro.Usuario, StringComparison.OrdinalIgnoreCase)) return false;
            if (!filtro.UsuarioExacto && !string.IsNullOrWhiteSpace(filtro.Usuario)
                && evento.Usuario.IndexOf(filtro.Usuario.Trim(), StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (!string.IsNullOrWhiteSpace(filtro.Criticidad) && evento.Criticidad != filtro.Criticidad) return false;
            return true;
        }

        // Un evento se reconoce por su número y su fecha (después de restaurar un backup los números se repiten).
        private static string Clave(EventoBitacoraBE evento)
        {
            return evento.IdEvento + "|" + evento.Fecha.ToString("s");
        }
    }
}
