using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-005-027 Gestionar Disputa Comercial: el dador reclama por un viaje pagado con tarjeta mientras la
    // plata está retenida (R13); el pago queda congelado y el administrador decide a quién va.
    public sealed class DisputaBLL
    {
        private readonly DisputaDAL disputas = new DisputaDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly TarjetaDAL tarjetas = new TarjetaDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();
        private readonly FacturacionBLL facturacion = new FacturacionBLL();

        // idDador null = todas (el administrador).
        public List<DisputaBE> Listar(int? idDador)
        {
            return disputas.Listar(idDador);
        }

        // Los viajes del dador que todavía se pueden reclamar: pagados con tarjeta y con la plata retenida.
        public List<ViajeBE> ListarReclamables(int idDador)
        {
            return viajes.ListarDelDador(idDador, string.Empty).FindAll(v => v.MedioPago != "Efectivo" && v.EstadoPago == "Retenido");
        }

        public string Abrir(int idViaje, int idDador, string usuario, string motivo)
        {
            motivo = (motivo ?? string.Empty).Trim();
            if (motivo.Length < 10 || motivo.Length > 500) return "Contá qué pasó (entre 10 y 500 caracteres).";
            // R13 (ERR016): el viaje es suyo, se pagó con tarjeta y la plata sigue retenida.
            if (!disputas.Abrir(idViaje, idDador, motivo))
                return "Ese viaje no tiene plata retenida: ya se liberó con el remito, se pagó en efectivo o ya tiene una disputa.";
            var viaje = viajes.Obtener(idViaje);
            if (viaje.IdTransportista.HasValue)
                notificaciones.Avisar(viaje.IdTransportista.Value, "El dador abrió una disputa por el viaje N° " + idViaje
                    + ": el pago queda congelado hasta que la resuelva el administrador.");
            bitacora.Registrar(idDador, usuario, "Apertura de una disputa por el viaje N° " + idViaje, "Advertencia");
            return null;
        }

        // El administrador devuelve la plata al dador o se la paga al transportista.
        public string Resolver(int idDisputa, bool devolverAlDador, string comentario, int idAdmin, string admin)
        {
            comentario = (comentario ?? string.Empty).Trim();
            if (comentario.Length < 5 || comentario.Length > 500) return "Escribí el motivo de la decisión (entre 5 y 500 caracteres).";
            var disputa = disputas.Obtener(idDisputa);
            if (disputa == null || disputa.Estado != "Abierta") return "La disputa ya fue resuelta.";
            var viaje = viajes.Obtener(disputa.IdViaje);

            TransaccionBE movimiento;
            TarjetaBE tarjeta = null;   // la tarjeta a la que se devuelve, si se le devuelve a una tarjeta
            if (devolverAlDador && viaje.MedioPago == "Saldo")
            {
                // Se pagó con saldo: vuelve al saldo de la billetera del dador.
                movimiento = new TransaccionBE
                {
                    IdUsuario = viaje.IdDador.Value,
                    Tipo = "Devolución",
                    Monto = viaje.PrecioDador,
                    AfectaSaldo = true,
                    IdViaje = viaje.IdViaje,
                    Detalle = "Devolución del viaje N° " + viaje.IdViaje + " al saldo de la billetera (disputa resuelta)"
                };
            }
            else if (devolverAlDador)
            {
                tarjeta = tarjetas.ObtenerPorId(viaje.IdTarjeta.Value);
                movimiento = new TransaccionBE
                {
                    IdUsuario = viaje.IdDador.Value,
                    Tipo = "Devolución",
                    Monto = viaje.PrecioDador,
                    AfectaSaldo = false,
                    IdViaje = viaje.IdViaje,
                    IdTarjeta = viaje.IdTarjeta,
                    Detalle = "Devolución del viaje N° " + viaje.IdViaje + " a la " + tarjeta.Descripcion + " (disputa resuelta)",
                    Comprobante = PasarelaPagoServicio.Devolver(tarjeta.Token, viaje.PrecioDador)
                };
            }
            else
            {
                movimiento = new TransaccionBE
                {
                    IdUsuario = viaje.IdTransportista.Value,
                    Tipo = "Cobro de viaje",
                    Monto = viaje.NetoTransportista,
                    AfectaSaldo = true,
                    IdViaje = viaje.IdViaje,
                    Detalle = "Cobro del viaje N° " + viaje.IdViaje + " (disputa resuelta a favor del transportista)"
                };
            }
            disputa.Estado = devolverAlDador ? "Devuelta al dador" : "Pagada al transportista";
            disputa.Resolucion = comentario;
            disputa.ResueltaPor = admin;
            // Si no se guardó (otro administrador la resolvió un instante antes o falló la base), se anula la devolución
            // a la tarjeta, así el dador no la recibe dos veces.
            bool resuelta;
            try
            {
                resuelta = disputas.Resolver(disputa, devolverAlDador ? "Devuelto" : "Liberado", movimiento);
            }
            catch (Exception)
            {
                BilleteraBLL.AnularDevolucion(tarjeta, viaje.PrecioDador);
                throw;
            }
            if (!resuelta)
            {
                BilleteraBLL.AnularDevolucion(tarjeta, viaje.PrecioDador);
                return "La disputa ya fue resuelta.";
            }

            var aviso = "Disputa del viaje N° " + viaje.IdViaje + " resuelta: " + (devolverAlDador
                ? "se le devuelve el pago al dador." : "se le paga al transportista.") + " Motivo: " + comentario;
            notificaciones.Avisar(viaje.IdDador.Value, aviso);
            if (viaje.IdTransportista.HasValue) notificaciones.Avisar(viaje.IdTransportista.Value, aviso);
            // Si el viaje ya se entregó mientras estaba en disputa, recién ahora LogTech factura la comisión y el remito.
            if (!devolverAlDador && viaje.Estado == "Entregado") facturacion.Emitir(new FacturacionViaje(viaje), viaje.IdDador.Value);
            bitacora.Registrar(idAdmin, admin, "Resolución de la disputa del viaje N° " + viaje.IdViaje + ": " + disputa.Estado, "Advertencia");
            return null;
        }
    }
}
