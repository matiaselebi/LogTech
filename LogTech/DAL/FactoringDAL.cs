using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Pedidos de factoring (CU-006-029 y 030). Aprobar guarda el pedido, deja el viaje «Adelantado» y acredita el adelanto
    // en la billetera, todo en la misma transacción.
    public sealed class FactoringDAL
    {
        // Datos del scoring: promedio de estrellas, viajes entregados y disputas perdidas.
        public decimal? PromedioCalificaciones(int idTransportista)
        {
            var valor = EjecutorDAL.LeerValor("SELECT AVG(CAST(Puntaje AS DECIMAL(4,2))) FROM Calificaciones WHERE IdTransportista = @Id;",
                EjecutorDAL.Parametro("@Id", idTransportista));
            return valor == null || valor is System.DBNull ? (decimal?)null : (decimal)valor;
        }

        public int ViajesEntregados(int idTransportista)
        {
            return (int)EjecutorDAL.LeerValor("SELECT COUNT(*) FROM Viajes WHERE IdTransportista = @Id AND Estado = N'Entregado';",
                EjecutorDAL.Parametro("@Id", idTransportista));
        }

        public int DisputasPerdidas(int idTransportista)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT COUNT(*) FROM Disputas d JOIN Viajes v ON v.IdViaje = d.IdViaje
                WHERE v.IdTransportista = @Id AND d.Estado = N'Devuelta al dador';", EjecutorDAL.Parametro("@Id", idTransportista));
        }

        // R15: lo que la entidad ya adelantó y todavía no recuperó (los dadores no pagaron esos viajes).
        public decimal Comprometido()
        {
            return (decimal)EjecutorDAL.LeerValor(@"SELECT ISNULL(SUM(s.MontoAdelantado), 0) FROM SolicitudesFactoring s
                JOIN Viajes v ON v.IdViaje = s.IdViaje WHERE s.Estado = N'Aprobada' AND v.EstadoPago = N'Adelantado';");
        }

        public void Rechazar(SolicitudFactoringBE s)
        {
            EjecutorDAL.EjecutarCambio(Insertar, null, Parametros(s));
        }

        // R14 en la base: solo si el viaje sigue a cobrar. Devuelve false si ya no lo estaba.
        public bool Aprobar(SolicitudFactoringBE s, TransaccionBE adelanto)
        {
            var parametros = new List<SqlParameter>(Parametros(s));
            parametros.AddRange(TransaccionDAL.Parametros(adelanto));
            return EjecutorDAL.EjecutarCambio("usp_Factoring_Aprobar", "Viajes,Transacciones", parametros.ToArray()) == 3;
        }

        public List<SolicitudFactoringBE> Listar(int idTransportista)
        {
            return EjecutorDAL.Leer(@"SELECT s.IdSolicitud, s.IdViaje, s.IdTransportista, s.Fecha, s.MontoViaje, s.Comision,
                    s.MontoAdelantado, s.Puntaje, s.Estado, s.Motivo, v.Origen + N' → ' + v.Destino
                FROM SolicitudesFactoring s JOIN Viajes v ON v.IdViaje = s.IdViaje
                WHERE s.IdTransportista = @Id ORDER BY s.Fecha DESC, s.IdSolicitud DESC;", l => new SolicitudFactoringBE
            {
                IdSolicitud = l.GetInt32(0),
                IdViaje = l.GetInt32(1),
                IdTransportista = l.GetInt32(2),
                Fecha = l.GetDateTime(3),
                MontoViaje = l.GetDecimal(4),
                Comision = l.GetDecimal(5),
                MontoAdelantado = l.GetDecimal(6),
                Puntaje = l.GetInt32(7),
                Estado = l.GetString(8),
                Motivo = EjecutorDAL.Texto(l, 9),
                Recorrido = l.GetString(10)
            }, EjecutorDAL.Parametro("@Id", idTransportista));
        }

        private const string Insertar = @"INSERT INTO SolicitudesFactoring (IdViaje, IdTransportista, MontoViaje, Comision, MontoAdelantado, Puntaje, Estado, Motivo)
                VALUES (@Viaje, @Transportista, @MontoViaje, @Comision, @Adelantado, @Puntaje, @Estado, @Motivo);";

        private static SqlParameter[] Parametros(SolicitudFactoringBE s)
        {
            return new[]
            {
                EjecutorDAL.Parametro("@Viaje", s.IdViaje), EjecutorDAL.Parametro("@Transportista", s.IdTransportista),
                EjecutorDAL.Parametro("@MontoViaje", s.MontoViaje), EjecutorDAL.Parametro("@Comision", s.Comision),
                EjecutorDAL.Parametro("@Adelantado", s.MontoAdelantado), EjecutorDAL.Parametro("@Puntaje", s.Puntaje),
                EjecutorDAL.Parametro("@Estado", s.Estado), EjecutorDAL.Parametro("@Motivo", s.Motivo)
            };
        }
    }
}
