using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-008-040 a 043: suscripciones mensuales. La flota contrata la Flota SaaS y paga por cada vehículo que tiene
    // cargado; el transportista contrata la Premium (no paga la comisión del 7 % y recibe alertas de cargas de alta
    // rentabilidad). Se pagan con una tarjeta guardada o con el saldo de la billetera, se renuevan solas cada mes y cada
    // cobro genera su factura (CU-008-039) con la estrategia del servicio. El dador también puede contratar la Flota SaaS:
    // al pagarla, su cuenta pasa a ser de Administrador de Flota.
    //
    // Flota SaaS, por vehículo: al contratarla paga sus vehículos activos (como mínimo uno). Cada vehículo que agrega por
    // encima de los que ya pagó ese mes se cobra en el momento, el mes entero. Si da uno de baja, ese mes ya está pagado
    // y no se devuelve; la renovación cobra los vehículos activos que tenga ese día.
    public sealed class SuscripcionBLL
    {
        // Lista de precios v5 (6.2.5), con IVA incluido.
        public const decimal PrecioPorPatente = 52500;
        public const decimal PrecioPremium = 22500;

        private readonly SuscripcionDAL suscripciones = new SuscripcionDAL();
        private readonly TarjetaDAL tarjetas = new TarjetaDAL();
        private readonly TransaccionDAL transacciones = new TransaccionDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly VehiculoDAL vehiculos = new VehiculoDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();
        private readonly FacturacionBLL facturacion = new FacturacionBLL();

        // El transportista contrata la Premium; la flota y el dador, la Flota SaaS.
        public static string TipoDelRol(string rol)
        {
            return rol == "Transportista" ? "Premium transportista" : "Flota SaaS";
        }

        // Lo que se paga por mes: la Flota SaaS, por vehículo; la Premium, un precio fijo.
        public static decimal CalcularMonto(string tipo, int vehiculosPagos)
        {
            return tipo == "Flota SaaS" ? vehiculosPagos * PrecioPorPatente : PrecioPremium;
        }

        // CU-008-043: la vigente (activa o pendiente de baja), o null.
        public SuscripcionBE ObtenerVigente(int idUsuario, string rol)
        {
            return suscripciones.ObtenerVigente(idUsuario, TipoDelRol(rol));
        }

        public bool EsPremium(int idUsuario)
        {
            return suscripciones.ObtenerVigente(idUsuario, "Premium transportista") != null;
        }

        public List<int> ListarPremium()
        {
            return suscripciones.ListarPremium();
        }

        public int VehiculosActivos(int idUsuario)
        {
            return vehiculos.Listar(idUsuario, string.Empty, "Activo").Count;
        }

        // Cuántos vehículos se cobran al contratar o al renovar la Flota SaaS: los activos, como mínimo uno.
        public int VehiculosACobrar(int idUsuario)
        {
            return Math.Max(1, VehiculosActivos(idUsuario));
        }

        // CU-008-040. Cobra el primer mes, guarda la suscripción y emite la factura. null si salió bien, o el error.
        // idTarjeta 0: paga con el saldo de la billetera (la suscripción queda sin tarjeta y se renueva con el saldo).
        public string Contratar(int idUsuario, string usuario, string rol, int idTarjeta)
        {
            var tipo = TipoDelRol(rol);
            TarjetaBE tarjeta = null;
            if (idTarjeta != 0)
            {
                tarjeta = tarjetas.Obtener(idTarjeta, idUsuario);
                if (tarjeta == null) return "Elegí una de tus tarjetas guardadas (las agregás en la Billetera) o el saldo.";
            }
            if (suscripciones.ObtenerVigente(idUsuario, tipo) != null) return "Ya tenés la suscripción " + tipo + " vigente.";
            // El dador que pasa a flota deja de publicar cargas: no puede quedar ninguna sin terminar ni sin pagar.
            var pasaAFlota = rol == "Dador de carga";
            if (pasaAFlota && TieneViajesSinTerminar(idUsuario))
                return "Para pasar tu cuenta a flota no podés tener cargas publicadas, viajes en curso ni pagos pendientes.";

            var cuenta = usuarios.ObtenerPorId(idUsuario);
            var cantidad = tipo == "Flota SaaS" ? VehiculosACobrar(idUsuario) : 0;
            var suscripcion = new SuscripcionBE
            {
                IdUsuario = idUsuario,
                Tipo = tipo,
                CantidadPatentes = tipo == "Flota SaaS" ? cantidad : (int?)null,
                Monto = CalcularMonto(tipo, cantidad),
                IdTarjeta = tarjeta == null ? (int?)null : idTarjeta,
                Vence = DateTime.Now.AddMonths(1)
            };
            // ponytail: el saldo se controla antes de guardar; dos pagos al mismo tiempo podrían dejarlo en negativo.
            if (tarjeta == null && transacciones.Saldo(idUsuario) < suscripcion.Monto) return BilleteraBLL.FaltaSaldo(transacciones.Saldo(idUsuario));
            // R21: el primer mes se cobra a la tarjeta (o sale del saldo).
            var pago = BilleteraBLL.Pago(idUsuario, "Pago de suscripción", tarjeta, "Alta de la suscripción " + tipo, suscripcion.Monto);
            var suscriptor = string.IsNullOrEmpty(cuenta.RazonSocial) ? cuenta.Nombre + " " + cuenta.Apellido : cuenta.RazonSocial;
            // Si no se guardó (ya la tenía o falló la base), se devuelve lo cobrado a la tarjeta (con saldo, el
            // movimiento no llegó a guardarse).
            try
            {
                suscripcion.IdSuscripcion = suscripciones.Contratar(suscripcion, suscriptor, pago, pasaAFlota);
            }
            catch (Exception)
            {
                BilleteraBLL.Devolver(tarjeta, suscripcion.Monto);
                throw;
            }
            if (suscripcion.IdSuscripcion == 0)
            {
                BilleteraBLL.Devolver(tarjeta, suscripcion.Monto);
                return "Ya tenés la suscripción " + tipo + " vigente.";
            }
            bitacora.Registrar(idUsuario, usuario, "Alta de la suscripción " + tipo + Vehiculos(suscripcion) + " por "
                + suscripcion.Monto.ToString("C0") + " por mes", "Informativa");
            if (pasaAFlota)
            {
                bitacora.Registrar(idUsuario, usuario, "Cambio de rol de Dador de carga a Administrador de Flota", "Advertencia");
                notificaciones.Avisar(idUsuario, "Tu cuenta ahora es de flota: cargá tus vehículos y conductores y tomá cargas de la bolsa.");
            }
            facturacion.Emitir(Estrategia(suscripcion, cantidad, DateTime.Today, suscripcion.Vence), idUsuario);
            return null;
        }

        // Cargas del dador que todavía no terminaron: publicadas, en viaje, sin pagar en el punto de pago o en disputa.
        private bool TieneViajesSinTerminar(int idDador)
        {
            foreach (var viaje in viajes.ListarDelDador(idDador, string.Empty))
            {
                if (viaje.Estado != "Entregado" && viaje.Estado != "Cancelado") return true;
                if (viaje.EstadoPago == "A cobrar" || viaje.EstadoPago == "En disputa") return true;
            }
            return false;
        }

        // R39 (cambio de plan del seguro, CU-006-032): la diferencia entre el precio nuevo y el viejo por los días que
        // faltan del mes pagado.
        public static decimal Prorratear(decimal precioAntes, decimal precioDespues, DateTime vence)
        {
            var diasDelMes = (vence - vence.AddMonths(-1)).TotalDays;
            var diasQueFaltan = Math.Max(0, (vence - DateTime.Now).TotalDays);
            return Math.Round((precioDespues - precioAntes) * (decimal)(diasQueFaltan / diasDelMes), 2);
        }

        // CU-008-041: cambia con qué se paga (una tarjeta o el saldo). Lo que se paga por mes no cambia: en la Flota SaaS
        // lo deciden los vehículos que tenga cargados.
        public string Modificar(int idUsuario, string usuario, string rol, int idTarjeta)
        {
            var actual = ObtenerVigente(idUsuario, rol);
            if (actual == null) return "No tenés una suscripción vigente.";
            if (actual.CancelaAlVencer) return "La suscripción está dada de baja: termina el " + actual.Vence.ToString("dd/MM/yyyy") + ".";
            if (idTarjeta != 0 && tarjetas.Obtener(idTarjeta, idUsuario) == null)
                return "Elegí una de tus tarjetas guardadas (las agregás en la Billetera) o el saldo.";
            if (!suscripciones.CambiarMedioDePago(actual.IdSuscripcion, idTarjeta == 0 ? (int?)null : idTarjeta))
                return "La suscripción ya no está vigente.";
            notificaciones.Avisar(idUsuario, "Tu suscripción " + actual.Tipo + " se renueva el " + actual.Vence.ToString("dd/MM/yyyy")
                + (idTarjeta == 0 ? " con el saldo de la billetera." : " con la tarjeta que elegiste."));
            bitacora.Registrar(idUsuario, usuario, "Cambio del medio de pago de la suscripción " + actual.Tipo, "Informativa");
            return null;
        }

        // CU-008-042 (R40): deja de renovarse y sigue hasta el último día pagado. Devuelve null si salió bien.
        public string Cancelar(int idUsuario, string usuario, string rol, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo)) return "Elegí el motivo de la baja.";
            var actual = ObtenerVigente(idUsuario, rol);
            if (actual == null || !suscripciones.Cancelar(actual.IdSuscripcion)) return "No tenés una suscripción para dar de baja.";
            notificaciones.Avisar(idUsuario, "Diste de baja la suscripción " + actual.Tipo + ". Seguís usándola hasta el "
                + actual.Vence.ToString("dd/MM/yyyy") + " y no se te vuelve a cobrar.");
            bitacora.Registrar(idUsuario, usuario, "Baja de la suscripción " + actual.Tipo + " al vencer el "
                + actual.Vence.ToString("dd/MM/yyyy") + " (motivo: " + motivo + ")", "Advertencia");
            return null;
        }

        // Lo que se le avisa antes de confirmar un vehículo nuevo (el pop-up de «Guardar vehículo»).
        public string AvisoAltaVehiculo(int idUsuario, string rol)
        {
            if (rol != "Administrador de Flota")
                return "¿Guardar el vehículo? Su documentación queda en revisión y no vas a poder usarlo hasta que LogTech la apruebe.";
            var suscripcion = suscripciones.ObtenerVigente(idUsuario, "Flota SaaS");
            if (suscripcion == null) return "¿Guardar el vehículo?";
            var pagos = suscripcion.CantidadPatentes ?? 0;
            if (VehiculosActivos(idUsuario) < pagos)
                return "¿Guardar el vehículo? Entra en los " + pagos + " vehículos que ya pagaste este mes. Su documentación queda en revisión.";
            return "Al confirmar, se cobra " + PrecioPorPatente.ToString("C0") + " (un vehículo más) " + ConQuePaga(suscripcion)
                + " y tu Flota SaaS pasa a " + (pagos + 1) + " vehículos: " + CalcularMonto(suscripcion.Tipo, pagos + 1).ToString("C0")
                + " por mes. Su documentación queda en revisión. ¿Confirmás?";
        }

        // Antes de guardar un vehículo nuevo. El transportista maneja un solo camión. La flota tiene que tener la Flota
        // SaaS y, si ya tiene tantos vehículos como los que pagó este mes, se le cobra uno más en el momento: cobro es la
        // suscripción con el vehículo sumado, pago el movimiento y tarjeta con la que se cobró (null con el saldo), para
        // devolver el cobro si no se puede guardar. Devuelve null si puede cargarlo, o el texto del error.
        public string PrepararAltaVehiculo(int idUsuario, string patente, out SuscripcionBE cobro, out TransaccionBE pago, out TarjetaBE tarjeta)
        {
            cobro = null;
            pago = null;
            tarjeta = null;
            var cuenta = usuarios.ObtenerPorId(idUsuario);
            if (cuenta == null) return "La cuenta no existe.";
            if (cuenta.Rol == "Transportista")
                return VehiculosActivos(idUsuario) > 0 ? "Ya tenés un camión cargado. Para cambiarlo, dalo de baja primero." : null;
            if (cuenta.Rol != "Administrador de Flota") return null;

            var suscripcion = suscripciones.ObtenerVigente(idUsuario, "Flota SaaS");
            if (suscripcion == null) return "Para cargar vehículos contratá la Flota SaaS en la sección Suscripción.";
            if (suscripcion.CancelaAlVencer)
                return "Diste de baja la Flota SaaS: termina el " + suscripcion.Vence.ToString("dd/MM/yyyy") + " y ya no se pueden sumar vehículos.";
            var pagos = suscripcion.CantidadPatentes ?? 0;
            if (VehiculosActivos(idUsuario) < pagos) return null;   // entra en lo que ya pagó este mes

            if (suscripcion.IdTarjeta.HasValue)
            {
                tarjeta = tarjetas.Obtener(suscripcion.IdTarjeta.Value, idUsuario);
                if (tarjeta == null) return "La tarjeta de tu Flota SaaS ya no está: elegí otra o el saldo en la sección Suscripción.";
            }
            // R12. ponytail: el saldo se controla antes de guardar; dos pagos al mismo tiempo podrían dejarlo en negativo.
            else if (transacciones.Saldo(idUsuario) < PrecioPorPatente) return BilleteraBLL.FaltaSaldo(transacciones.Saldo(idUsuario));

            cobro = suscripcion;
            cobro.CantidadPatentes = pagos + 1;
            cobro.Monto = CalcularMonto(cobro.Tipo, pagos + 1);
            // R21: un mes de un vehículo más, a la tarjeta de la suscripción o del saldo.
            pago = BilleteraBLL.Pago(idUsuario, "Pago de suscripción", tarjeta, "Flota SaaS: un vehículo más (" + patente + ")", PrecioPorPatente);
            return null;
        }

        // Después de guardar el vehículo cobrado: la factura, el aviso y la bitácora.
        public void TerminarAltaVehiculo(SuscripcionBE cobro, string patente, string usuario)
        {
            facturacion.Emitir(Estrategia(cobro, 1, DateTime.Today, cobro.Vence), cobro.IdUsuario);
            notificaciones.Avisar(cobro.IdUsuario, "Se cobraron " + PrecioPorPatente.ToString("C0") + " por el vehículo " + patente
                + ". Tu Flota SaaS pasa a " + cobro.CantidadPatentes + " vehículos: " + cobro.Monto.ToString("C0") + " por mes.");
            bitacora.Registrar(cobro.IdUsuario, usuario, "Flota SaaS: cobro de un vehículo más (" + patente + "), pasa a "
                + cobro.CantidadPatentes + " vehículos", "Informativa");
        }

        // Rutina diaria: las suscripciones cuyo mes terminó se renuevan (se cobra otro mes y se factura) o, si se
        // dieron de baja, la tarjeta ya no está o el saldo no alcanza, terminan. La Flota SaaS cobra los vehículos
        // activos que tenga ese día.
        public void ProcesarVencimientos()
        {
            foreach (var suscripcion in suscripciones.ListarVencidas())
            {
                var esFlota = suscripcion.Tipo == "Flota SaaS";
                var cantidad = esFlota ? VehiculosACobrar(suscripcion.IdUsuario) : 0;
                if (esFlota)
                {
                    suscripcion.CantidadPatentes = cantidad;
                    suscripcion.Monto = CalcularMonto(suscripcion.Tipo, cantidad);
                }
                var conSaldo = !suscripcion.IdTarjeta.HasValue;
                var tarjeta = conSaldo ? null : tarjetas.Obtener(suscripcion.IdTarjeta.Value, suscripcion.IdUsuario);
                var motivoFin = suscripcion.CancelaAlVencer ? string.Empty
                    : !conSaldo && tarjeta == null ? " (sin tarjeta para renovarla)"
                    : conSaldo && transacciones.Saldo(suscripcion.IdUsuario) < suscripcion.Monto ? " (sin saldo para renovarla)"
                    : null;
                if (motivoFin != null)
                {
                    suscripciones.Terminar(suscripcion.IdSuscripcion);
                    bitacora.Registrar(suscripcion.IdUsuario, "sistema", "Fin de la suscripción " + suscripcion.Tipo + motivoFin, "Informativa");
                    continue;
                }
                var pago = BilleteraBLL.Pago(suscripcion.IdUsuario, "Pago de suscripción", tarjeta,
                    "Renovación de la suscripción " + suscripcion.Tipo + Vehiculos(suscripcion), suscripcion.Monto);
                var desde = suscripcion.Vence;
                if (suscripciones.Renovar(suscripcion, desde.AddMonths(1), pago))
                {
                    bitacora.Registrar(suscripcion.IdUsuario, "sistema", "Renovación de la suscripción " + suscripcion.Tipo + Vehiculos(suscripcion), "Informativa");
                    facturacion.Emitir(Estrategia(suscripcion, cantidad, desde, desde.AddMonths(1)), suscripcion.IdUsuario);
                }
                else BilleteraBLL.Devolver(tarjeta, suscripcion.Monto);
            }
        }

        // Patrón Strategy: cada suscripción se factura con la estrategia de su servicio.
        private static IEstrategiaFacturacion Estrategia(SuscripcionBE suscripcion, int vehiculosACobrar, DateTime desde, DateTime hasta)
        {
            if (suscripcion.Tipo == "Flota SaaS") return new FacturacionFlota(suscripcion, vehiculosACobrar, desde, hasta);
            return new FacturacionPremium(suscripcion, desde);
        }

        private static string Vehiculos(SuscripcionBE suscripcion)
        {
            return suscripcion.CantidadPatentes.HasValue ? " de " + suscripcion.CantidadPatentes + " vehículo(s)" : string.Empty;
        }

        private static string ConQuePaga(SuscripcionBE suscripcion)
        {
            return suscripcion.IdTarjeta.HasValue ? "a tu " + (suscripcion.Tarjeta ?? "tarjeta") : "de tu saldo";
        }
    }
}
