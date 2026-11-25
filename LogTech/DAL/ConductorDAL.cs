using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;
using LogTech.Servicios;

namespace LogTech.DAL
{
    public sealed class ConductorDAL
    {
        private const string Consulta = @"SELECT c.IdConductor, c.IdFlota, c.Nombre, c.Apellido, c.Dni, c.NumeroLicencia,
                c.Telefono, c.Estado, c.FechaAlta,
                CASE WHEN EXISTS (SELECT 1 FROM Viajes v WHERE v.IdConductor = c.IdConductor AND v.Estado IN " + ViajeDAL.EstadosEnCurso + @")
                    THEN 1 ELSE 0 END
            FROM Conductores c ";

        public ConductorBE Obtener(int idConductor)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE c.IdConductor = @Id;", LeerConductor, EjecutorDAL.Parametro("@Id", idConductor));
            return lista.Count == 0 ? null : lista[0];
        }

        // Conductores de la flota. texto: el DNI completo (está cifrado) o parte del nombre o el apellido; estado vacío = todos.
        public List<ConductorBE> Listar(int idFlota, string texto, string estado)
        {
            return EjecutorDAL.Leer(Consulta + @"WHERE c.IdFlota = @Flota AND (@Estado = N'' OR c.Estado = @Estado)
                AND (@Texto = N'' OR c.Dni = @Dni OR c.Nombre LIKE @Like OR c.Apellido LIKE @Like)
                ORDER BY c.Estado, c.Apellido, c.Nombre;", LeerConductor,
                EjecutorDAL.Parametro("@Flota", idFlota), EjecutorDAL.Parametro("@Estado", estado ?? string.Empty),
                EjecutorDAL.Parametro("@Texto", texto ?? string.Empty), EjecutorDAL.Parametro("@Like", "%" + texto + "%"),
                EjecutorDAL.Parametro("@Dni", CifradoServicio.Cifrar((texto ?? string.Empty).Trim())));
        }

        // R7: el DNI no puede estar en otro conductor activo, de esta flota o de otra.
        public bool DniEnUso(string dni, int exceptoId)
        {
            return (int)EjecutorDAL.LeerValor("SELECT COUNT(*) FROM Conductores WHERE Dni = @Dni AND Estado = 'Activo' AND IdConductor <> @Id;",
                EjecutorDAL.Parametro("@Dni", CifradoServicio.Cifrar(dni)), EjecutorDAL.Parametro("@Id", exceptoId)) > 0;
        }

        // El conductor y sus documentos (quedan pendientes de revisión), juntos en una sola transacción.
        public void Alta(ConductorBE conductor, IList<DocumentoBE> documentos)
        {
            var parametros = new List<SqlParameter>(Parametros(conductor));
            var sql = @"INSERT INTO Conductores (IdFlota, Nombre, Apellido, Dni, NumeroLicencia, Telefono)
                    VALUES (@Flota, @Nombre, @Apellido, @Dni, @Licencia, @Telefono);
                DECLARE @IdConductor INT = SCOPE_IDENTITY();
                " + DocumentoDAL.Insertar(documentos, "IdConductor", "@IdConductor", parametros);
            EjecutorDAL.EjecutarCambio(sql, null, parametros.ToArray());
        }

        public void Modificar(ConductorBE conductor)
        {
            const string sql = @"UPDATE Conductores SET Nombre = @Nombre, Apellido = @Apellido, Dni = @Dni,
                    NumeroLicencia = @Licencia, Telefono = @Telefono
                WHERE IdConductor = @Id AND IdFlota = @Flota;";
            var parametros = new List<SqlParameter>(Parametros(conductor));
            parametros.Add(EjecutorDAL.Parametro("@Id", conductor.IdConductor));
            EjecutorDAL.EjecutarCambio(sql, null, parametros.ToArray());
        }

        // Baja lógica. Si era el conductor designado de un camión, el camión queda sin conductor fijo.
        public void Inactivar(int idConductor)
        {
            EjecutorDAL.EjecutarCambio(@"UPDATE Conductores SET Estado = 'Inactivo' WHERE IdConductor = @Id;
                UPDATE Vehiculos SET IdConductorDesignado = NULL WHERE IdConductorDesignado = @Id;", null,
                EjecutorDAL.Parametro("@Id", idConductor));
        }

        private static SqlParameter[] Parametros(ConductorBE conductor)
        {
            return new[]
            {
                EjecutorDAL.Parametro("@Flota", conductor.IdFlota),
                EjecutorDAL.Parametro("@Nombre", conductor.Nombre),
                EjecutorDAL.Parametro("@Apellido", conductor.Apellido),
                EjecutorDAL.Parametro("@Dni", CifradoServicio.Cifrar(conductor.Dni)),
                EjecutorDAL.Parametro("@Licencia", CifradoServicio.Cifrar(conductor.NumeroLicencia)),
                EjecutorDAL.Parametro("@Telefono", CifradoServicio.Cifrar(conductor.Telefono))
            };
        }

        private static ConductorBE LeerConductor(SqlDataReader lector)
        {
            return new ConductorBE
            {
                IdConductor = lector.GetInt32(0),
                IdFlota = lector.GetInt32(1),
                Nombre = lector.GetString(2),
                Apellido = lector.GetString(3),
                Dni = CifradoServicio.Descifrar(lector.GetString(4)),
                NumeroLicencia = CifradoServicio.Descifrar(lector.GetString(5)),
                Telefono = CifradoServicio.Descifrar(lector.GetString(6)),
                Estado = lector.GetString(7),
                FechaAlta = lector.GetDateTime(8),
                EnViaje = lector.GetInt32(9) == 1
            };
        }
    }
}
