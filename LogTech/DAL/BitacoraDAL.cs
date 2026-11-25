using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class BitacoraDAL
    {
        // La bitácora se escribe aunque la base esté dañada (ahí se anota el error de integridad): firma solo su fila
        // nueva y actualiza el DVV únicamente si la tabla estaba sana, así nunca tapa una alteración hecha por afuera.
        public void Registrar(int? idUsuario, string usuario, string descripcion, string criticidad)
        {
            EjecutorDAL.EjecutarSinRecalcular("usp_Bitacora_Registrar",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = (object)idUsuario ?? DBNull.Value },
                new SqlParameter("@Usuario", SqlDbType.NVarChar, 50) { Value = usuario.Length > 50 ? usuario.Substring(0, 50) : usuario },
                new SqlParameter("@Descripcion", SqlDbType.NVarChar, 250) { Value = descripcion },
                new SqlParameter("@Criticidad", SqlDbType.NVarChar, 20) { Value = criticidad });
        }

        public IList<EventoBitacoraBE> ObtenerPagina(int pagina, int cantidad, FiltroBitacora filtro, out int total)
        {
            var condiciones = new StringBuilder(" WHERE 1 = 1");
            if (filtro.Desde.HasValue) condiciones.Append(" AND Fecha >= @Desde");
            if (filtro.Hasta.HasValue) condiciones.Append(" AND Fecha < @Hasta");
            // El filtro de usuario acepta el nombre (o parte) o el número de ID.
            if (PorId(filtro)) condiciones.Append(" AND IdUsuario = @IdUsuario");
            else if (!string.IsNullOrWhiteSpace(filtro.Usuario)) condiciones.Append(" AND Usuario LIKE @Usuario");
            if (!string.IsNullOrWhiteSpace(filtro.Criticidad)) condiciones.Append(" AND Criticidad = @Criticidad");
            var where = condiciones.ToString();

            var eventos = new List<EventoBitacoraBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            {
                conexion.Open();
                using (var contar = new SqlCommand("SELECT COUNT(*) FROM BitacoraEventos" + where, conexion))
                {
                    AgregarFiltros(contar, filtro);
                    total = Convert.ToInt32(contar.ExecuteScalar());
                }

                const string plantilla = @"SELECT IdEvento, Fecha, Usuario, Descripcion, Criticidad, IdUsuario FROM BitacoraEventos{0}
                    ORDER BY IdEvento DESC OFFSET @Desde2 ROWS FETCH NEXT @Cantidad ROWS ONLY;";
                using (var comando = new SqlCommand(string.Format(plantilla, where), conexion))
                {
                    AgregarFiltros(comando, filtro);
                    comando.Parameters.Add("@Desde2", SqlDbType.Int).Value = pagina * cantidad;
                    comando.Parameters.Add("@Cantidad", SqlDbType.Int).Value = cantidad;
                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read()) eventos.Add(LeerEvento(lector));
                    }
                }
            }
            return eventos;
        }

        // Eventos anteriores a la fecha límite, del más nuevo al más viejo. Si la tabla fue alterada por afuera no
        // devuelve nada: pasarlos al historial y borrarlos taparía el daño.
        public IList<EventoBitacoraBE> ListarAnteriores(DateTime limite)
        {
            const string sql = @"IF dbo.TablaIntegra(N'BitacoraEventos') = 1
                SELECT IdEvento, Fecha, Usuario, Descripcion, Criticidad, IdUsuario FROM BitacoraEventos
                WHERE Fecha < @Limite ORDER BY IdEvento DESC;";
            return EjecutorDAL.Leer(sql, LeerEvento, new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = limite });
        }

        // Borra los eventos que ya se pasaron al historial (hasta el último que se leyó) y actualiza el DVV, solo si
        // la tabla sigue sana.
        public void BorrarAnteriores(DateTime limite, long hastaIdEvento)
        {
            const string sql = @"IF dbo.TablaIntegra(N'BitacoraEventos') = 1
                BEGIN
                    DELETE FROM BitacoraEventos WHERE Fecha < @Limite AND IdEvento <= @HastaId;
                    DELETE FROM DVV WHERE Tabla = N'BitacoraEventos';
                    INSERT INTO DVV (Tabla, Digito) SELECT Tabla, Digito FROM dbo.DVV_Calculado WHERE Tabla = N'BitacoraEventos';
                END";
            EjecutorDAL.EjecutarSinRecalcular(sql,
                new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = limite },
                new SqlParameter("@HastaId", SqlDbType.BigInt) { Value = hastaIdEvento });
        }

        private static EventoBitacoraBE LeerEvento(SqlDataReader lector)
        {
            return new EventoBitacoraBE
            {
                IdEvento = lector.GetInt64(0),
                Fecha = lector.GetDateTime(1),
                Usuario = lector.GetString(2),
                Descripcion = lector.GetString(3),
                Criticidad = lector.GetString(4),
                IdUsuario = lector.IsDBNull(5) ? (int?)null : lector.GetInt32(5)
            };
        }

        // Si en «Usuario» se escribió solo un número, se busca por el ID del usuario.
        internal static bool PorId(FiltroBitacora filtro)
        {
            int id;
            return filtro.Usuario != null && int.TryParse(filtro.Usuario.Trim(), out id) && id > 0;
        }

        private static void AgregarFiltros(SqlCommand comando, FiltroBitacora filtro)
        {
            if (filtro.Desde.HasValue)
                comando.Parameters.Add("@Desde", SqlDbType.DateTime2).Value = filtro.Desde.Value;
            if (filtro.Hasta.HasValue)
                comando.Parameters.Add("@Hasta", SqlDbType.DateTime2).Value = filtro.Hasta.Value.Date.AddDays(1);
            if (PorId(filtro))
                comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = int.Parse(filtro.Usuario.Trim());
            else if (!string.IsNullOrWhiteSpace(filtro.Usuario))
                comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = "%" + filtro.Usuario.Trim() + "%";
            if (!string.IsNullOrWhiteSpace(filtro.Criticidad))
                comando.Parameters.Add("@Criticidad", SqlDbType.NVarChar, 20).Value = filtro.Criticidad;
        }
    }
}
