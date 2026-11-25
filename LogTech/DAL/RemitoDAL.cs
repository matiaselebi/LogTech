using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;
using LogTech.Servicios;

namespace LogTech.DAL
{
    public sealed class RemitoDAL
    {
        private const string Consulta = @"SELECT r.IdRemito, r.IdViaje, r.Receptor, r.DniReceptor, r.Foto, r.FechaEmision, r.Token,
                v.Origen, v.Destino, v.Mercaderia, ISNULL(t.RazonSocial, t.Nombre + ' ' + t.Apellido),
                ISNULL(d.RazonSocial, d.Nombre + ' ' + d.Apellido)
            FROM Remitos r
            JOIN Viajes v ON v.IdViaje = r.IdViaje
            LEFT JOIN Usuarios t ON t.IdUsuario = v.IdTransportista
            LEFT JOIN Usuarios d ON d.IdUsuario = v.IdDador ";

        // R10: el remito solo se guarda si el viaje está "En destino"; en la misma transacción
        // el viaje pasa a "Entregado". Devuelve false si el viaje ya no estaba en destino.
        public bool Alta(RemitoBE remito)
        {
            return EjecutorDAL.EjecutarCambio("usp_Remito_Alta", "Remitos", EjecutorDAL.Parametro("@Viaje", remito.IdViaje),
                EjecutorDAL.Parametro("@Receptor", remito.Receptor), EjecutorDAL.Parametro("@Dni", CifradoServicio.Cifrar(remito.DniReceptor)),
                EjecutorDAL.Parametro("@Foto", remito.Foto), EjecutorDAL.Parametro("@Fecha", remito.FechaEmision),
                EjecutorDAL.Parametro("@Token", remito.Token)) > 0;
        }

        public RemitoBE ObtenerDelViaje(int idViaje)
        {
            var lista = EjecutorDAL.Leer(Consulta + "WHERE r.IdViaje = @Id;", LeerRemito, EjecutorDAL.Parametro("@Id", idViaje));
            return lista.Count == 0 ? null : lista[0];
        }

        // Remitos de los viajes donde participó el usuario (como dador o como transportista).
        // texto: parte del origen, destino, receptor o número de viaje.
        public List<RemitoBE> ListarDelUsuario(int idUsuario, string texto)
        {
            return EjecutorDAL.Leer(Consulta + @"WHERE (v.IdDador = @Id OR v.IdTransportista = @Id)
                AND (@Texto = N'' OR v.Origen LIKE @Like OR v.Destino LIKE @Like OR r.Receptor LIKE @Like
                     OR CAST(v.IdViaje AS NVARCHAR(10)) = @Texto)
                ORDER BY r.FechaEmision DESC;", LeerRemito,
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Texto", texto ?? string.Empty),
                EjecutorDAL.Parametro("@Like", "%" + texto + "%"));
        }

        private static RemitoBE LeerRemito(SqlDataReader lector)
        {
            return new RemitoBE
            {
                IdRemito = lector.GetInt32(0),
                IdViaje = lector.GetInt32(1),
                Receptor = lector.GetString(2),
                DniReceptor = CifradoServicio.Descifrar(lector.GetString(3)),
                Foto = lector.GetString(4),
                FechaEmision = lector.GetDateTime(5),
                Token = lector.GetString(6),
                Origen = lector.GetString(7),
                Destino = lector.GetString(8),
                Mercaderia = lector.GetString(9),
                Transportista = EjecutorDAL.Texto(lector, 10),
                Dador = EjecutorDAL.Texto(lector, 11)
            };
        }
    }
}
