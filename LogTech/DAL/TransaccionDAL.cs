using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Movimientos de plata (tabla protegida con dígito verificador). Las operaciones que además cambian
    // el estado de pago de un viaje se hacen en la misma transacción, para que nunca quede una sin la otra.
    public sealed class TransaccionDAL
    {
        // INSERT de un movimiento; lo usan también ViajeDAL y DisputaDAL dentro de sus transacciones.
        internal const string Insertar = @"INSERT INTO Transacciones (IdUsuario, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta, Detalle, Comprobante)
                VALUES (@TUsuario, @TTipo, @TMonto, @TAfecta, @TViaje, @TTarjeta, @TDetalle, @TComprobante);";

        // prefijo: para guardar dos movimientos en el mismo SQL (el segundo usa otro, por ejemplo "@C").
        internal static SqlParameter[] Parametros(TransaccionBE t, string prefijo = "@T")
        {
            return new[]
            {
                EjecutorDAL.Parametro(prefijo + "Usuario", t.IdUsuario), EjecutorDAL.Parametro(prefijo + "Tipo", t.Tipo),
                EjecutorDAL.Parametro(prefijo + "Monto", t.Monto), EjecutorDAL.Parametro(prefijo + "Afecta", t.AfectaSaldo),
                EjecutorDAL.Parametro(prefijo + "Viaje", t.IdViaje), EjecutorDAL.Parametro(prefijo + "Tarjeta", t.IdTarjeta),
                EjecutorDAL.Parametro(prefijo + "Detalle", t.Detalle), EjecutorDAL.Parametro(prefijo + "Comprobante", t.Comprobante)
            };
        }

        // Carga de saldo.
        public void Registrar(TransaccionBE transaccion)
        {
            EjecutorDAL.EjecutarCambio(Insertar, "Transacciones", Parametros(transaccion));
        }

        // R12: el retiro se guarda solo si el saldo alcanza. El bloqueo (UPDLOCK) evita que dos retiros
        // al mismo tiempo dejen la billetera en negativo.
        public bool RetirarSiAlcanza(TransaccionBE retiro)
        {
            const string sql = @"IF (SELECT ISNULL(SUM(Monto), 0) FROM Transacciones WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdUsuario = @TUsuario AND AfectaSaldo = 1) + @TMonto >= 0 " + Insertar;
            return EjecutorDAL.EjecutarCambio(sql, "Transacciones", Parametros(retiro)) == 1;
        }

        // Cambia el estado de pago del viaje (solo si sigue en estadoActual) y guarda el movimiento.
        public bool RegistrarConViaje(int idViaje, string estadoActual, string estadoNuevo, TransaccionBE transaccion)
        {
            var sql = @"UPDATE Viajes SET EstadoPago = @Nuevo WHERE IdViaje = @Viaje AND EstadoPago = @Actual;
                IF @@ROWCOUNT = 1 " + Insertar;
            var parametros = new List<SqlParameter>(Parametros(transaccion))
            {
                EjecutorDAL.Parametro("@Viaje", idViaje), EjecutorDAL.Parametro("@Actual", estadoActual),
                EjecutorDAL.Parametro("@Nuevo", estadoNuevo)
            };
            return EjecutorDAL.EjecutarCambio(sql, "Viajes,Transacciones", parametros.ToArray()) == 2;
        }

        // El dador pagó en el punto de pago: el viaje queda liberado, se guarda su pago y, si no se adelantó con
        // factoring, el cobro del transportista (cobro null = ya lo había cobrado la entidad que adelanta).
        public bool PagarEnPuntoDePago(int idViaje, string estadoActual, TransaccionBE pago, TransaccionBE cobro)
        {
            var sql = @"UPDATE Viajes SET EstadoPago = N'Liberado' WHERE IdViaje = @Viaje AND EstadoPago = @Actual;
                IF @@ROWCOUNT = 1
                BEGIN
                    " + Insertar + (cobro == null ? "" : @"
                    " + Insertar.Replace("@T", "@C")) + @"
                END";
            var parametros = new List<SqlParameter>(Parametros(pago))
            {
                EjecutorDAL.Parametro("@Viaje", idViaje), EjecutorDAL.Parametro("@Actual", estadoActual)
            };
            if (cobro != null) parametros.AddRange(Parametros(cobro, "@C"));
            return EjecutorDAL.EjecutarCambio(sql, "Viajes,Transacciones", parametros.ToArray()) == (cobro == null ? 2 : 3);
        }

        public decimal Saldo(int idUsuario)
        {
            return (decimal)EjecutorDAL.LeerValor("SELECT ISNULL(SUM(Monto), 0) FROM Transacciones WHERE IdUsuario = @Id AND AfectaSaldo = 1;",
                EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Plata retenida en escrow: la que pagó el dador con tarjeta o con saldo, o la que va a cobrar el transportista.
        public decimal EnEscrow(int idUsuario)
        {
            return (decimal)EjecutorDAL.LeerValor(@"SELECT
                    ISNULL(SUM(CASE WHEN IdDador = @Id THEN TarifaAcordada - DescuentoDador ELSE 0 END), 0)
                  + ISNULL(SUM(CASE WHEN IdTransportista = @Id THEN TarifaAcordada - Comision - DescuentoDador - CargoRemito ELSE 0 END), 0)
                FROM Viajes
                WHERE MedioPago IN (N'Tarjeta', N'Saldo') AND EstadoPago IN (N'Retenido', N'En disputa') AND (IdDador = @Id OR IdTransportista = @Id);",
                EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Administrador: toda la plata retenida en escrow (viajes con tarjeta o saldo todavía sin liberar) y cuántos viajes son.
        public TotalEstadisticaBE TotalEnEscrow()
        {
            return EjecutorDAL.Leer(@"SELECT COUNT(*), ISNULL(SUM(TarifaAcordada - DescuentoDador), 0) FROM Viajes
                WHERE MedioPago IN (N'Tarjeta', N'Saldo') AND EstadoPago IN (N'Retenido', N'En disputa');",
                l => new TotalEstadisticaBE { Nombre = "Escrow", Cantidad = l.GetInt32(0), Monto = l.GetDecimal(1) })[0];
        }

        // Los últimos movimientos, del más nuevo al más viejo.
        // ponytail: muestra los últimos 200; si hace falta todo el historial, paginar como la bitácora.
        public List<TransaccionBE> Listar(int idUsuario)
        {
            return EjecutorDAL.Leer(@"SELECT TOP (200) IdTransaccion, IdUsuario, Fecha, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta,
                    Detalle, Comprobante
                FROM Transacciones WHERE IdUsuario = @Id ORDER BY Fecha DESC, IdTransaccion DESC;",
                LeerTransaccion, EjecutorDAL.Parametro("@Id", idUsuario));
        }

        private static TransaccionBE LeerTransaccion(SqlDataReader lector)
        {
            return new TransaccionBE
            {
                IdTransaccion = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Fecha = lector.GetDateTime(2),
                Tipo = lector.GetString(3),
                Monto = lector.GetDecimal(4),
                AfectaSaldo = lector.GetBoolean(5),
                IdViaje = EjecutorDAL.Entero(lector, 6),
                IdTarjeta = EjecutorDAL.Entero(lector, 7),
                Detalle = lector.GetString(8),
                Comprobante = EjecutorDAL.Texto(lector, 9)
            };
        }
    }
}
