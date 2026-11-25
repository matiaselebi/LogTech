using System;
using System.Data;
using System.Data.SqlClient;

namespace LogTech.DAL
{
    public sealed class RecuperacionClaveDAL
    {
        public void Crear(int idUsuario, string token, DateTime expira)
        {
            const string sql = @"INSERT INTO RecuperacionesClave (IdUsuario, Token, FechaExpira)
                VALUES (@IdUsuario, @Token, @Expira);";
            EjecutorDAL.EjecutarCambio(sql, null,
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Token", SqlDbType.Char, 64) { Value = token },
                new SqlParameter("@Expira", SqlDbType.DateTime2) { Value = expira });
        }

        // Devuelve el IdUsuario si el token existe, no fue usado y no venció. Si no, 0.
        public int ObtenerUsuarioVigente(string token)
        {
            const string sql = @"SELECT IdUsuario FROM RecuperacionesClave
                WHERE Token = @Token AND Usado = 0 AND FechaExpira > SYSDATETIME();";
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Token", SqlDbType.Char, 64).Value = token ?? string.Empty;
                conexion.Open();
                var valor = comando.ExecuteScalar();
                return valor == null ? 0 : Convert.ToInt32(valor);
            }
        }

        public void MarcarUsado(string token)
        {
            EjecutorDAL.EjecutarCambio("UPDATE RecuperacionesClave SET Usado = 1 WHERE Token = @Token;", null,
                new SqlParameter("@Token", SqlDbType.Char, 64) { Value = token });
        }
    }
}
