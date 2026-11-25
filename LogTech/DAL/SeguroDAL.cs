using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Planes de las aseguradoras y seguros contratados (CU-006-031 a CU-006-034). El seguro es una fila de Suscripciones
    // de tipo «Protección de carga», así el tablero del administrador lo cuenta con el resto de las suscripciones.
    public sealed class SeguroDAL
    {
        private const string ConsultaPlan = "SELECT IdPlan, Aseguradora, Nombre, CoberturaPorViaje, ViajesPorMes, Precio, Detalle FROM PlanesSeguro ";

        public List<string> ListarAseguradoras()
        {
            return EjecutorDAL.Leer("SELECT DISTINCT Aseguradora FROM PlanesSeguro ORDER BY Aseguradora;", l => l.GetString(0));
        }

        public List<PlanSeguroBE> ListarPlanes(string aseguradora)
        {
            return EjecutorDAL.Leer(ConsultaPlan + "WHERE Aseguradora = @Aseguradora ORDER BY Precio;", l => LeerPlan(l, 0),
                EjecutorDAL.Parametro("@Aseguradora", aseguradora));
        }

        public PlanSeguroBE ObtenerPlan(int idPlan)
        {
            var lista = EjecutorDAL.Leer(ConsultaPlan + "WHERE IdPlan = @Id;", l => LeerPlan(l, 0), EjecutorDAL.Parametro("@Id", idPlan));
            return lista.Count == 0 ? null : lista[0];
        }

        // El seguro vigente del usuario (como mucho hay uno, R16), o null.
        public SeguroBE ObtenerActivo(int idUsuario)
        {
            var lista = EjecutorDAL.Leer(ConsultaSeguro + "WHERE s.IdUsuario = @Id AND s.FechaBaja IS NULL;", LeerSeguro,
                EjecutorDAL.Parametro("@Id", idUsuario));
            return lista.Count == 0 ? null : lista[0];
        }

        public List<SeguroBE> Listar(int idUsuario)
        {
            return EjecutorDAL.Leer(ConsultaSeguro + "WHERE s.IdUsuario = @Id ORDER BY s.FechaAlta DESC, s.IdSuscripcion DESC;", LeerSeguro,
                EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Rutina diaria: los seguros vigentes cuyo mes pagado ya terminó.
        public List<SeguroBE> ListarVencidos()
        {
            return EjecutorDAL.Leer(ConsultaSeguro + "WHERE s.FechaBaja IS NULL AND s.Vence <= SYSDATETIME();", LeerSeguro);
        }

        // Guarda el seguro y su pago en la misma transacción, solo si el usuario no tiene otro vigente (R16).
        // El bloqueo evita que dos pedidos al mismo tiempo dejen dos seguros activos.
        public bool Contratar(SeguroBE seguro, string suscriptor, TransaccionBE pago)
        {
            var parametros = new List<SqlParameter>(TransaccionDAL.Parametros(pago))
            {
                EjecutorDAL.Parametro("@Suscriptor", suscriptor), EjecutorDAL.Parametro("@Monto", seguro.Monto),
                EjecutorDAL.Parametro("@Usuario", seguro.IdUsuario), EjecutorDAL.Parametro("@Plan", seguro.Plan.IdPlan),
                EjecutorDAL.Parametro("@Tarjeta", seguro.IdTarjeta), EjecutorDAL.Parametro("@Poliza", seguro.NumeroPoliza),
                EjecutorDAL.Parametro("@Vence", seguro.Vence)
            };
            return EjecutorDAL.EjecutarCambio("usp_Seguro_Contratar", "Transacciones", parametros.ToArray()) == 2;
        }

        // CU-006-033: deja de renovarse; sigue vigente hasta el final del mes pagado.
        public bool Cancelar(int idUsuario)
        {
            return EjecutorDAL.EjecutarCambio(@"UPDATE Suscripciones SET CancelaAlVencer = 1
                WHERE IdUsuario = @Id AND IdPlanSeguro IS NOT NULL AND FechaBaja IS NULL AND CancelaAlVencer = 0;", null,
                EjecutorDAL.Parametro("@Id", idUsuario)) == 1;
        }

        // CU-006-032: el seguro pasa a otro plan con una póliza nueva; el ajuste prorrateado (R39) se suma al pendiente.
        public bool CambiarPlan(int idSuscripcion, PlanSeguroBE plan, string poliza, decimal ajuste)
        {
            return EjecutorDAL.EjecutarCambio(@"UPDATE Suscripciones SET IdPlanSeguro = @Plan, Monto = @Monto, NumeroPoliza = @Poliza,
                    AjustePendiente = AjustePendiente + @Ajuste
                WHERE IdSuscripcion = @Id AND IdPlanSeguro IS NOT NULL AND FechaBaja IS NULL AND CancelaAlVencer = 0;", null,
                EjecutorDAL.Parametro("@Plan", plan.IdPlan), EjecutorDAL.Parametro("@Monto", plan.Precio),
                EjecutorDAL.Parametro("@Poliza", poliza), EjecutorDAL.Parametro("@Ajuste", ajuste),
                EjecutorDAL.Parametro("@Id", idSuscripcion)) == 1;
        }

        // Renovación: un mes más y el pago, juntos. El ajuste de un cambio de plan ya se cobró: queda en lo que sobre.
        public bool Renovar(SeguroBE seguro, DateTime nuevoVence, decimal ajusteQueSigue, TransaccionBE pago)
        {
            var sql = @"UPDATE Suscripciones SET Vence = @Nuevo, AjustePendiente = @Ajuste
                WHERE IdSuscripcion = @Id AND FechaBaja IS NULL AND Vence = @Vence;
                IF @@ROWCOUNT = 1 " + TransaccionDAL.Insertar;
            var parametros = new List<SqlParameter>(TransaccionDAL.Parametros(pago))
            {
                EjecutorDAL.Parametro("@Id", seguro.IdSuscripcion), EjecutorDAL.Parametro("@Vence", seguro.Vence),
                EjecutorDAL.Parametro("@Nuevo", nuevoVence), EjecutorDAL.Parametro("@Ajuste", ajusteQueSigue)
            };
            return EjecutorDAL.EjecutarCambio(sql, "Transacciones", parametros.ToArray()) == 2;
        }

        // Fin del seguro (cancelado o sin tarjeta para renovar): la baja queda en la fecha en que venció.
        public void Terminar(int idSuscripcion)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Suscripciones SET FechaBaja = Vence WHERE IdSuscripcion = @Id AND FechaBaja IS NULL;", null,
                EjecutorDAL.Parametro("@Id", idSuscripcion));
        }

        private const string ConsultaSeguro = @"SELECT p.IdPlan, p.Aseguradora, p.Nombre, p.CoberturaPorViaje, p.ViajesPorMes, p.Precio,
                p.Detalle, s.IdSuscripcion, s.IdUsuario, s.Monto, s.FechaAlta, s.Vence, s.CancelaAlVencer, s.FechaBaja,
                s.NumeroPoliza, s.IdTarjeta, t.Marca + N' terminada en ' + t.Ultimos4, s.AjustePendiente
            FROM Suscripciones s
            JOIN PlanesSeguro p ON p.IdPlan = s.IdPlanSeguro
            LEFT JOIN Tarjetas t ON t.IdTarjeta = s.IdTarjeta ";

        private static PlanSeguroBE LeerPlan(SqlDataReader l, int c)
        {
            return new PlanSeguroBE
            {
                IdPlan = l.GetInt32(c),
                Aseguradora = l.GetString(c + 1),
                Nombre = l.GetString(c + 2),
                CoberturaPorViaje = l.GetDecimal(c + 3),
                ViajesPorMes = EjecutorDAL.Entero(l, c + 4),
                Precio = l.GetDecimal(c + 5),
                Detalle = l.GetString(c + 6)
            };
        }

        private static SeguroBE LeerSeguro(SqlDataReader l)
        {
            return new SeguroBE
            {
                Plan = LeerPlan(l, 0),
                IdSuscripcion = l.GetInt32(7),
                IdUsuario = l.GetInt32(8),
                Monto = l.GetDecimal(9),
                FechaAlta = l.GetDateTime(10),
                Vence = l.GetDateTime(11),
                CancelaAlVencer = l.GetBoolean(12),
                FechaBaja = l.IsDBNull(13) ? (DateTime?)null : l.GetDateTime(13),
                NumeroPoliza = EjecutorDAL.Texto(l, 14),
                IdTarjeta = EjecutorDAL.Entero(l, 15),
                Tarjeta = EjecutorDAL.Texto(l, 16),
                AjustePendiente = l.GetDecimal(17)
            };
        }
    }
}
