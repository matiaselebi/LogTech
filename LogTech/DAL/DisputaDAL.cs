using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Disputas comerciales (CU-005-027). Abrir y resolver cambian también el estado de pago del viaje,
    // en la misma transacción.
    public sealed class DisputaDAL
    {
        private const string Consulta = @"SELECT d.IdDisputa, d.IdViaje, d.Fecha, d.Motivo, d.Estado, d.Resolucion, d.FechaResolucion,
                d.ResueltaPor, v.Origen + N' → ' + v.Destino, ISNULL(da.RazonSocial, da.Nombre + ' ' + da.Apellido),
                ISNULL(t.RazonSocial, t.Nombre + ' ' + t.Apellido), v.TarifaAcordada - v.DescuentoDador, v.Estado
            FROM Disputas d
            JOIN Viajes v ON v.IdViaje = d.IdViaje
            JOIN Usuarios da ON da.IdUsuario = v.IdDador
            LEFT JOIN Usuarios t ON t.IdUsuario = v.IdTransportista ";

        // R13: solo si el viaje es del dador, se pagó con tarjeta y la plata sigue retenida.
        public bool Abrir(int idViaje, int idDador, string motivo)
        {
            const string sql = @"UPDATE Viajes SET EstadoPago = N'En disputa'
                WHERE IdViaje = @Viaje AND IdDador = @Dador AND MedioPago IN (N'Tarjeta', N'Saldo') AND EstadoPago = N'Retenido';
                IF @@ROWCOUNT = 1 INSERT INTO Disputas (IdViaje, Motivo) VALUES (@Viaje, @Motivo);";
            return EjecutorDAL.EjecutarCambio(sql, "Viajes", EjecutorDAL.Parametro("@Viaje", idViaje),
                EjecutorDAL.Parametro("@Dador", idDador), EjecutorDAL.Parametro("@Motivo", motivo)) == 2;
        }

        // idDador null = todas (el administrador).
        public List<DisputaBE> Listar(int? idDador)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE (@Dador IS NULL OR v.IdDador = @Dador) ORDER BY CASE WHEN d.Estado = N'Abierta' THEN 0 ELSE 1 END, d.Fecha DESC;",
                LeerDisputa, EjecutorDAL.Parametro("@Dador", idDador));
        }

        public DisputaBE Obtener(int idDisputa)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE d.IdDisputa = @Id;", LeerDisputa, EjecutorDAL.Parametro("@Id", idDisputa));
            return lista.Count == 0 ? null : lista[0];
        }

        // Cierra la disputa (si sigue abierta), deja el viaje en estadoPago y guarda el movimiento de plata.
        public bool Resolver(DisputaBE disputa, string estadoPago, TransaccionBE movimiento)
        {
            var sql = @"UPDATE Disputas SET Estado = @Estado, Resolucion = @Resolucion, FechaResolucion = SYSDATETIME(), ResueltaPor = @Admin
                WHERE IdDisputa = @Id AND Estado = N'Abierta';
                IF @@ROWCOUNT = 1
                BEGIN
                    UPDATE Viajes SET EstadoPago = @EstadoPago WHERE IdViaje = @Viaje AND EstadoPago = N'En disputa';
                    " + TransaccionDAL.Insertar + @"
                END";
            var parametros = new List<SqlParameter>(TransaccionDAL.Parametros(movimiento))
            {
                EjecutorDAL.Parametro("@Id", disputa.IdDisputa), EjecutorDAL.Parametro("@Estado", disputa.Estado),
                EjecutorDAL.Parametro("@Resolucion", disputa.Resolucion), EjecutorDAL.Parametro("@Admin", disputa.ResueltaPor),
                EjecutorDAL.Parametro("@EstadoPago", estadoPago), EjecutorDAL.Parametro("@Viaje", disputa.IdViaje)
            };
            return EjecutorDAL.EjecutarCambio(sql, "Viajes,Transacciones", parametros.ToArray()) == 3;
        }

        private static DisputaBE LeerDisputa(SqlDataReader lector)
        {
            return new DisputaBE
            {
                IdDisputa = lector.GetInt32(0),
                IdViaje = lector.GetInt32(1),
                Fecha = lector.GetDateTime(2),
                Motivo = lector.GetString(3),
                Estado = lector.GetString(4),
                Resolucion = EjecutorDAL.Texto(lector, 5),
                FechaResolucion = lector.IsDBNull(6) ? (System.DateTime?)null : lector.GetDateTime(6),
                ResueltaPor = EjecutorDAL.Texto(lector, 7),
                Recorrido = lector.GetString(8),
                Dador = lector.GetString(9),
                Transportista = EjecutorDAL.Texto(lector, 10),
                Monto = lector.GetDecimal(11),
                EstadoViaje = lector.GetString(12)
            };
        }
    }
}
