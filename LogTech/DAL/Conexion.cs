using System.Configuration;
using System.Data.SqlClient;

namespace LogTech.DAL
{
    internal static class Conexion
    {
        // La cadena está en Web.config (connectionStrings, «LogTechDB»): quien instala el sistema la cambia sin recompilar
        // (por ejemplo, Data Source=.\SQLEXPRESS si usa SQL Server Express).
        internal static readonly string cadena = ConfigurationManager.ConnectionStrings["LogTechDB"].ConnectionString;

        // Apunta a master. Se usa para BACKUP y RESTORE, que no pueden correr
        // conectados a la propia base.
        internal static readonly string maestra =
            new SqlConnectionStringBuilder(cadena) { InitialCatalog = "master" }.ConnectionString;
    }
}
