using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class EstadisticaDAL
    {
        // Lo que cobra LogTech por mes de cada suscripción: el abono entero, salvo los seguros (Protección de carga), que
        // los cobra la aseguradora y le paga a LogTech el 5 % de la prima (AseguradoraServicio.ComisionLogTech).
        private const string IngresoLogTech = "CASE WHEN Tipo = N'Protección de carga' THEN Monto * 0.05 ELSE Monto END";

        public ResumenTableroBE ObtenerResumen(DateTime inicioMes)
        {
            const string sql = @"SELECT
                (SELECT COUNT(*) FROM Suscripciones WHERE FechaBaja IS NULL),
                (SELECT ISNULL(SUM(" + IngresoLogTech + @"), 0) FROM Suscripciones WHERE FechaBaja IS NULL),
                (SELECT COUNT(*) FROM Suscripciones WHERE FechaAlta >= @InicioMes),
                (SELECT COUNT(*) FROM Viajes WHERE FechaPedido >= @InicioMes AND Estado <> N'Cancelado'),
                (SELECT ISNULL(SUM(TarifaAcordada), 0) FROM Viajes WHERE FechaPedido >= @InicioMes AND Estado <> N'Cancelado'),
                (SELECT ISNULL(SUM(Comision), 0) FROM Viajes WHERE FechaPedido >= @InicioMes AND Estado <> N'Cancelado'),
                (SELECT COUNT(*) FROM Suscripciones WHERE Tipo = N'Protección de carga' AND FechaBaja IS NULL),
                (SELECT ISNULL(SUM(Monto * 0.05), 0) FROM Suscripciones WHERE Tipo = N'Protección de carga' AND FechaBaja IS NULL);";

            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@InicioMes", SqlDbType.DateTime2).Value = inicioMes;
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    lector.Read();
                    return new ResumenTableroBE
                    {
                        SuscriptoresActivos = lector.GetInt32(0),
                        IngresoMensualSuscriptores = lector.GetDecimal(1),
                        SuscripcionesDelMes = lector.GetInt32(2),
                        ViajesDelMes = lector.GetInt32(3),
                        MontoViajesDelMes = lector.GetDecimal(4),
                        ComisionesDelMes = lector.GetDecimal(5),
                        SegurosActivos = lector.GetInt32(6),
                        ComisionSeguros = lector.GetDecimal(7)
                    };
                }
            }
        }

        // tipo vacío = todas las suscripciones.
        public IList<PuntoEstadisticaBE> SuscripcionesPorPeriodo(PeriodoEstadistica periodo, DateTime desde, string tipo)
        {
            var grupo = Agrupar("FechaAlta", periodo);
            return LeerSerie("SELECT " + grupo + ", COUNT(*), SUM(" + IngresoLogTech + ") FROM Suscripciones " +
                "WHERE FechaAlta >= @Desde AND (@Tipo = '' OR Tipo = @Tipo) GROUP BY " + grupo + ";",
                new SqlParameter("@Desde", SqlDbType.DateTime2) { Value = desde },
                new SqlParameter("@Tipo", SqlDbType.NVarChar, 30) { Value = tipo });
        }

        // Viajes (sin los cancelados) con un peso desde pesoDesde (incluido) hasta pesoHasta (sin incluir).
        public IList<PuntoEstadisticaBE> ViajesPorPeriodo(PeriodoEstadistica periodo, DateTime desde, decimal pesoDesde, decimal pesoHasta)
        {
            var grupo = Agrupar("FechaPedido", periodo);
            return LeerSerie("SELECT " + grupo + ", COUNT(*), SUM(TarifaAcordada) FROM Viajes " +
                "WHERE FechaPedido >= @Desde AND PesoToneladas >= @PesoDesde AND PesoToneladas < @PesoHasta AND Estado <> N'Cancelado' " +
                "GROUP BY " + grupo + ";",
                new SqlParameter("@Desde", SqlDbType.DateTime2) { Value = desde },
                new SqlParameter("@PesoDesde", SqlDbType.Decimal) { Value = pesoDesde },
                new SqlParameter("@PesoHasta", SqlDbType.Decimal) { Value = pesoHasta });
        }

        // Patentes de las suscripciones Flota SaaS vigentes en una fecha y lo que pagan por mes.
        public PuntoEstadisticaBE PatentesActivas(DateTime fecha)
        {
            const string sql = @"SELECT ISNULL(SUM(CantidadPatentes), 0), ISNULL(SUM(Monto), 0)
                FROM Suscripciones
                WHERE Tipo = N'Flota SaaS' AND FechaAlta < @Fecha AND (FechaBaja IS NULL OR FechaBaja >= @Fecha);";

            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value = fecha;
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    lector.Read();
                    return new PuntoEstadisticaBE { Cantidad = lector.GetInt32(0), Monto = lector.GetDecimal(1) };
                }
            }
        }

        // Suscriptores activos de cada tipo y lo que pagan por mes.
        public IList<TotalEstadisticaBE> SuscriptoresPorTipo()
        {
            return LeerTotales(@"SELECT CASE WHEN Tipo = N'Protección de carga' THEN N'Seguros (5 % de la prima)' ELSE Tipo END,
                COUNT(*), ISNULL(SUM(" + IngresoLogTech + @"), 0), 0 FROM Suscripciones
                WHERE FechaBaja IS NULL GROUP BY Tipo ORDER BY Tipo;");
        }

        // tipo vacío = todas las suscripciones.
        public TotalEstadisticaBE TotalSuscriptores(string tipo)
        {
            return LeerTotales(@"SELECT N'Suscriptores', COUNT(*), ISNULL(SUM(" + IngresoLogTech + @"), 0), 0 FROM Suscripciones
                WHERE FechaBaja IS NULL AND (@Tipo = '' OR Tipo = @Tipo);",
                new SqlParameter("@Tipo", SqlDbType.NVarChar, 30) { Value = tipo })[0];
        }

        // Viajes pedidos (sin los cancelados) desde una fecha, con un peso desde pesoDesde (incluido) hasta pesoHasta (sin incluir).
        public TotalEstadisticaBE TotalViajes(DateTime desde, decimal pesoDesde, decimal pesoHasta)
        {
            return LeerTotales(@"SELECT N'Viajes', COUNT(*), ISNULL(SUM(TarifaAcordada), 0), ISNULL(SUM(Comision), 0)
                FROM Viajes WHERE FechaPedido >= @Desde AND PesoToneladas >= @PesoDesde AND PesoToneladas < @PesoHasta
                AND Estado <> N'Cancelado';",
                new SqlParameter("@Desde", SqlDbType.DateTime2) { Value = desde },
                new SqlParameter("@PesoDesde", SqlDbType.Decimal) { Value = pesoDesde },
                new SqlParameter("@PesoHasta", SqlDbType.Decimal) { Value = pesoHasta })[0];
        }

        // Remitos digitales que cobró LogTech desde una fecha ($ 1.500 cada uno, dentro del precio del viaje).
        public TotalEstadisticaBE RemitosDelMes(DateTime desde)
        {
            return LeerTotales(@"SELECT N'Remitos digitales', COUNT(*), ISNULL(SUM(CargoRemito), 0), 0
                FROM Viajes WHERE FechaPedido >= @Desde AND CargoRemito > 0 AND Estado <> N'Cancelado';",
                new SqlParameter("@Desde", SqlDbType.DateTime2) { Value = desde })[0];
        }

        // Comisiones del factoring (4 %) de los adelantos aprobados desde una fecha.
        public TotalEstadisticaBE FactoringDelMes(DateTime desde)
        {
            return LeerTotales(@"SELECT N'Comisiones de factoring', COUNT(*), ISNULL(SUM(Comision), 0), 0
                FROM SolicitudesFactoring WHERE Estado = N'Aprobada' AND Fecha >= @Desde;",
                new SqlParameter("@Desde", SqlDbType.DateTime2) { Value = desde })[0];
        }

        // Reporte de la flota: cuántas incidencias tuvo cada viaje de la flota (solo los que tuvieron alguna).
        public Dictionary<int, int> IncidenciasPorViaje(int idFlota)
        {
            var incidencias = new Dictionary<int, int>();
            foreach (var fila in EjecutorDAL.Leer(@"SELECT i.IdViaje, COUNT(*) FROM Incidencias i
                JOIN Viajes v ON v.IdViaje = i.IdViaje WHERE v.IdTransportista = @Flota GROUP BY i.IdViaje;",
                lector => new KeyValuePair<int, int>(lector.GetInt32(0), lector.GetInt32(1)),
                EjecutorDAL.Parametro("@Flota", idFlota)))
            {
                incidencias[fila.Key] = fila.Value;
            }
            return incidencias;
        }

        // Lee filas de 4 columnas: nombre, cantidad, monto y comisión.
        private static IList<TotalEstadisticaBE> LeerTotales(string sql, params SqlParameter[] parametros)
        {
            var totales = new List<TotalEstadisticaBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        totales.Add(new TotalEstadisticaBE
                        {
                            Nombre = lector.GetString(0),
                            Cantidad = lector.GetInt32(1),
                            Monto = lector.GetDecimal(2),
                            Comision = Convert.ToDecimal(lector.GetValue(3))
                        });
                    }
                }
            }
            return totales;
        }

        // Expresión SQL que pasa la fecha a la clave del período. Arma el SQL solo con
        // textos fijos de este archivo (nada que escriba el usuario): no hay inyección SQL.
        private static string Agrupar(string columna, PeriodoEstadistica periodo)
        {
            if (periodo == PeriodoEstadistica.Diario)
                return "CONVERT(CHAR(10), " + columna + ", 23)";     // 2026-10-07
            if (periodo == PeriodoEstadistica.Mensual)
                return "CONVERT(CHAR(7), " + columna + ", 23)";      // 2026-10
            return "CONVERT(CHAR(4), YEAR(" + columna + "))";        // 2026
        }

        private static IList<PuntoEstadisticaBE> LeerSerie(string sql, params SqlParameter[] parametros)
        {
            var puntos = new List<PuntoEstadisticaBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        puntos.Add(new PuntoEstadisticaBE
                        {
                            Clave = lector.GetString(0),
                            Cantidad = lector.GetInt32(1),
                            Monto = lector.GetDecimal(2)
                        });
                    }
                }
            }
            return puntos;
        }
    }
}
