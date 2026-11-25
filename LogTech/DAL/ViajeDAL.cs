using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    // Ofertas de carga y viajes (la misma tabla), con su historial y sus incidencias.
    public sealed class ViajeDAL
    {
        // Estados en los que el viaje ocupa al vehículo y al conductor (R6 y R8).
        internal const string EstadosEnCurso = "(N'Asignado', N'En camino a carga', N'En origen', N'En tránsito', N'En destino')";

        private const string Consulta = @"SELECT v.IdViaje, v.FechaPedido, v.IdDador,
                ISNULL(d.RazonSocial, d.Nombre + ' ' + d.Apellido), v.Origen, v.Destino, v.Mercaderia,
                v.PesoToneladas, v.FechaCarga, v.TarifaAcordada, v.Comision, v.Estado, v.IdTransportista,
                ISNULL(t.RazonSocial, t.Nombre + ' ' + t.Apellido), v.IdVehiculo, ve.Patente, v.IdConductor,
                c.Apellido + ', ' + c.Nombre,
                CASE WHEN EXISTS (SELECT 1 FROM Remitos r WHERE r.IdViaje = v.IdViaje) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM Calificaciones k WHERE k.IdViaje = v.IdViaje) THEN 1 ELSE 0 END,
                v.MedioPago, v.IdTarjeta, v.DescuentoDador, v.CargoRemito, v.EstadoPago,
                (SELECT r.FechaEmision FROM Remitos r WHERE r.IdViaje = v.IdViaje)
            FROM Viajes v
            LEFT JOIN Usuarios d ON d.IdUsuario = v.IdDador
            LEFT JOIN Usuarios t ON t.IdUsuario = v.IdTransportista
            LEFT JOIN Vehiculos ve ON ve.IdVehiculo = v.IdVehiculo
            LEFT JOIN Conductores c ON c.IdConductor = v.IdConductor ";

        public ViajeBE Obtener(int idViaje)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE v.IdViaje = @Id;", LeerViaje,
                EjecutorDAL.Parametro("@Id", idViaje));
            return lista.Count == 0 ? null : lista[0];
        }

        // Ofertas del dador; estado vacío = todas.
        public List<ViajeBE> ListarDelDador(int idDador, string estado)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE v.IdDador = @Dador AND (@Estado = N'' OR v.Estado = @Estado) " +
                "ORDER BY v.FechaPedido DESC;", LeerViaje,
                EjecutorDAL.Parametro("@Dador", idDador), EjecutorDAL.Parametro("@Estado", estado ?? string.Empty));
        }

        // Bolsa de cargas: ofertas pendientes; origen o destino vacío = cualquiera.
        public List<ViajeBE> ListarPendientes(string origen, string destino, decimal pesoMaximo)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE v.Estado = N'Pendiente' AND v.IdDador IS NOT NULL " +
                "AND (@Origen = N'' OR v.Origen = @Origen) AND (@Destino = N'' OR v.Destino = @Destino) " +
                "AND v.PesoToneladas <= @Peso ORDER BY v.FechaCarga, v.IdViaje;", LeerViaje,
                EjecutorDAL.Parametro("@Origen", origen ?? string.Empty),
                EjecutorDAL.Parametro("@Destino", destino ?? string.Empty),
                EjecutorDAL.Parametro("@Peso", pesoMaximo));
        }

        // Viajes tomados por un transportista o una flota: en curso o ya entregados.
        public List<ViajeBE> ListarDelTransportista(int idTransportista, bool entregados)
        {
            var condicion = entregados ? "v.Estado = N'Entregado'" : "v.Estado IN " + EstadosEnCurso;
            return EjecutorDAL.Leer(Consulta + "WHERE v.IdTransportista = @Id AND " + condicion +
                " ORDER BY v.FechaCarga DESC, v.IdViaje DESC;", LeerViaje, EjecutorDAL.Parametro("@Id", idTransportista));
        }

        // Viajes en efectivo ya entregados que el dador todavía tiene que pagar en el punto de pago.
        public List<ViajeBE> ListarAPagar(int idDador)
        {
            return EjecutorDAL.Leer(Consulta + "WHERE v.IdDador = @Dador AND v.MedioPago = N'Efectivo' " +
                "AND v.EstadoPago IN (N'A cobrar', N'Adelantado') ORDER BY v.FechaCarga, v.IdViaje;", LeerViaje,
                EjecutorDAL.Parametro("@Dador", idDador));
        }

        // Cambia el estado de pago solo si sigue en estadoActual.
        public bool CambiarEstadoPago(int idViaje, string estadoActual, string estadoNuevo)
        {
            return EjecutorDAL.EjecutarCambio("UPDATE Viajes SET EstadoPago = @Nuevo WHERE IdViaje = @Id AND EstadoPago = @Actual;", "Viajes",
                EjecutorDAL.Parametro("@Id", idViaje), EjecutorDAL.Parametro("@Actual", estadoActual),
                EjecutorDAL.Parametro("@Nuevo", estadoNuevo)) == 1;
        }

        // Administrador: todos los viajes en curso.
        public List<ViajeBE> ListarEnCurso()
        {
            return EjecutorDAL.Leer(Consulta + "WHERE v.Estado IN " + EstadosEnCurso + " ORDER BY v.FechaCarga, v.IdViaje;", LeerViaje);
        }

        // R38 (ERR041): el usuario tiene una carga en viaje (en tránsito o en destino), como dador o como transportista.
        public bool TieneCargaEnViaje(int idUsuario)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT COUNT(*) FROM Viajes WHERE Estado IN (N'En tránsito', N'En destino')
                AND (IdDador = @Id OR IdTransportista = @Id);", EjecutorDAL.Parametro("@Id", idUsuario)) > 0;
        }

        // R23 (ERR025): el usuario tiene viajes en curso (como transportista o flota), ofertas pendientes o en curso
        // (como dador), plata sin liquidar de un viaje (retenida, en disputa, a cobrar o adelantada con factoring) o un
        // reclamo abierto.
        public bool TieneOperacionesActivas(int idUsuario)
        {
            return (int)EjecutorDAL.LeerValor(@"SELECT COUNT(*) FROM Viajes v
                WHERE (v.IdTransportista = @Id AND v.Estado IN " + EstadosEnCurso + @")
                   OR (v.IdDador = @Id AND (v.Estado = N'Pendiente' OR v.Estado IN " + EstadosEnCurso + @"))
                   OR ((v.IdDador = @Id OR v.IdTransportista = @Id)
                       AND v.EstadoPago IN (N'Retenido', N'En disputa', N'A cobrar', N'Adelantado'))
                   OR ((v.IdDador = @Id OR v.IdTransportista = @Id)
                       AND EXISTS (SELECT 1 FROM Disputas d WHERE d.IdViaje = v.IdViaje AND d.Estado = N'Abierta'));",
                EjecutorDAL.Parametro("@Id", idUsuario)) > 0;
        }

        public void Publicar(ViajeBE viaje)
        {
            EjecutorDAL.EjecutarCambio("usp_Viaje_Publicar", "Viajes", ParametrosOferta(viaje));
        }

        // R2: solo cambia si la oferta sigue pendiente. Devuelve false si ya la tomaron.
        public bool Modificar(ViajeBE viaje)
        {
            const string sql = @"UPDATE Viajes SET Origen = @Origen, Destino = @Destino, Mercaderia = @Mercaderia,
                    PesoToneladas = @Peso, FechaCarga = @FechaCarga, TarifaAcordada = @Tarifa, Comision = @Comision,
                    MedioPago = @MedioPago, IdTarjeta = @Tarjeta, DescuentoDador = @Descuento, CargoRemito = @Remito
                WHERE IdViaje = @Id AND IdDador = @Dador AND Estado = N'Pendiente';";
            var parametros = new List<SqlParameter>(ParametrosOferta(viaje));
            parametros.Add(EjecutorDAL.Parametro("@Id", viaje.IdViaje));
            return EjecutorDAL.EjecutarCambio(sql, "Viajes", parametros.ToArray()) == 1;
        }

        // Cambia el estado solo si el viaje sigue en el estado que vio el usuario (si otro
        // usuario lo cambió un instante antes, no hace nada y devuelve false).
        public bool CambiarEstado(int idViaje, string estadoActual, string estadoNuevo)
        {
            return EjecutorDAL.EjecutarCambio("usp_Viaje_CambiarEstado", "Viajes",
                EjecutorDAL.Parametro("@Id", idViaje), EjecutorDAL.Parametro("@Actual", estadoActual),
                EjecutorDAL.Parametro("@Nuevo", estadoNuevo)) == 1;
        }

        // CU-001-005. R2: solo si sigue pendiente. Si el viaje se paga con tarjeta, en la misma transacción
        // queda retenido el pago del dador (CU-005-025): «retencion» es ese movimiento (null en efectivo).
        // sinComision: el transportista es Premium y no paga el 7 %.
        public bool Aceptar(int idViaje, int idTransportista, int? idVehiculo, TransaccionBE retencion, bool sinComision)
        {
            var parametros = new List<SqlParameter>
            {
                EjecutorDAL.Parametro("@Id", idViaje), EjecutorDAL.Parametro("@Transportista", idTransportista),
                EjecutorDAL.Parametro("@Vehiculo", idVehiculo), EjecutorDAL.Parametro("@Retiene", retencion != null),
                EjecutorDAL.Parametro("@SinComision", sinComision)
            };
            parametros.AddRange(TransaccionDAL.Parametros(retencion ?? new TransaccionBE { Tipo = string.Empty, Detalle = string.Empty }));
            return EjecutorDAL.EjecutarCambio("usp_Viaje_Aceptar", "Viajes,Transacciones", parametros.ToArray()) > 0;
        }

        // Promoción de los dadores nuevos: cuántas ofertas publicó (sin contar las dadas de baja).
        public int ContarOfertasDelDador(int idDador)
        {
            return (int)EjecutorDAL.LeerValor("SELECT COUNT(*) FROM Viajes WHERE IdDador = @Dador AND Estado <> N'Cancelado';",
                EjecutorDAL.Parametro("@Dador", idDador));
        }

        // CU-002-015: la flota vincula su conductor y su vehículo antes de salir.
        public bool AsignarInterno(int idViaje, int idFlota, int idConductor, int idVehiculo)
        {
            const string sql = @"UPDATE Viajes SET IdConductor = @Conductor, IdVehiculo = @Vehiculo
                WHERE IdViaje = @Id AND IdTransportista = @Flota AND Estado = N'Asignado';";
            return EjecutorDAL.EjecutarCambio(sql, "Viajes", EjecutorDAL.Parametro("@Id", idViaje),
                EjecutorDAL.Parametro("@Flota", idFlota), EjecutorDAL.Parametro("@Conductor", idConductor),
                EjecutorDAL.Parametro("@Vehiculo", idVehiculo)) == 1;
        }

        public void RegistrarHistorial(int idViaje, string estado, string detalle, decimal? latitud, decimal? longitud, string usuario)
        {
            const string sql = @"INSERT INTO HistorialViaje (IdViaje, Estado, Detalle, Latitud, Longitud, Usuario)
                VALUES (@Id, @Estado, @Detalle, @Latitud, @Longitud, @Usuario);";
            EjecutorDAL.EjecutarCambio(sql, null, EjecutorDAL.Parametro("@Id", idViaje), EjecutorDAL.Parametro("@Estado", estado),
                EjecutorDAL.Parametro("@Detalle", detalle), EjecutorDAL.Parametro("@Latitud", latitud),
                EjecutorDAL.Parametro("@Longitud", longitud), EjecutorDAL.Parametro("@Usuario", usuario));
        }

        public void RegistrarIncidencia(int idViaje, string tipo, string descripcion, string foto, string usuario)
        {
            const string sql = @"INSERT INTO Incidencias (IdViaje, Tipo, Descripcion, Foto, Usuario)
                VALUES (@Id, @Tipo, @Descripcion, @Foto, @Usuario);";
            EjecutorDAL.EjecutarCambio(sql, null, EjecutorDAL.Parametro("@Id", idViaje), EjecutorDAL.Parametro("@Tipo", tipo),
                EjecutorDAL.Parametro("@Descripcion", descripcion), EjecutorDAL.Parametro("@Foto", foto),
                EjecutorDAL.Parametro("@Usuario", usuario));
        }

        // Línea de tiempo del monitoreo: cambios de estado e incidencias, del más viejo al más nuevo.
        public List<EventoViajeBE> ObtenerLineaDeTiempo(int idViaje)
        {
            const string sql = @"SELECT Fecha, Estado, Detalle, Latitud, Longitud, Usuario, 0, NULL, 0
                    FROM HistorialViaje WHERE IdViaje = @Id
                UNION ALL
                SELECT Fecha, N'Incidencia: ' + Tipo, Descripcion, NULL, NULL, Usuario, 1, IdIncidencia,
                    CASE WHEN Foto IS NULL THEN 0 ELSE 1 END
                    FROM Incidencias WHERE IdViaje = @Id
                ORDER BY Fecha;";
            return EjecutorDAL.Leer(sql, lector => new EventoViajeBE
            {
                Fecha = lector.GetDateTime(0),
                Titulo = lector.GetString(1),
                Detalle = lector.GetString(2),
                Latitud = EjecutorDAL.Decimal(lector, 3),
                Longitud = EjecutorDAL.Decimal(lector, 4),
                Usuario = lector.GetString(5),
                EsIncidencia = lector.GetInt32(6) == 1,
                IdIncidencia = EjecutorDAL.Entero(lector, 7),
                TieneFoto = lector.GetInt32(8) == 1
            }, EjecutorDAL.Parametro("@Id", idViaje));
        }

        // Para Archivo.ashx: el viaje y la foto de una incidencia.
        public string ObtenerFotoIncidencia(int idIncidencia, out int idViaje)
        {
            var lista = EjecutorDAL.Leer("SELECT IdViaje, Foto FROM Incidencias WHERE IdIncidencia = @Id;",
                lector => new { IdViaje = lector.GetInt32(0), Foto = EjecutorDAL.Texto(lector, 1) },
                EjecutorDAL.Parametro("@Id", idIncidencia));
            idViaje = lista.Count == 0 ? 0 : lista[0].IdViaje;
            return lista.Count == 0 ? null : lista[0].Foto;
        }

        private static SqlParameter[] ParametrosOferta(ViajeBE viaje)
        {
            return new[]
            {
                EjecutorDAL.Parametro("@Dador", viaje.IdDador),
                EjecutorDAL.Parametro("@Origen", viaje.Origen),
                EjecutorDAL.Parametro("@Destino", viaje.Destino),
                EjecutorDAL.Parametro("@Mercaderia", viaje.Mercaderia),
                EjecutorDAL.Parametro("@Peso", viaje.PesoToneladas),
                EjecutorDAL.Parametro("@FechaCarga", viaje.FechaCarga.Date),
                EjecutorDAL.Parametro("@Tarifa", viaje.TarifaAcordada),
                EjecutorDAL.Parametro("@Comision", viaje.Comision),
                EjecutorDAL.Parametro("@MedioPago", viaje.MedioPago),
                EjecutorDAL.Parametro("@Tarjeta", viaje.IdTarjeta),
                EjecutorDAL.Parametro("@Descuento", viaje.DescuentoDador),
                EjecutorDAL.Parametro("@Remito", viaje.CargoRemito)
            };
        }

        private static ViajeBE LeerViaje(SqlDataReader lector)
        {
            return new ViajeBE
            {
                IdViaje = lector.GetInt32(0),
                FechaPedido = lector.GetDateTime(1),
                IdDador = EjecutorDAL.Entero(lector, 2),
                Dador = EjecutorDAL.Texto(lector, 3),
                Origen = lector.GetString(4),
                Destino = lector.GetString(5),
                Mercaderia = lector.GetString(6),
                PesoToneladas = lector.GetDecimal(7),
                FechaCarga = lector.GetDateTime(8),
                TarifaAcordada = lector.GetDecimal(9),
                Comision = lector.GetDecimal(10),
                Estado = lector.GetString(11),
                IdTransportista = EjecutorDAL.Entero(lector, 12),
                Transportista = EjecutorDAL.Texto(lector, 13),
                IdVehiculo = EjecutorDAL.Entero(lector, 14),
                Patente = EjecutorDAL.Texto(lector, 15),
                IdConductor = EjecutorDAL.Entero(lector, 16),
                Conductor = EjecutorDAL.Texto(lector, 17),
                TieneRemito = lector.GetInt32(18) == 1,
                Calificado = lector.GetInt32(19) == 1,
                MedioPago = lector.GetString(20),
                IdTarjeta = EjecutorDAL.Entero(lector, 21),
                DescuentoDador = lector.GetDecimal(22),
                CargoRemito = lector.GetDecimal(23),
                EstadoPago = lector.GetString(24),
                FechaRemito = lector.IsDBNull(25) ? (DateTime?)null : lector.GetDateTime(25)
            };
        }
    }
}
