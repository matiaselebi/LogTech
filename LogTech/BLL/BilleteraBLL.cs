using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-005-025 Gestionar Fondos Escrow y CU-005-026 Consultar Estado de Pago: tarjetas, billetera
    // (saldo, carga y retiro), retención del pago con tarjeta al aceptar el viaje y liquidación con el remito.
    public sealed class BilleteraBLL
    {
        public const decimal MontoMinimo = 1000;
        public const decimal MontoMaximo = 5000000;

        private readonly TarjetaDAL tarjetas = new TarjetaDAL();
        private readonly TransaccionDAL transacciones = new TransaccionDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly CuentaDepositoDAL cuentas = new CuentaDepositoDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        public BilleteraBE ObtenerResumen(int idUsuario)
        {
            return new BilleteraBE
            {
                Saldo = transacciones.Saldo(idUsuario),
                EnEscrow = transacciones.EnEscrow(idUsuario)
            };
        }

        // Administrador: toda la plata retenida en escrow.
        public TotalEstadisticaBE ObtenerEscrowTotal()
        {
            return transacciones.TotalEnEscrow();
        }

        public List<TransaccionBE> ListarMovimientos(int idUsuario)
        {
            return transacciones.Listar(idUsuario);
        }

        // ---- tarjetas ----

        public List<TarjetaBE> ListarTarjetas(int idUsuario)
        {
            return tarjetas.Listar(idUsuario);
        }

        public TarjetaBE ObtenerTarjeta(int idTarjeta, int idUsuario)
        {
            return tarjetas.Obtener(idTarjeta, idUsuario);
        }

        // Devuelve null si salió bien, o el texto del error. El número y el código de seguridad no se guardan:
        // la pasarela valida la tarjeta (R21) y devuelve un token.
        public string AgregarTarjeta(int idUsuario, string usuario, string titular, string numero, string vencimiento, string codigo)
        {
            titular = (titular ?? string.Empty).Trim();
            numero = Regex.Replace(numero ?? string.Empty, @"[\s\-]", string.Empty);
            vencimiento = (vencimiento ?? string.Empty).Trim();
            codigo = (codigo ?? string.Empty).Trim();
            var marca = PasarelaPagoServicio.Marca(numero);

            // ERR001: formato.
            if (!Validador.EsNombre(titular, 25)) return "Escribí el nombre del titular como figura en la tarjeta (hasta 25 letras).";
            if (marca == null || !PasarelaPagoServicio.LuhnValido(numero))
                return "Número de tarjeta inválido. Aceptamos Visa y Mastercard.";
            if (!VencimientoValido(vencimiento)) return "Elegí el vencimiento: este mes o uno posterior.";
            if (!Regex.IsMatch(codigo, @"^\d{3}$")) return "El código tiene 3 números.";

            // R21 (ERR023): la pasarela la valida con el banco.
            string error;
            var token = PasarelaPagoServicio.Tokenizar(numero, out error);
            if (token == null) return error;

            var tarjeta = new TarjetaBE
            {
                IdUsuario = idUsuario,
                Titular = titular,
                Marca = marca,
                Ultimos4 = numero.Substring(numero.Length - 4),
                Vencimiento = vencimiento,
                Token = token
            };
            tarjetas.Alta(tarjeta);
            bitacora.Registrar(idUsuario, usuario, "Alta de la tarjeta " + tarjeta.Descripcion, "Informativa");
            return null;
        }

        public string QuitarTarjeta(int idTarjeta, int idUsuario, string usuario)
        {
            var tarjeta = tarjetas.Obtener(idTarjeta, idUsuario);
            if (tarjeta == null) return "La tarjeta no existe.";
            if (tarjetas.EnUso(idTarjeta))
                return "Esa tarjeta paga una oferta publicada o tu seguro. Cambiá el medio de pago de la oferta o cancelá el seguro antes de quitarla.";
            tarjetas.Quitar(idTarjeta, idUsuario);
            bitacora.Registrar(idUsuario, usuario, "Baja de la tarjeta " + tarjeta.Descripcion, "Informativa");
            return null;
        }

        // MM/AA, mes de 01 a 12, y la tarjeta vale hasta el último día de ese mes.
        private static bool VencimientoValido(string vencimiento)
        {
            if (!Regex.IsMatch(vencimiento, @"^(0[1-9]|1[0-2])/\d{2}$")) return false;
            var mes = int.Parse(vencimiento.Substring(0, 2));
            var anio = 2000 + int.Parse(vencimiento.Substring(3, 2));
            return new DateTime(anio, mes, 1).AddMonths(1) > DateTime.Today;
        }

        // ---- saldo de la billetera (todos los clientes) ----

        public decimal Saldo(int idUsuario)
        {
            return transacciones.Saldo(idUsuario);
        }

        // Pago de un servicio (suscripción, seguro): con una tarjeta (cobra la pasarela, R21) o, si tarjeta es null,
        // con el saldo de la billetera (el movimiento resta del saldo). Antes hay que controlar que el saldo alcance.
        internal static TransaccionBE Pago(int idUsuario, string tipo, TarjetaBE tarjeta, string detalle, decimal monto)
        {
            return new TransaccionBE
            {
                IdUsuario = idUsuario,
                Tipo = tipo,
                Monto = -monto,
                AfectaSaldo = tarjeta == null,
                IdTarjeta = tarjeta == null ? (int?)null : tarjeta.IdTarjeta,
                Detalle = detalle + (tarjeta == null ? " con el saldo de la billetera" : " con " + tarjeta.Descripcion),
                Comprobante = tarjeta == null ? null : PasarelaPagoServicio.Cobrar(tarjeta.Token, monto)
            };
        }

        // Si la operación no se pudo guardar, se devuelve lo cobrado a la tarjeta. Con saldo no hace falta: el
        // movimiento no llegó a guardarse.
        internal static void Devolver(TarjetaBE tarjeta, decimal monto)
        {
            if (tarjeta != null) PasarelaPagoServicio.Devolver(tarjeta.Token, monto);
        }

        // Al revés: si una devolución a la tarjeta no se pudo guardar, se anula (se vuelve a cobrar lo devuelto), así
        // no se devuelve dos veces. Con saldo no hace falta: el movimiento no llegó a guardarse.
        internal static void AnularDevolucion(TarjetaBE tarjeta, decimal monto)
        {
            if (tarjeta != null) PasarelaPagoServicio.Cobrar(tarjeta.Token, monto);
        }

        internal static string FaltaSaldo(decimal saldo)
        {
            return "No te alcanza el saldo de la billetera (tenés " + saldo.ToString("C0") + "). Cargá saldo o pagá con tarjeta.";
        }

        public string CargarSaldo(int idUsuario, string usuario, int idTarjeta, decimal monto)
        {
            var tarjeta = tarjetas.Obtener(idTarjeta, idUsuario);
            if (tarjeta == null) return "Elegí una de tus tarjetas.";
            if (monto < MontoMinimo || monto > MontoMaximo)
                return "El monto tiene que ser de " + MontoMinimo.ToString("C0") + " a " + MontoMaximo.ToString("C0") + ".";
            monto = Math.Round(monto, 2);
            var carga = new TransaccionBE
            {
                IdUsuario = idUsuario,
                Tipo = "Carga de saldo",
                Monto = monto,
                AfectaSaldo = true,
                IdTarjeta = idTarjeta,
                Detalle = "Carga de saldo con " + tarjeta.Descripcion,
                Comprobante = PasarelaPagoServicio.Cobrar(tarjeta.Token, monto)
            };
            // Si no se guardó (falló la base), se devuelve lo cobrado a la tarjeta.
            try
            {
                transacciones.Registrar(carga);
            }
            catch (Exception)
            {
                Devolver(tarjeta, monto);
                throw;
            }
            bitacora.Registrar(idUsuario, usuario, "Carga de saldo de " + monto.ToString("C0"), "Informativa");
            return null;
        }

        // R12 (ERR015): solo lo que hay disponible.
        public string Retirar(int idUsuario, string usuario, decimal monto, string cbuOAlias)
        {
            cbuOAlias = (cbuOAlias ?? string.Empty).Trim();
            if (monto < MontoMinimo || monto > MontoMaximo)
                return "El monto tiene que ser de " + MontoMinimo.ToString("C0") + " a " + MontoMaximo.ToString("C0") + ".";
            if (!Validador.EsCbuOAlias(cbuOAlias)) return "Ingresá un CBU de 22 números o un alias de 6 a 20 letras, números, puntos o guiones.";
            monto = Math.Round(monto, 2);
            var retiro = new TransaccionBE
            {
                IdUsuario = idUsuario,
                Tipo = "Retiro",
                Monto = -monto,
                AfectaSaldo = true,
                // El CBU es un dato sensible: en el movimiento queda solo el final.
                Detalle = Regex.IsMatch(cbuOAlias, @"^\d{22}$") ? "Retiro al CBU terminado en " + cbuOAlias.Substring(18) : "Retiro al alias " + cbuOAlias
            };
            if (!transacciones.RetirarSiAlcanza(retiro))
                return "No tenés saldo suficiente: podés retirar hasta " + Math.Max(0, transacciones.Saldo(idUsuario)).ToString("C0") + ".";
            // ponytail: la transferencia simulada no falla; con una pasarela real, transferir antes y guardar después.
            PasarelaPagoServicio.Transferir(cbuOAlias, monto);
            bitacora.Registrar(idUsuario, usuario, "Retiro de " + monto.ToString("C0") + " de la billetera", "Advertencia");
            return null;
        }

        // ---- CBU para el depósito: se guarda una vez y el retiro lo trae cargado ----

        public string ObtenerCbuDeposito(int idUsuario)
        {
            return cuentas.Obtener(idUsuario);
        }

        public string GuardarCbuDeposito(int idUsuario, string usuario, string cbuOAlias)
        {
            cbuOAlias = (cbuOAlias ?? string.Empty).Trim();
            if (!Validador.EsCbuOAlias(cbuOAlias)) return "Ingresá un CBU de 22 números o un alias de 6 a 20 letras, números, puntos o guiones.";
            cuentas.Guardar(idUsuario, cbuOAlias);
            bitacora.Registrar(idUsuario, usuario, "CBU para el depósito guardado", "Advertencia");
            return null;
        }

        public string EliminarCbuDeposito(int idUsuario, string usuario)
        {
            if (!cuentas.Eliminar(idUsuario)) return "No tenés un CBU guardado.";
            bitacora.Registrar(idUsuario, usuario, "CBU para el depósito eliminado", "Advertencia");
            return null;
        }

        // ---- escrow ----

        // CU-005-025: al aceptar un viaje pagado con tarjeta o con saldo se le cobra al dador y la plata queda retenida.
        // Devuelve el movimiento para guardarlo junto con la aceptación, o null si el viaje es en efectivo. tarjeta: la
        // tarjeta cobrada (null con saldo o en efectivo), para devolverle lo cobrado si la aceptación no se guarda.
        public TransaccionBE CobrarRetencion(ViajeBE viaje, out TarjetaBE tarjeta)
        {
            tarjeta = null;
            if (!viaje.IdDador.HasValue) return null;
            if (viaje.MedioPago == "Saldo")
                return new TransaccionBE
                {
                    IdUsuario = viaje.IdDador.Value,
                    Tipo = "Pago retenido",
                    Monto = -viaje.PrecioDador,
                    AfectaSaldo = true,
                    IdViaje = viaje.IdViaje,
                    Detalle = "Pago del viaje N° " + viaje.IdViaje + " con el saldo de la billetera, retenido hasta el remito"
                };
            if (viaje.MedioPago != "Tarjeta" || !viaje.IdTarjeta.HasValue) return null;
            tarjeta = tarjetas.ObtenerPorId(viaje.IdTarjeta.Value);
            return new TransaccionBE
            {
                IdUsuario = viaje.IdDador.Value,
                Tipo = "Pago retenido",
                Monto = -viaje.PrecioDador,
                AfectaSaldo = false,
                IdViaje = viaje.IdViaje,
                IdTarjeta = viaje.IdTarjeta,
                Detalle = "Pago del viaje N° " + viaje.IdViaje + " con " + tarjeta.Descripcion + ", retenido hasta el remito",
                Comprobante = PasarelaPagoServicio.Cobrar(tarjeta.Token, viaje.PrecioDador)
            };
        }

        // Con el remito (LiberarFondosObservador). Tarjeta o saldo: el transportista cobra en su billetera lo que le
        // corresponde. Efectivo: queda «A cobrar»; el dador tiene 30 días para pagarlo en un punto de pago y el
        // transportista lo puede adelantar con factoring. Si el pago está en disputa no se toca (CU-005-027).
        public void Liquidar(ViajeBE viaje)
        {
            if (!viaje.IdTransportista.HasValue) return;
            if (viaje.MedioPago != "Efectivo")
            {
                transacciones.RegistrarConViaje(viaje.IdViaje, "Retenido", "Liberado", new TransaccionBE
                {
                    IdUsuario = viaje.IdTransportista.Value,
                    Tipo = "Cobro de viaje",
                    Monto = viaje.NetoTransportista,
                    AfectaSaldo = true,
                    IdViaje = viaje.IdViaje,
                    Detalle = "Cobro del viaje N° " + viaje.IdViaje + " (tarifa menos comisión y remito)"
                });
                return;
            }
            viajes.CambiarEstadoPago(viaje.IdViaje, "Pendiente", "A cobrar");
        }

        // ---- efectivo en punto de pago ----

        public List<ViajeBE> ListarAPagar(int idDador)
        {
            return viajes.ListarAPagar(idDador);
        }

        // El dador paga un viaje en efectivo en Pago Fácil o Rapipago (simulado con un botón) o con el saldo de su
        // billetera. El transportista cobra en su billetera, salvo que lo haya adelantado con factoring: entonces ya
        // cobró y la plata es de la entidad.
        public string PagarEnPuntoDePago(int idViaje, int idDador, string usuario, bool conSaldo)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdDador != idDador || viaje.MedioPago != "Efectivo"
                || (viaje.EstadoPago != "A cobrar" && viaje.EstadoPago != "Adelantado"))
                return "Ese viaje no tiene un pago en efectivo pendiente.";
            // ponytail: el saldo se controla antes de guardar; dos pagos al mismo tiempo podrían dejarlo en negativo.
            if (conSaldo && transacciones.Saldo(idDador) < viaje.PrecioDador) return FaltaSaldo(transacciones.Saldo(idDador));
            var pago = new TransaccionBE
            {
                IdUsuario = idDador,
                Tipo = conSaldo ? "Pago con saldo" : "Pago en punto de pago",
                Monto = -viaje.PrecioDador,
                AfectaSaldo = conSaldo,
                IdViaje = idViaje,
                Detalle = conSaldo ? "Pago del viaje N° " + idViaje + " con el saldo de la billetera"
                    : "Pago en efectivo del viaje N° " + idViaje + " en Pago Fácil (código " + viaje.CodigoPago + ")",
                Comprobante = conSaldo ? null : PasarelaPagoServicio.CobrarEnPuntoDePago(viaje.CodigoPago, viaje.PrecioDador)
            };
            var cobro = viaje.EstadoPago == "Adelantado" ? null : new TransaccionBE
            {
                IdUsuario = viaje.IdTransportista.Value,
                Tipo = "Cobro de viaje",
                Monto = viaje.NetoTransportista,
                AfectaSaldo = true,
                IdViaje = idViaje,
                Detalle = "Cobro del viaje N° " + idViaje + (conSaldo ? ", pagado por el dador con el saldo de su billetera"
                    : " en efectivo, pagado por el dador en un punto de pago")
            };
            // Si no se guardó (otro pago llegó un instante antes o falló la base), el punto de pago le devuelve la plata al
            // dador (con saldo no hace falta: el movimiento no llegó a guardarse).
            bool pagado;
            try
            {
                pagado = transacciones.PagarEnPuntoDePago(idViaje, viaje.EstadoPago, pago, cobro);
            }
            catch (Exception)
            {
                if (!conSaldo) PasarelaPagoServicio.DevolverEnPuntoDePago(viaje.CodigoPago, viaje.PrecioDador);
                throw;
            }
            if (!pagado)
            {
                if (!conSaldo) PasarelaPagoServicio.DevolverEnPuntoDePago(viaje.CodigoPago, viaje.PrecioDador);
                return "Ese viaje ya se pagó.";
            }
            bitacora.Registrar(idDador, usuario, (conSaldo ? "Pago con saldo" : "Pago en punto de pago") + " del viaje N° " + idViaje
                + " por " + viaje.PrecioDador.ToString("C0"), "Informativa");
            return null;
        }
    }
}
