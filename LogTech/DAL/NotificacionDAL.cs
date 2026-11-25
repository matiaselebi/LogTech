using System.Collections.Generic;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class NotificacionDAL
    {
        public void Agregar(int idUsuario, string texto)
        {
            EjecutorDAL.EjecutarCambio("INSERT INTO Notificaciones (IdUsuario, Texto) VALUES (@Id, @Texto);", null,
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Texto", texto));
        }

        // Las últimas notificaciones del usuario, primero las no leídas.
        public List<NotificacionBE> ListarUltimas(int idUsuario, int cantidad)
        {
            return EjecutorDAL.Leer(@"SELECT TOP (@Cantidad) Fecha, Texto, Leida FROM Notificaciones
                WHERE IdUsuario = @Id ORDER BY Leida, Fecha DESC, IdNotificacion DESC;", lector => new NotificacionBE
            {
                Fecha = lector.GetDateTime(0),
                Texto = lector.GetString(1),
                Leida = lector.GetBoolean(2)
            }, EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Cantidad", cantidad));
        }

        public void MarcarLeidas(int idUsuario)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Notificaciones SET Leida = 1 WHERE IdUsuario = @Id AND Leida = 0;", null,
                EjecutorDAL.Parametro("@Id", idUsuario));
        }
    }
}
