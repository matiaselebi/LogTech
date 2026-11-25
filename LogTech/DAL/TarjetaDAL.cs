using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class TarjetaDAL
    {
        private const string Consulta = @"SELECT IdTarjeta, IdUsuario, Titular, Marca, Ultimos4, Vencimiento, Token, FechaAlta
            FROM Tarjetas ";

        // Las tarjetas activas del usuario.
        public List<TarjetaBE> Listar(int idUsuario)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE IdUsuario = @Id AND Activa = 1 ORDER BY FechaAlta;", LeerTarjeta,
                EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Una tarjeta activa del usuario (null si no es suya o la quitó).
        public TarjetaBE Obtener(int idTarjeta, int idUsuario)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE IdTarjeta = @Tarjeta AND IdUsuario = @Id AND Activa = 1;", LeerTarjeta,
                EjecutorDAL.Parametro("@Tarjeta", idTarjeta), EjecutorDAL.Parametro("@Id", idUsuario));
            return lista.Count == 0 ? null : lista[0];
        }

        // Cualquier tarjeta, aunque ya la hayan quitado (para cobrar o devolver un pago hecho con ella).
        public TarjetaBE ObtenerPorId(int idTarjeta)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE IdTarjeta = @Tarjeta;", LeerTarjeta, EjecutorDAL.Parametro("@Tarjeta", idTarjeta));
            return lista.Count == 0 ? null : lista[0];
        }

        public void Alta(TarjetaBE tarjeta)
        {
            EjecutorDAL.EjecutarCambio(@"INSERT INTO Tarjetas (IdUsuario, Titular, Marca, Ultimos4, Vencimiento, Token)
                VALUES (@Id, @Titular, @Marca, @Ultimos4, @Vencimiento, @Token);", null,
                EjecutorDAL.Parametro("@Id", tarjeta.IdUsuario), EjecutorDAL.Parametro("@Titular", tarjeta.Titular),
                EjecutorDAL.Parametro("@Marca", tarjeta.Marca), EjecutorDAL.Parametro("@Ultimos4", tarjeta.Ultimos4),
                EjecutorDAL.Parametro("@Vencimiento", tarjeta.Vencimiento), EjecutorDAL.Parametro("@Token", tarjeta.Token));
        }

        // Baja lógica: los pagos hechos con ella la siguen nombrando.
        public bool Quitar(int idTarjeta, int idUsuario)
        {
            return EjecutorDAL.EjecutarCambio("UPDATE Tarjetas SET Activa = 0 WHERE IdTarjeta = @Tarjeta AND IdUsuario = @Id AND Activa = 1;",
                null, EjecutorDAL.Parametro("@Tarjeta", idTarjeta), EjecutorDAL.Parametro("@Id", idUsuario)) == 1;
        }

        // La tarjeta está elegida para pagar una oferta que todavía no se aceptó, o un seguro que se va a renovar.
        public bool EnUso(int idTarjeta)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT (SELECT COUNT(*) FROM Viajes WHERE IdTarjeta = @Tarjeta AND Estado = N'Pendiente')
                + (SELECT COUNT(*) FROM Suscripciones WHERE IdTarjeta = @Tarjeta AND FechaBaja IS NULL AND CancelaAlVencer = 0);",
                EjecutorDAL.Parametro("@Tarjeta", idTarjeta)) > 0;
        }

        private static TarjetaBE LeerTarjeta(SqlDataReader lector)
        {
            return new TarjetaBE
            {
                IdTarjeta = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Titular = lector.GetString(2),
                Marca = lector.GetString(3),
                Ultimos4 = lector.GetString(4),
                Vencimiento = lector.GetString(5),
                Token = lector.GetString(6),
                FechaAlta = lector.GetDateTime(7)
            };
        }
    }
}
