using System;
using System.Data;
using System.Data.SqlClient;

namespace LogTech.DAL
{
    public sealed class MantenimientoDAL
    {
        public string CuentaServicio()
        {
            using (var conexion = new SqlConnection(Conexion.maestra))
            using (var comando = new SqlCommand(
                "SELECT service_account FROM sys.dm_server_services WHERE servicename LIKE 'SQL Server (%';", conexion))
            {
                conexion.Open();
                return Convert.ToString(comando.ExecuteScalar());
            }
        }

        public void Backup(string ruta)
        {
            using (var conexion = new SqlConnection(Conexion.maestra))
            using (var comando = new SqlCommand(
                "BACKUP DATABASE LogTechDB TO DISK = @Ruta WITH FORMAT, INIT;", conexion))
            {
                comando.Parameters.Add("@Ruta", SqlDbType.NVarChar, 260).Value = ruta;
                comando.CommandTimeout = 0;
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public bool ArchivoValido(string ruta)
        {
            using (var conexion = new SqlConnection(Conexion.maestra))
            using (var comando = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @Ruta;", conexion))
            {
                comando.Parameters.Add("@Ruta", SqlDbType.NVarChar, 260).Value = ruta;
                conexion.Open();
                try { comando.ExecuteNonQuery(); return true; }
                catch (SqlException) { return false; }
            }
        }

        // CU-010-066 Recuperación de DV: el administrador vuelve a firmar toda la base (acepta los datos actuales).
        public void RecalcularDigitos()
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("EXEC dbo.RecalcularDigitos;", conexion))
            {
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public void Restore(string ruta)
        {
            // El escenario típico de este botón es recuperarse de una base borrada por
            // completo (usuario de contingencia): ahí LogTechDB no existe todavía, y
            // ALTER DATABASE sobre una base inexistente tira error 5011. El IF evita ese
            // error - sin él, el RESTORE terminaba andando igual (SQL Server sigue el
            // batch después del error de ALTER) pero el cliente veía una excepción y la
            // pantalla mostraba "no se pudo restaurar" con la base ya restaurada bien.
            const string sql = @"IF DB_ID('LogTechDB') IS NOT NULL
                    ALTER DATABASE LogTechDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE LogTechDB FROM DISK = @Ruta WITH REPLACE;
                ALTER DATABASE LogTechDB SET MULTI_USER;";
            using (var conexion = new SqlConnection(Conexion.maestra))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Ruta", SqlDbType.NVarChar, 260).Value = ruta;
                comando.CommandTimeout = 0;
                conexion.Open();
                comando.ExecuteNonQuery();
            }

            // RESTORE WITH REPLACE reemplaza la base "por debajo" de cualquier conexión que
            // el pool tenga abierta contra ella. Sin limpiar el pool, la primera conexión
            // nueva puede reusar una conexión vieja e invalida y tirar SqlException, aunque
            // el restore ya haya terminado bien (por eso a veces avisa error pero la base
            // queda restaurada igual).
            SqlConnection.ClearAllPools();

            // Deja los dígitos verificadores sincronizados con la base recién restaurada.
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("EXEC dbo.RecalcularDigitos;", conexion))
            {
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
