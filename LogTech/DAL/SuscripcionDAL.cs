using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Suscripciones Flota SaaS (por vehículo) y Premium transportista (CU-008-040 a 043). Comparten la tabla Suscripciones con los
    // seguros (que tienen IdPlanSeguro) y con los datos de ejemplo del tablero (sin IdUsuario).
    public sealed class SuscripcionDAL
    {
        private const string Consulta = @"SELECT s.IdSuscripcion, s.IdUsuario, s.Tipo, s.CantidadPatentes, s.Monto, s.FechaAlta, s.FechaBaja,
                s.IdTarjeta, t.Marca + N' terminada en ' + t.Ultimos4, s.Vence, s.CancelaAlVencer
            FROM Suscripciones s
            LEFT JOIN Tarjetas t ON t.IdTarjeta = s.IdTarjeta
            WHERE s.IdUsuario IS NOT NULL AND s.IdPlanSeguro IS NULL ";

        // La vigente de la cuenta (activa o pendiente de baja), o null.
        public SuscripcionBE ObtenerVigente(int idUsuario, string tipo)
        {
            var lista = EjecutorDAL.Leer(Consulta + "AND s.IdUsuario = @Id AND s.Tipo = @Tipo AND s.FechaBaja IS NULL;", Leer,
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Tipo", tipo));
            return lista.Count == 0 ? null : lista[0];
        }

        // Rutina diaria: las vigentes cuyo mes pagado ya terminó.
        public List<SuscripcionBE> ListarVencidas()
        {
            return EjecutorDAL.Leer(Consulta + "AND s.FechaBaja IS NULL AND s.Vence <= SYSDATETIME();", Leer);
        }

        // Los transportistas con Premium vigente (para las alertas de cargas de alta rentabilidad).
        public List<int> ListarPremium()
        {
            return EjecutorDAL.Leer(@"SELECT IdUsuario FROM Suscripciones WHERE Tipo = N'Premium transportista' AND IdUsuario IS NOT NULL
                AND IdPlanSeguro IS NULL AND FechaBaja IS NULL;", lector => lector.GetInt32(0));
        }

        // Guarda la suscripción y el pago del primer mes juntos, solo si la cuenta no tiene otra igual vigente.
        // Devuelve el número de la suscripción, o 0 si ya tenía una.
        // pasaAFlota: el dador que contrata la Flota SaaS pasa a ser Administrador de Flota en la misma transacción.
        public int Contratar(SuscripcionBE suscripcion, string suscriptor, TransaccionBE pago, bool pasaAFlota)
        {
            var parametros = new List<SqlParameter>(TransaccionDAL.Parametros(pago))
            {
                EjecutorDAL.Parametro("@Suscriptor", suscriptor), EjecutorDAL.Parametro("@Tipo", suscripcion.Tipo),
                EjecutorDAL.Parametro("@Patentes", suscripcion.CantidadPatentes), EjecutorDAL.Parametro("@Monto", suscripcion.Monto),
                EjecutorDAL.Parametro("@Usuario", suscripcion.IdUsuario), EjecutorDAL.Parametro("@Tarjeta", suscripcion.IdTarjeta),
                EjecutorDAL.Parametro("@Vence", suscripcion.Vence), EjecutorDAL.Parametro("@PasaAFlota", pasaAFlota)
            };
            return Convert.ToInt32(EjecutorDAL.CambiarYLeer("usp_Suscripcion_Contratar", parametros.ToArray()));
        }

        // CU-008-041: con qué se paga desde ahora (null = el saldo de la billetera).
        public bool CambiarMedioDePago(int idSuscripcion, int? idTarjeta)
        {
            return EjecutorDAL.EjecutarCambio("UPDATE Suscripciones SET IdTarjeta = @Tarjeta WHERE IdSuscripcion = @Id AND FechaBaja IS NULL;", null,
                EjecutorDAL.Parametro("@Tarjeta", idTarjeta), EjecutorDAL.Parametro("@Id", idSuscripcion)) == 1;
        }

        // CU-008-042: deja de renovarse; sigue vigente hasta el final del mes pagado.
        public bool Cancelar(int idSuscripcion)
        {
            return EjecutorDAL.EjecutarCambio(@"UPDATE Suscripciones SET CancelaAlVencer = 1
                WHERE IdSuscripcion = @Id AND FechaBaja IS NULL AND CancelaAlVencer = 0;", null,
                EjecutorDAL.Parametro("@Id", idSuscripcion)) == 1;
        }

        // Renovación: un mes más y el pago, juntos. La Flota SaaS queda con los vehículos que se cobraron.
        public bool Renovar(SuscripcionBE suscripcion, DateTime nuevoVence, TransaccionBE pago)
        {
            var sql = @"UPDATE Suscripciones SET Vence = @Nuevo, CantidadPatentes = @Patentes, Monto = @Monto
                WHERE IdSuscripcion = @Id AND FechaBaja IS NULL AND Vence = @Vence;
                IF @@ROWCOUNT = 1 " + TransaccionDAL.Insertar;
            var parametros = new List<SqlParameter>(TransaccionDAL.Parametros(pago))
            {
                EjecutorDAL.Parametro("@Id", suscripcion.IdSuscripcion), EjecutorDAL.Parametro("@Vence", suscripcion.Vence),
                EjecutorDAL.Parametro("@Nuevo", nuevoVence), EjecutorDAL.Parametro("@Patentes", suscripcion.CantidadPatentes),
                EjecutorDAL.Parametro("@Monto", suscripcion.Monto)
            };
            return EjecutorDAL.EjecutarCambio(sql, "Transacciones", parametros.ToArray()) == 2;
        }

        // Fin de la suscripción (dada de baja o sin tarjeta para renovar): la baja queda en la fecha en que venció.
        public void Terminar(int idSuscripcion)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Suscripciones SET FechaBaja = Vence WHERE IdSuscripcion = @Id AND FechaBaja IS NULL;", null,
                EjecutorDAL.Parametro("@Id", idSuscripcion));
        }

        private static SuscripcionBE Leer(SqlDataReader lector)
        {
            return new SuscripcionBE
            {
                IdSuscripcion = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Tipo = lector.GetString(2),
                CantidadPatentes = EjecutorDAL.Entero(lector, 3),
                Monto = lector.GetDecimal(4),
                FechaAlta = lector.GetDateTime(5),
                FechaBaja = lector.IsDBNull(6) ? (DateTime?)null : lector.GetDateTime(6),
                IdTarjeta = EjecutorDAL.Entero(lector, 7),
                Tarjeta = EjecutorDAL.Texto(lector, 8),
                Vence = lector.GetDateTime(9),
                CancelaAlVencer = lector.GetBoolean(10)
            };
        }
    }
}
