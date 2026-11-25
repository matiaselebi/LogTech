using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Web;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Avisos para los usuarios: quedan en su panel (Inicio) y, si en Web.config está
    // AvisosPorCorreo = true, también se mandan por correo.
    public sealed class NotificacionBLL
    {
        private const int CantidadEnPanel = 10;
        private readonly NotificacionDAL notificaciones = new NotificacionDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();

        public void Avisar(int idUsuario, string texto)
        {
            if (texto.Length > 250) texto = texto.Substring(0, 247) + "...";
            notificaciones.Agregar(idUsuario, texto);
            if (ConfigurationManager.AppSettings["AvisosPorCorreo"] != "true") return;

            // El correo es un extra: si falla, el aviso igual quedó en el panel.
            try
            {
                var usuario = usuarios.ObtenerPorId(idUsuario);
                if (usuario != null)
                    CorreoServicio.Enviar(usuario.Correo, "LogTech: novedades", "<p>" + HttpUtility.HtmlEncode(texto) + "</p>");
            }
            catch (SmtpException) { }
            catch (InvalidOperationException) { }
        }

        public List<NotificacionBE> ListarUltimas(int idUsuario)
        {
            return notificaciones.ListarUltimas(idUsuario, CantidadEnPanel);
        }

        public void MarcarLeidas(int idUsuario)
        {
            notificaciones.MarcarLeidas(idUsuario);
        }
    }
}
