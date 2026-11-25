using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-006-029 Alta Solicitud de Factoring y CU-006-030 Procesar Factoring. Sobre un viaje en efectivo entregado que el
    // dador todavía no pagó, el transportista cobra al momento lo que le corresponde menos el 4 %. La entidad que adelanta
    // (simulada) recupera la plata cuando el dador paga en el punto de pago.
    public sealed class FactoringBLL
    {
        public const decimal Tasa = 0.04m;
        private const int PuntajeMinimo = 60;
        // ponytail: fondo simulado de la entidad que adelanta (R15); con una entidad real se consultaría su saldo.
        private const decimal FondoEntidad = 50000000;

        private readonly FactoringDAL factoring = new FactoringDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        // WS_FactoringLogistico: cuánto se cobra al momento por un monto (sin evaluar a nadie).
        public CotizacionFactoringBE Cotizar(decimal monto)
        {
            var comision = Math.Round(monto * Tasa, 2);
            return new CotizacionFactoringBE { Monto = monto, Comision = comision, MontoAdelantado = monto - comision };
        }

        // Scoring de 0 a 100: reputación (promedio de estrellas) hasta 60, viajes entregados hasta 30 (1,5 por viaje,
        // hasta 20 viajes) y 10 si no perdió ninguna disputa.
        public PuntajeFactoringBE CalcularPuntaje(int idTransportista)
        {
            var promedio = factoring.PromedioCalificaciones(idTransportista);
            return new PuntajeFactoringBE
            {
                Reputacion = promedio.HasValue ? (int)Math.Round(promedio.Value / 5 * 60) : 0,
                Viajes = (int)Math.Round(Math.Min(factoring.ViajesEntregados(idTransportista), 20) * 1.5),
                SinDisputas = factoring.DisputasPerdidas(idTransportista) == 0 ? 10 : 0
            };
        }

        // Los viajes que se pueden adelantar: entregados, en efectivo y que el dador todavía no pagó.
        public List<ViajeBE> ListarAdelantables(int idTransportista)
        {
            return viajes.ListarDelTransportista(idTransportista, true).FindAll(v => v.MedioPago == "Efectivo" && v.EstadoPago == "A cobrar");
        }

        public List<SolicitudFactoringBE> ListarSolicitudes(int idTransportista)
        {
            return factoring.Listar(idTransportista);
        }

        // Devuelve null si se aprobó, o el motivo del rechazo.
        public string Solicitar(int idViaje, int idTransportista, string usuario)
        {
            var cuenta = usuarios.ObtenerPorId(idTransportista);
            if (cuenta == null || cuenta.Rol != "Transportista") return "El factoring es solo para transportistas.";
            var viaje = viajes.Obtener(idViaje);
            // R14 (ERR017): el viaje es suyo, se entregó, se paga en efectivo y el dador todavía no lo pagó.
            if (viaje == null || viaje.IdTransportista != idTransportista || viaje.Estado != "Entregado"
                || viaje.MedioPago != "Efectivo" || viaje.EstadoPago != "A cobrar")
                return "Ese viaje no se puede adelantar: tiene que estar entregado, ser en efectivo y el dador no tiene que haberlo pagado.";

            var puntaje = CalcularPuntaje(idTransportista);
            var cotizacion = Cotizar(viaje.NetoTransportista);
            var solicitud = new SolicitudFactoringBE
            {
                IdViaje = idViaje,
                IdTransportista = idTransportista,
                MontoViaje = cotizacion.Monto,
                Comision = cotizacion.Comision,
                MontoAdelantado = cotizacion.MontoAdelantado,
                Puntaje = puntaje.Total
            };

            string rechazo = null;
            if (puntaje.Total < PuntajeMinimo)
                rechazo = "Tu puntaje es " + puntaje.Total + " de 100 y hace falta " + PuntajeMinimo + ": sumá viajes y buenas calificaciones.";
            // R15 (ERR018): la entidad que adelanta tiene que tener plata.
            else if (solicitud.MontoAdelantado > FondoEntidad - factoring.Comprometido())
                rechazo = "La entidad que adelanta no tiene fondos en este momento. Probá más tarde.";
            if (rechazo != null)
            {
                solicitud.Estado = "Rechazada";
                solicitud.Motivo = rechazo;
                factoring.Rechazar(solicitud);
                bitacora.Registrar(idTransportista, usuario, "Factoring rechazado del viaje N° " + idViaje + " (puntaje " + puntaje.Total + ")", "Informativa");
                return rechazo;
            }

            solicitud.Estado = "Aprobada";
            var adelanto = new TransaccionBE
            {
                IdUsuario = idTransportista,
                Tipo = "Adelanto de factoring",
                Monto = solicitud.MontoAdelantado,
                AfectaSaldo = true,
                IdViaje = idViaje,
                Detalle = "Adelanto del viaje N° " + idViaje + " (" + solicitud.MontoViaje.ToString("C0") + " menos el 4 % del factoring)",
                Comprobante = PasarelaPagoServicio.AcreditarAdelanto(solicitud.MontoAdelantado)
            };
            // Si no se guardó (el dador pagó o se adelantó un instante antes, o falló la base), la entidad anula el adelanto.
            bool aprobado;
            try
            {
                aprobado = factoring.Aprobar(solicitud, adelanto);
            }
            catch (Exception)
            {
                PasarelaPagoServicio.AnularAdelanto(solicitud.MontoAdelantado);
                throw;
            }
            if (!aprobado)
            {
                PasarelaPagoServicio.AnularAdelanto(solicitud.MontoAdelantado);
                return "Ese viaje ya no está a cobrar: el dador lo pagó o ya se adelantó.";
            }
            bitacora.Registrar(idTransportista, usuario, "Factoring aprobado del viaje N° " + idViaje + " por " + solicitud.MontoAdelantado.ToString("C0"), "Informativa");
            return null;
        }
    }
}
