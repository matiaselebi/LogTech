using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web.Hosting;

namespace LogTech.Servicios
{
    // Manda correos. Si hay un host SMTP real cargado en appSettings (claves CorreoSmtp*,
    // puestas en Web.Mail.config para que la contraseña no vaya al repo), lo usa. Si no,
    // cae en la carpeta de pickup de <system.net><mailSettings> (guarda un .eml en
    // App_Data/Mails, no manda nada real).
    public static class CorreoServicio
    {
        // adjunto y nombreAdjunto son opcionales (por ejemplo, el PDF de la factura).
        public static void Enviar(string destino, string asunto, string cuerpoHtml, byte[] adjunto = null, string nombreAdjunto = null)
        {
            var host = ConfigurationManager.AppSettings["CorreoSmtpHost"];
            string remitente;
            SmtpClient cliente;

            if (string.IsNullOrWhiteSpace(host))
            {
                cliente = new SmtpClient();
                var seccion = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                remitente = string.IsNullOrWhiteSpace(seccion?.From) ? "no-reply@logtech.local" : seccion.From;
            }
            else
            {
                var puerto = int.Parse(ConfigurationManager.AppSettings["CorreoSmtpPuerto"] ?? "587");
                var usuario = ConfigurationManager.AppSettings["CorreoSmtpUsuario"];
                cliente = new SmtpClient(host, puerto)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(usuario, ConfigurationManager.AppSettings["CorreoSmtpClave"])
                };
                remitente = ConfigurationManager.AppSettings["CorreoRemitente"] ?? usuario;
            }

            using (cliente)
            using (var mensaje = new MailMessage(new MailAddress(remitente, "LogTech"), new MailAddress(destino)))
            {
                mensaje.Subject = asunto;
                mensaje.Body = cuerpoHtml;
                mensaje.IsBodyHtml = true;
                if (adjunto != null) mensaje.Attachments.Add(new Attachment(new MemoryStream(adjunto), nombreAdjunto));

                if (cliente.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory
                    && string.IsNullOrEmpty(cliente.PickupDirectoryLocation))
                {
                    var carpeta = HostingEnvironment.MapPath("~/App_Data/Mails");
                    Directory.CreateDirectory(carpeta);
                    cliente.PickupDirectoryLocation = carpeta;
                }

                cliente.Send(mensaje);
            }
        }
    }
}
