using System;
using System.Configuration;

namespace LogTech.Servicios
{
    // ARCA SIMULADO (decisión de la entrega final): autoriza las facturas electrónicas y devuelve el CAE (Código de
    // Autorización Electrónico) y su vencimiento, como el servicio web de factura electrónica real (WSFE). Para usar
    // el real se reemplaza solo esta clase: LogTech se autentica con su certificado digital (WSAA) y pide el CAE.
    public static class ArcaServicio
    {
        private const int DiasVencimientoCae = 10;
        private static readonly Random azar = new Random();

        // Devuelve el CAE (14 números) o null si ARCA no responde (ERR022). Con ArcaDisponible = false en Web.config
        // se simula que ARCA está caído.
        public static string SolicitarCae(string tipo, int puntoVenta, int numero, decimal total, out DateTime vencimiento)
        {
            vencimiento = DateTime.Today.AddDays(DiasVencimientoCae);
            if (ConfigurationManager.AppSettings["ArcaDisponible"] == "false") return null;
            lock (azar)
            {
                return "7" + azar.Next(10000000, 99999999) + azar.Next(10000, 99999);
            }
        }
    }
}
