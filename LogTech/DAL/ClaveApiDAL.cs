using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class ClaveApiDAL
    {
        public ClaveApiBE ObtenerActiva(int idUsuario)
        {
            var lista = EjecutorDAL.Leer(@"SELECT IdClave, IdUsuario, Ultimos4, FechaAlta, FechaUltimoUso FROM ClavesApi
                WHERE IdUsuario = @Id AND Activa = 1;", LeerClave, EjecutorDAL.Parametro("@Id", idUsuario));
            return lista.Count == 0 ? null : lista[0];
        }

        // Deja inactiva la clave anterior y guarda la nueva, en la misma transacción.
        public void Guardar(int idUsuario, string hash, string ultimos4)
        {
            EjecutorDAL.EjecutarCambio(@"UPDATE ClavesApi SET Activa = 0 WHERE IdUsuario = @Id AND Activa = 1;
                INSERT INTO ClavesApi (IdUsuario, Hash, Ultimos4) VALUES (@Id, @Hash, @Ultimos4);", null,
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Hash", hash),
                EjecutorDAL.Parametro("@Ultimos4", ultimos4));
        }

        public bool Revocar(int idUsuario)
        {
            return EjecutorDAL.EjecutarCambio("UPDATE ClavesApi SET Activa = 0 WHERE IdUsuario = @Id AND Activa = 1;", null,
                EjecutorDAL.Parametro("@Id", idUsuario)) > 0;
        }

        // IdUsuario dueño de la clave si está activa y la cuenta también; si no, null. Anota el último uso.
        public int? UsarClave(string hash)
        {
            var valor = EjecutorDAL.CambiarYLeer(@"UPDATE c SET FechaUltimoUso = SYSDATETIME()
                OUTPUT inserted.IdUsuario
                FROM ClavesApi c JOIN Usuarios u ON u.IdUsuario = c.IdUsuario
                WHERE c.Hash = @Hash AND c.Activa = 1 AND u.Estado = 'Activo';",
                EjecutorDAL.Parametro("@Hash", hash));
            return valor == null ? (int?)null : (int)valor;
        }

        private static ClaveApiBE LeerClave(SqlDataReader lector)
        {
            return new ClaveApiBE
            {
                IdClave = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Ultimos4 = lector.GetString(2),
                FechaAlta = lector.GetDateTime(3),
                FechaUltimoUso = lector.IsDBNull(4) ? (System.DateTime?)null : lector.GetDateTime(4)
            };
        }
    }
}
