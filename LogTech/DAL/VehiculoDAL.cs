using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class VehiculoDAL
    {
        private const string Consulta = @"SELECT x.IdVehiculo, x.IdPropietario, ISNULL(u.RazonSocial, u.Nombre + ' ' + u.Apellido),
                x.Patente, x.Marca, x.Modelo, x.CapacidadToneladas, x.TipoAcoplado, x.Estado, x.FechaAlta,
                CASE WHEN EXISTS (SELECT 1 FROM Viajes v WHERE v.IdVehiculo = x.IdVehiculo AND v.Estado IN " + ViajeDAL.EstadosEnCurso + @")
                    THEN 1 ELSE 0 END,
                x.IdConductorDesignado, c.Apellido + N', ' + c.Nombre
            FROM Vehiculos x JOIN Usuarios u ON u.IdUsuario = x.IdPropietario
            LEFT JOIN Conductores c ON c.IdConductor = x.IdConductorDesignado ";

        public VehiculoBE Obtener(int idVehiculo)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE x.IdVehiculo = @Id;", LeerVehiculo, EjecutorDAL.Parametro("@Id", idVehiculo));
            return lista.Count == 0 ? null : lista[0];
        }

        // Vehículos del usuario. texto: parte de la patente, marca o modelo; estado vacío = todos.
        public List<VehiculoBE> Listar(int idPropietario, string texto, string estado)
        {
            return EjecutorDAL.Leer(Consulta + @"WHERE x.IdPropietario = @Id AND (@Estado = N'' OR x.Estado = @Estado)
                AND (@Texto = N'' OR x.Patente LIKE @Like OR x.Marca LIKE @Like OR x.Modelo LIKE @Like)
                ORDER BY x.Estado, x.Patente;", LeerVehiculo,
                EjecutorDAL.Parametro("@Id", idPropietario), EjecutorDAL.Parametro("@Estado", estado ?? string.Empty),
                EjecutorDAL.Parametro("@Texto", texto ?? string.Empty), EjecutorDAL.Parametro("@Like", "%" + texto + "%"));
        }

        // R5: la patente no puede estar en otro vehículo activo.
        public bool PatenteEnUso(string patente, int exceptoId)
        {
            return (int)EjecutorDAL.LeerValor("SELECT COUNT(*) FROM Vehiculos WHERE Patente = @Patente AND Estado = 'Activo' AND IdVehiculo <> @Id;",
                EjecutorDAL.Parametro("@Patente", patente), EjecutorDAL.Parametro("@Id", exceptoId)) > 0;
        }

        // El vehículo, sus documentos (quedan pendientes de revisión) y, si la flota paga un vehículo más, la suscripción
        // con el vehículo sumado y su pago: todo junto, en una sola transacción.
        public void Alta(VehiculoBE vehiculo, IList<DocumentoBE> documentos, SuscripcionBE cobro, TransaccionBE pago)
        {
            var parametros = new List<SqlParameter>(Parametros(vehiculo));
            var sql = @"INSERT INTO Vehiculos (IdPropietario, Patente, Marca, Modelo, CapacidadToneladas, TipoAcoplado)
                    VALUES (@Propietario, @Patente, @Marca, @Modelo, @Capacidad, @Acoplado);
                DECLARE @IdVehiculo INT = SCOPE_IDENTITY();
                " + DocumentoDAL.Insertar(documentos, "IdVehiculo", "@IdVehiculo", parametros);
            if (cobro != null)
            {
                // ponytail: si la suscripción terminó en ese instante, el vehículo igual se guarda; la renovación lo ajusta.
                sql += @"UPDATE Suscripciones SET CantidadPatentes = @Patentes, Monto = @Monto
                    WHERE IdSuscripcion = @IdSuscripcion AND FechaBaja IS NULL;
                " + TransaccionDAL.Insertar;
                parametros.AddRange(TransaccionDAL.Parametros(pago));
                parametros.Add(EjecutorDAL.Parametro("@Patentes", cobro.CantidadPatentes));
                parametros.Add(EjecutorDAL.Parametro("@Monto", cobro.Monto));
                parametros.Add(EjecutorDAL.Parametro("@IdSuscripcion", cobro.IdSuscripcion));
            }
            EjecutorDAL.EjecutarCambio(sql, null, parametros.ToArray());
        }

        public void Modificar(VehiculoBE vehiculo)
        {
            const string sql = @"UPDATE Vehiculos SET Patente = @Patente, Marca = @Marca, Modelo = @Modelo,
                    CapacidadToneladas = @Capacidad, TipoAcoplado = @Acoplado
                WHERE IdVehiculo = @Id AND IdPropietario = @Propietario;";
            var parametros = new List<SqlParameter>(Parametros(vehiculo));
            parametros.Add(EjecutorDAL.Parametro("@Id", vehiculo.IdVehiculo));
            EjecutorDAL.EjecutarCambio(sql, null, parametros.ToArray());
        }

        public void Inactivar(int idVehiculo)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Vehiculos SET Estado = 'Inactivo' WHERE IdVehiculo = @Id;", null,
                EjecutorDAL.Parametro("@Id", idVehiculo));
        }

        // Flota: el conductor fijo del camión (null = ninguno).
        public void DesignarConductor(int idVehiculo, int? idConductor)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Vehiculos SET IdConductorDesignado = @Conductor WHERE IdVehiculo = @Id;", null,
                EjecutorDAL.Parametro("@Id", idVehiculo), EjecutorDAL.Parametro("@Conductor", idConductor));
        }

        // Motor de asignación del dador (CU-001-006): camiones activos y libres que soportan
        // el peso, con la reputación de su dueño y la última ubicación que reportaron.
        public List<VehiculoBE> ListarCompatibles(decimal peso)
        {
            const string sql = @"SELECT x.IdVehiculo, x.IdPropietario, ISNULL(u.RazonSocial, u.Nombre + ' ' + u.Apellido),
                    x.Patente, x.Marca, x.Modelo, x.CapacidadToneladas, x.TipoAcoplado,
                    k.Promedio, k.Cantidad, p.Latitud, p.Longitud
                FROM Vehiculos x
                JOIN Usuarios u ON u.IdUsuario = x.IdPropietario AND u.Estado = 'Activo'
                CROSS APPLY (SELECT AVG(CAST(c.Puntaje AS DECIMAL(4,2))) AS Promedio, COUNT(*) AS Cantidad
                             FROM Calificaciones c WHERE c.IdTransportista = x.IdPropietario) k
                OUTER APPLY (SELECT TOP 1 h.Latitud, h.Longitud FROM HistorialViaje h JOIN Viajes v ON v.IdViaje = h.IdViaje
                             WHERE v.IdVehiculo = x.IdVehiculo AND h.Latitud IS NOT NULL ORDER BY h.Fecha DESC) p
                WHERE x.Estado = 'Activo' AND x.CapacidadToneladas >= @Peso
                  AND NOT EXISTS (SELECT 1 FROM Viajes v WHERE v.IdVehiculo = x.IdVehiculo AND v.Estado IN " + ViajeDAL.EstadosEnCurso + ");";
            return EjecutorDAL.Leer(sql, lector =>
            {
                return new VehiculoBE
                {
                    IdVehiculo = lector.GetInt32(0),
                    IdPropietario = lector.GetInt32(1),
                    Propietario = lector.GetString(2),
                    Patente = lector.GetString(3),
                    Marca = lector.GetString(4),
                    Modelo = lector.GetString(5),
                    CapacidadToneladas = lector.GetDecimal(6),
                    TipoAcoplado = lector.GetString(7),
                    Reputacion = EjecutorDAL.Decimal(lector, 8),
                    CantidadCalificaciones = lector.GetInt32(9),
                    Latitud = EjecutorDAL.Decimal(lector, 10),
                    Longitud = EjecutorDAL.Decimal(lector, 11),
                    Estado = "Activo"
                };
            }, EjecutorDAL.Parametro("@Peso", peso));
        }

        private static SqlParameter[] Parametros(VehiculoBE vehiculo)
        {
            return new[]
            {
                EjecutorDAL.Parametro("@Propietario", vehiculo.IdPropietario),
                EjecutorDAL.Parametro("@Patente", vehiculo.Patente),
                EjecutorDAL.Parametro("@Marca", vehiculo.Marca),
                EjecutorDAL.Parametro("@Modelo", vehiculo.Modelo),
                EjecutorDAL.Parametro("@Capacidad", vehiculo.CapacidadToneladas),
                EjecutorDAL.Parametro("@Acoplado", vehiculo.TipoAcoplado)
            };
        }

        private static VehiculoBE LeerVehiculo(SqlDataReader lector)
        {
            return new VehiculoBE
            {
                IdVehiculo = lector.GetInt32(0),
                IdPropietario = lector.GetInt32(1),
                Propietario = lector.GetString(2),
                Patente = lector.GetString(3),
                Marca = lector.GetString(4),
                Modelo = lector.GetString(5),
                CapacidadToneladas = lector.GetDecimal(6),
                TipoAcoplado = lector.GetString(7),
                Estado = lector.GetString(8),
                FechaAlta = lector.GetDateTime(9),
                EnViaje = lector.GetInt32(10) == 1,
                IdConductorDesignado = EjecutorDAL.Entero(lector, 11),
                ConductorDesignado = EjecutorDAL.Texto(lector, 12)
            };
        }
    }
}
