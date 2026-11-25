using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class DocumentoDAL
    {
        private const string Consulta = @"SELECT d.IdDocumento, d.IdUsuario, ISNULL(u.RazonSocial, u.Nombre + ' ' + u.Apellido),
                d.IdVehiculo, x.Patente, d.Tipo, d.Archivo, d.FechaVencimiento, d.Estado, d.Motivo, d.FechaCarga,
                d.IdConductor, c.Apellido + N', ' + c.Nombre
            FROM Documentos d
            JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
            LEFT JOIN Vehiculos x ON x.IdVehiculo = d.IdVehiculo
            LEFT JOIN Conductores c ON c.IdConductor = d.IdConductor ";

        // Lo que queda «de la cuenta»: ni de un vehículo ni de un conductor (R3 mira solo estos).
        private const string DeLaCuenta = "IdVehiculo IS NULL AND IdConductor IS NULL";

        public DocumentoBE Obtener(int idDocumento)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE d.IdDocumento = @Id;", LeerDocumento, EjecutorDAL.Parametro("@Id", idDocumento));
            return lista.Count == 0 ? null : lista[0];
        }

        // Legajo de un usuario (sin los dados de baja): los suyos, los de sus vehículos y los de sus conductores.
        public List<DocumentoBE> ListarDelUsuario(int idUsuario)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE d.IdUsuario = @Id AND d.Estado <> 'Inactivo' ORDER BY d.FechaVencimiento;",
                LeerDocumento, EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Los documentos de todos los vehículos (para saber qué camiones están habilitados en el match del dador).
        public List<DocumentoBE> ListarDeVehiculos()
        {
            return EjecutorDAL.Leer(Consulta + "WHERE d.IdVehiculo IS NOT NULL AND d.Estado <> 'Inactivo';", LeerDocumento);
        }

        // Para el analista: documentos de todos los usuarios; estado vacío = todos menos los dados de baja.
        public List<DocumentoBE> ListarTodos(string estado)
        {
            return EjecutorDAL.Leer(Consulta + @"WHERE (@Estado = N'' AND d.Estado <> 'Inactivo' OR d.Estado = @Estado)
                ORDER BY CASE WHEN d.Estado = 'Pendiente' THEN 0 ELSE 1 END, d.FechaCarga;", LeerDocumento,
                EjecutorDAL.Parametro("@Estado", estado ?? string.Empty));
        }

        public void Alta(DocumentoBE documento)
        {
            const string sql = @"INSERT INTO Documentos (IdUsuario, IdVehiculo, IdConductor, Tipo, Archivo, FechaVencimiento)
                VALUES (@Usuario, @Vehiculo, @Conductor, @Tipo, @Archivo, @Vencimiento);";
            EjecutorDAL.EjecutarCambio(sql, null, EjecutorDAL.Parametro("@Usuario", documento.IdUsuario),
                EjecutorDAL.Parametro("@Vehiculo", documento.IdVehiculo), EjecutorDAL.Parametro("@Conductor", documento.IdConductor),
                EjecutorDAL.Parametro("@Tipo", documento.Tipo), EjecutorDAL.Parametro("@Archivo", documento.Archivo),
                EjecutorDAL.Parametro("@Vencimiento", documento.FechaVencimiento.Date));
        }

        // Los documentos que se cargan junto con un vehículo o un conductor nuevo, en el mismo SQL (una sola transacción).
        // columna: IdVehiculo o IdConductor; variable: la variable del SQL con el id recién creado. Son textos fijos del
        // código, nunca lo que escribe el usuario.
        internal static string Insertar(IList<DocumentoBE> documentos, string columna, string variable, List<SqlParameter> parametros)
        {
            var sql = new StringBuilder();
            for (var i = 0; i < documentos.Count; i++)
            {
                sql.Append("INSERT INTO Documentos (IdUsuario, " + columna + ", Tipo, Archivo, FechaVencimiento) VALUES (@DUsuario" + i + ", "
                    + variable + ", @DTipo" + i + ", @DArchivo" + i + ", @DVence" + i + ");\n");
                parametros.Add(EjecutorDAL.Parametro("@DUsuario" + i, documentos[i].IdUsuario));
                parametros.Add(EjecutorDAL.Parametro("@DTipo" + i, documentos[i].Tipo));
                parametros.Add(EjecutorDAL.Parametro("@DArchivo" + i, documentos[i].Archivo));
                parametros.Add(EjecutorDAL.Parametro("@DVence" + i, documentos[i].FechaVencimiento.Date));
            }
            return sql.ToString();
        }

        // Al modificarlo vuelve a revisión: pendiente, sin motivo de rechazo y con el aviso de vencimiento sin mandar.
        public void Modificar(DocumentoBE documento)
        {
            const string sql = @"UPDATE Documentos SET IdVehiculo = @Vehiculo, IdConductor = @Conductor, Archivo = @Archivo,
                    FechaVencimiento = @Vencimiento, Estado = 'Pendiente', Motivo = NULL, FechaCarga = SYSDATETIME(), AvisoEnviado = 0
                WHERE IdDocumento = @Id;";
            EjecutorDAL.EjecutarCambio(sql, null, EjecutorDAL.Parametro("@Id", documento.IdDocumento),
                EjecutorDAL.Parametro("@Vehiculo", documento.IdVehiculo), EjecutorDAL.Parametro("@Conductor", documento.IdConductor),
                EjecutorDAL.Parametro("@Archivo", documento.Archivo), EjecutorDAL.Parametro("@Vencimiento", documento.FechaVencimiento.Date));
        }

        // Inactivo (baja lógica), Aprobado o Rechazado (con motivo). Solo si sigue en el estado esperado.
        public bool CambiarEstado(int idDocumento, string estadoActual, string estadoNuevo, string motivo)
        {
            return EjecutorDAL.EjecutarCambio("UPDATE Documentos SET Estado = @Nuevo, Motivo = @Motivo WHERE IdDocumento = @Id AND Estado = @Actual;",
                null, EjecutorDAL.Parametro("@Id", idDocumento), EjecutorDAL.Parametro("@Actual", estadoActual),
                EjecutorDAL.Parametro("@Nuevo", estadoNuevo), EjecutorDAL.Parametro("@Motivo", motivo)) == 1;
        }

        // R3: documentos de la cuenta vencidos (por estado o por fecha) que todavía están en el legajo. Los de los
        // vehículos y los conductores no cuentan acá: tienen su propio semáforo.
        public bool TieneVencidos(int idUsuario)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT COUNT(*) FROM Documentos WHERE IdUsuario = @Id AND " + DeLaCuenta + @"
                AND (Estado = 'Vencido' OR (Estado IN ('Aprobado', 'Pendiente') AND FechaVencimiento < CAST(SYSDATETIME() AS DATE)));",
                EjecutorDAL.Parametro("@Id", idUsuario)) > 0;
        }

        // R3: tiene el documento obligatorio aprobado y vigente.
        public bool TieneAprobado(int idUsuario, string tipo)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT COUNT(*) FROM Documentos WHERE IdUsuario = @Id AND Tipo = @Tipo
                AND Estado = 'Aprobado' AND FechaVencimiento >= CAST(SYSDATETIME() AS DATE);",
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Tipo", tipo)) > 0;
        }

        // Los avisos dicen de qué vehículo o conductor es cada documento (la flota tiene varios).
        private const string SalidaAviso = @"OUTPUT inserted.IdDocumento, inserted.IdUsuario, inserted.Tipo, inserted.FechaVencimiento,
                    x.Patente, c.Apellido + N', ' + c.Nombre
                FROM Documentos d
                LEFT JOIN Vehiculos x ON x.IdVehiculo = d.IdVehiculo
                LEFT JOIN Conductores c ON c.IdConductor = d.IdConductor ";

        // CU-003-021 (R37): pasa a "Vencido" lo que ya expiró y devuelve esos documentos.
        public List<DocumentoBE> MarcarVencidos(DateTime hoy)
        {
            return EjecutorDAL.CambiarYLeerFilas(@"UPDATE d SET Estado = 'Vencido' " + SalidaAviso
                + "WHERE d.Estado IN ('Aprobado', 'Pendiente') AND d.FechaVencimiento < @Hoy;", LeerAviso,
                EjecutorDAL.Parametro("@Hoy", hoy.Date));
        }

        // CU-003-021 (R37): aprobados que vencen en los próximos 30 días y todavía no se avisaron.
        public List<DocumentoBE> MarcarAvisosPorVencer(DateTime hoy)
        {
            return EjecutorDAL.CambiarYLeerFilas(@"UPDATE d SET AvisoEnviado = 1 " + SalidaAviso
                + "WHERE d.Estado = 'Aprobado' AND d.AvisoEnviado = 0 AND d.FechaVencimiento BETWEEN @Hoy AND DATEADD(DAY, 30, @Hoy);",
                LeerAviso, EjecutorDAL.Parametro("@Hoy", hoy.Date));
        }

        private static DocumentoBE LeerAviso(SqlDataReader lector)
        {
            return new DocumentoBE
            {
                IdDocumento = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Tipo = lector.GetString(2),
                FechaVencimiento = lector.GetDateTime(3),
                Patente = lector.IsDBNull(4) ? null : lector.GetString(4),
                Conductor = lector.IsDBNull(5) ? null : lector.GetString(5)
            };
        }

        private static DocumentoBE LeerDocumento(SqlDataReader lector)
        {
            return new DocumentoBE
            {
                IdDocumento = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Usuario = lector.GetString(2),
                IdVehiculo = EjecutorDAL.Entero(lector, 3),
                Patente = EjecutorDAL.Texto(lector, 4),
                Tipo = lector.GetString(5),
                Archivo = lector.GetString(6),
                FechaVencimiento = lector.GetDateTime(7),
                Estado = lector.GetString(8),
                Motivo = EjecutorDAL.Texto(lector, 9),
                FechaCarga = lector.GetDateTime(10),
                IdConductor = EjecutorDAL.Entero(lector, 11),
                Conductor = EjecutorDAL.Texto(lector, 12)
            };
        }
    }
}
