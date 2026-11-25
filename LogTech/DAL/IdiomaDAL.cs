using System.Data;
using System.Data.SqlClient;

namespace LogTech.DAL
{
    public sealed class IdiomaDAL
    {
        public void Actualizar(int idUsuario, string idioma)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Usuarios SET Idioma = @Idioma WHERE IdUsuario = @IdUsuario;", "Usuarios",
                new SqlParameter("@Idioma", SqlDbType.VarChar, 10) { Value = idioma },
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }
    }
}
