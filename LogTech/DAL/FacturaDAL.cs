using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;
using LogTech.Servicios;

namespace LogTech.DAL
{
    // Facturas electrónicas (CU-008-039). El CUIT del cliente se guarda cifrado con AES, como en Usuarios.
    public sealed class FacturaDAL
    {
        private const string Columnas = @"IdFactura, IdUsuario, Fecha, Tipo, PuntoVenta, Numero, Servicio, Detalle, Cantidad,
            PrecioUnitario, Neto, Iva, Total, Cliente, CuitCliente, CondicionIva, IdViaje, IdSuscripcion, Cae, VencimientoCae, Estado";

        // Guarda la factura como «Pendiente de CAE» y devuelve su número interno.
        public int Alta(FacturaBE factura)
        {
            return (int)EjecutorDAL.CambiarYLeer("usp_Factura_Alta",
                EjecutorDAL.Parametro("@IdUsuario", factura.IdUsuario), EjecutorDAL.Parametro("@Fecha", factura.Fecha),
                EjecutorDAL.Parametro("@Tipo", factura.Tipo), EjecutorDAL.Parametro("@PuntoVenta", factura.PuntoVenta),
                EjecutorDAL.Parametro("@Servicio", factura.Servicio), EjecutorDAL.Parametro("@Detalle", factura.Detalle),
                EjecutorDAL.Parametro("@Cantidad", factura.Cantidad), EjecutorDAL.Parametro("@PrecioUnitario", factura.PrecioUnitario),
                EjecutorDAL.Parametro("@Neto", factura.Neto), EjecutorDAL.Parametro("@Iva", factura.Iva),
                EjecutorDAL.Parametro("@Total", factura.Total), EjecutorDAL.Parametro("@Cliente", factura.Cliente),
                EjecutorDAL.Parametro("@Cuit", CifradoServicio.Cifrar(factura.CuitCliente)),
                EjecutorDAL.Parametro("@CondicionIva", factura.CondicionIva), EjecutorDAL.Parametro("@IdViaje", factura.IdViaje),
                EjecutorDAL.Parametro("@IdSuscripcion", factura.IdSuscripcion));
        }

        // Los números son correlativos por tipo de factura y punto de venta.
        public int ProximoNumero(string tipo, int puntoVenta)
        {
            return (int)EjecutorDAL.LeerValor("SELECT ISNULL(MAX(Numero), 0) + 1 FROM Facturas WHERE Tipo = @Tipo AND PuntoVenta = @PuntoVenta;",
                EjecutorDAL.Parametro("@Tipo", tipo), EjecutorDAL.Parametro("@PuntoVenta", puntoVenta));
        }

        // ARCA la autorizó: queda emitida con su número y su CAE. false si ya estaba emitida.
        public bool Autorizar(int idFactura, int numero, string cae, DateTime vencimientoCae)
        {
            return EjecutorDAL.EjecutarCambio(@"UPDATE Facturas SET Numero = @Numero, Cae = @Cae, VencimientoCae = @Vence, Estado = N'Emitida'
                WHERE IdFactura = @Id AND Estado = N'Pendiente de CAE';", null,
                EjecutorDAL.Parametro("@Numero", numero), EjecutorDAL.Parametro("@Cae", cae),
                EjecutorDAL.Parametro("@Vence", vencimientoCae.Date), EjecutorDAL.Parametro("@Id", idFactura)) == 1;
        }

        public FacturaBE Obtener(int idFactura)
        {
            var lista = EjecutorDAL.Leer("SELECT " + Columnas + " FROM Facturas WHERE IdFactura = @Id;", LeerFactura,
                EjecutorDAL.Parametro("@Id", idFactura));
            return lista.Count == 0 ? null : lista[0];
        }

        public List<FacturaBE> ListarDelUsuario(int idUsuario)
        {
            return EjecutorDAL.Leer("SELECT " + Columnas + " FROM Facturas WHERE IdUsuario = @Id ORDER BY Fecha DESC, IdFactura DESC;",
                LeerFactura, EjecutorDAL.Parametro("@Id", idUsuario));
        }

        // Las del período [desde, hasta), para el administrador y ReporteFacturacion.xml.
        public List<FacturaBE> Listar(DateTime desde, DateTime hasta)
        {
            return EjecutorDAL.Leer("SELECT " + Columnas + " FROM Facturas WHERE Fecha >= @Desde AND Fecha < @Hasta ORDER BY Fecha DESC, IdFactura DESC;",
                LeerFactura, EjecutorDAL.Parametro("@Desde", desde), EjecutorDAL.Parametro("@Hasta", hasta));
        }

        public List<FacturaBE> ListarPendientes()
        {
            return EjecutorDAL.Leer("SELECT " + Columnas + " FROM Facturas WHERE Estado = N'Pendiente de CAE' ORDER BY IdFactura;", LeerFactura);
        }

        private static FacturaBE LeerFactura(SqlDataReader lector)
        {
            return new FacturaBE
            {
                IdFactura = lector.GetInt32(0),
                IdUsuario = lector.GetInt32(1),
                Fecha = lector.GetDateTime(2),
                Tipo = lector.GetString(3),
                PuntoVenta = lector.GetInt32(4),
                Numero = EjecutorDAL.Entero(lector, 5),
                Servicio = lector.GetString(6),
                Detalle = lector.GetString(7),
                Cantidad = lector.GetInt32(8),
                PrecioUnitario = lector.GetDecimal(9),
                Neto = lector.GetDecimal(10),
                Iva = lector.GetDecimal(11),
                Total = lector.GetDecimal(12),
                Cliente = lector.GetString(13),
                CuitCliente = CifradoServicio.Descifrar(lector.GetString(14)),
                CondicionIva = lector.GetString(15),
                IdViaje = EjecutorDAL.Entero(lector, 16),
                IdSuscripcion = EjecutorDAL.Entero(lector, 17),
                Cae = EjecutorDAL.Texto(lector, 18),
                VencimientoCae = lector.IsDBNull(19) ? (DateTime?)null : lector.GetDateTime(19),
                Estado = lector.GetString(20)
            };
        }
    }
}
