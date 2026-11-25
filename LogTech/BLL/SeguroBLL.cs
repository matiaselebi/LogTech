using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // CU-006-031 Alta Contratación de Seguro, CU-006-033 Baja (cancelar al vencer) y CU-006-034 Consultar. El transportista,
    // la flota o el dador contrata por mes un plan de una aseguradora (simulada) y lo paga con una de sus tarjetas; se renueva
    // solo cada mes hasta que lo cancela. LogTech cobra el 5 % de la prima (lo ve el administrador en el tablero).
    public sealed class SeguroBLL
    {
        private readonly SeguroDAL seguros = new SeguroDAL();
        private readonly TarjetaDAL tarjetas = new TarjetaDAL();
        private readonly TransaccionDAL transacciones = new TransaccionDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        public List<string> ListarAseguradoras()
        {
            return seguros.ListarAseguradoras();
        }

        public List<PlanSeguroBE> ListarPlanes(string aseguradora)
        {
            return seguros.ListarPlanes(aseguradora);
        }

        public PlanSeguroBE ObtenerPlan(int idPlan)
        {
            return seguros.ObtenerPlan(idPlan);
        }

        public SeguroBE ObtenerActivo(int idUsuario)
        {
            return seguros.ObtenerActivo(idUsuario);
        }

        public List<SeguroBE> ListarHistorial(int idUsuario)
        {
            return seguros.Listar(idUsuario);
        }

        // Devuelve null si salió bien, o el texto del error. idTarjeta 0: paga con el saldo de la billetera.
        public string Contratar(int idUsuario, string usuario, int idPlan, int idTarjeta)
        {
            var plan = seguros.ObtenerPlan(idPlan);
            if (plan == null) return "Elegí uno de los planes.";
            TarjetaBE tarjeta = null;
            if (idTarjeta != 0)
            {
                tarjeta = tarjetas.Obtener(idTarjeta, idUsuario);
                if (tarjeta == null) return "Elegí una de tus tarjetas o el saldo para pagar el seguro.";
            }
            // ponytail: el saldo se controla antes de guardar; dos pagos al mismo tiempo podrían dejarlo en negativo.
            else if (transacciones.Saldo(idUsuario) < plan.Precio) return BilleteraBLL.FaltaSaldo(transacciones.Saldo(idUsuario));
            // R16: un solo seguro activo.
            var activo = seguros.ObtenerActivo(idUsuario);
            if (activo != null)
                return "Ya tenés un seguro activo hasta el " + activo.Vence.ToString("dd/MM/yyyy") + ". Podés contratar otro cuando termine.";

            var cuenta = usuarios.ObtenerPorId(idUsuario);
            var suscriptor = string.IsNullOrEmpty(cuenta.RazonSocial) ? cuenta.Nombre + " " + cuenta.Apellido : cuenta.RazonSocial;
            var seguro = new SeguroBE
            {
                IdUsuario = idUsuario,
                Plan = plan,
                Monto = plan.Precio,
                IdTarjeta = tarjeta == null ? (int?)null : idTarjeta,
                Vence = DateTime.Now.AddMonths(1),
                NumeroPoliza = AseguradoraServicio.EmitirPoliza(plan.Aseguradora, plan.Nombre)
            };
            if (seguro.NumeroPoliza == null) return "La aseguradora no responde en este momento. Intentá más tarde.";   // ERR042
            var pago = BilleteraBLL.Pago(idUsuario, "Pago de seguro", tarjeta, "Seguro " + plan.Aseguradora + " " + plan.Nombre
                + " (póliza " + seguro.NumeroPoliza + ")", plan.Precio);
            // Si no se guardó (ya tenía uno o falló la base), se devuelve lo cobrado a la tarjeta (con saldo, el
            // movimiento no llegó a guardarse).
            bool contratado;
            try
            {
                contratado = seguros.Contratar(seguro, suscriptor, pago);
            }
            catch (Exception)
            {
                BilleteraBLL.Devolver(tarjeta, plan.Precio);
                throw;
            }
            if (!contratado)
            {
                BilleteraBLL.Devolver(tarjeta, plan.Precio);
                return "Ya tenés un seguro activo. Podés contratar otro cuando termine.";
            }
            bitacora.Registrar(idUsuario, usuario, "Contratación del seguro " + plan.Aseguradora + " " + plan.Nombre + " (póliza "
                + seguro.NumeroPoliza + ")", "Informativa");
            return null;
        }

        // CU-006-032: cambia el plan del seguro activo por otro más caro o más barato, de la misma aseguradora o de otra.
        // La cobertura cambia ahora, con una póliza nueva; la diferencia de precio por los días que faltan del mes pagado
        // se suma (o se descuenta) en la próxima renovación (R39). Devuelve null si salió bien, o el texto del error.
        public string CambiarPlan(int idUsuario, string usuario, int idPlan)
        {
            var activo = seguros.ObtenerActivo(idUsuario);
            if (activo == null) return "No tenés un seguro activo para modificar.";
            if (activo.CancelaAlVencer)
                return "Cancelaste el seguro: te cubre hasta el " + activo.Vence.ToString("dd/MM/yyyy") + " y ya no se puede cambiar de plan.";
            var plan = seguros.ObtenerPlan(idPlan);
            if (plan == null) return "Elegí uno de los planes.";
            if (plan.IdPlan == activo.Plan.IdPlan) return "Ya tenés ese plan.";
            // R38 (ERR041): con una carga en viaje no se cambia la cobertura, para evitar fraudes contra la aseguradora.
            if (viajes.TieneCargaEnViaje(idUsuario))
                return "No podés cambiar de plan mientras tengas una carga en viaje (en tránsito o en destino). Hacelo cuando se entregue.";
            var poliza = AseguradoraServicio.EmitirPoliza(plan.Aseguradora, plan.Nombre);
            if (poliza == null) return "La aseguradora no responde en este momento. Intentá más tarde.";   // ERR042

            var ajuste = SuscripcionBLL.Prorratear(activo.Monto, plan.Precio, activo.Vence);
            if (!seguros.CambiarPlan(activo.IdSuscripcion, plan, poliza, ajuste)) return "El seguro ya no está activo.";
            notificaciones.Avisar(idUsuario, "Tu seguro pasó al plan " + plan.Nombre + " de " + plan.Aseguradora + " (póliza " + poliza
                + "), por " + plan.Precio.ToString("C0") + " por mes." + (ajuste == 0 ? string.Empty : " La próxima renovación "
                + (ajuste > 0 ? "suma " : "descuenta ") + Math.Abs(ajuste).ToString("C0") + " por los días que faltan de este mes."));
            bitacora.Registrar(idUsuario, usuario, "Cambio de plan del seguro: de " + activo.Plan.Aseguradora + " " + activo.Plan.Nombre
                + " a " + plan.Aseguradora + " " + plan.Nombre + " (póliza " + poliza + ")", "Informativa");
            return null;
        }

        // Lo que la próxima renovación sumaría (o descontaría) si cambia ahora al plan elegido (0 si no tiene seguro).
        public decimal CalcularAjusteCambio(int idUsuario, int idPlan)
        {
            var activo = seguros.ObtenerActivo(idUsuario);
            var plan = seguros.ObtenerPlan(idPlan);
            return activo == null || plan == null ? 0 : SuscripcionBLL.Prorratear(activo.Monto, plan.Precio, activo.Vence);
        }

        // CU-006-033: no se devuelve nada; el seguro sigue hasta el final del mes pagado y no se renueva.
        public string Cancelar(int idUsuario, string usuario)
        {
            var activo = seguros.ObtenerActivo(idUsuario);
            if (activo == null || !seguros.Cancelar(idUsuario)) return "No tenés un seguro para cancelar.";
            bitacora.Registrar(idUsuario, usuario, "Cancelación del seguro " + activo.Plan.Aseguradora + " " + activo.Plan.Nombre
                + " al vencer el " + activo.Vence.ToString("dd/MM/yyyy"), "Advertencia");
            return null;
        }

        // Rutina diaria: los seguros cuyo mes terminó se renuevan (se cobra otro mes a la misma tarjeta o al saldo) o,
        // si se cancelaron, la tarjeta ya no está o el saldo no alcanza, terminan.
        public void ProcesarVencimientos()
        {
            foreach (var seguro in seguros.ListarVencidos())
            {
                // R39: se cobra el mes más el ajuste de un cambio de plan; si el descuento supera el mes, lo que sobra queda.
                var total = seguro.Monto + seguro.AjustePendiente;
                var conSaldo = !seguro.IdTarjeta.HasValue;
                var tarjeta = conSaldo ? null : tarjetas.Obtener(seguro.IdTarjeta.Value, seguro.IdUsuario);
                var motivoFin = seguro.CancelaAlVencer ? string.Empty
                    : !conSaldo && tarjeta == null ? " (sin tarjeta para renovarlo)"
                    : conSaldo && transacciones.Saldo(seguro.IdUsuario) < Math.Max(0, total) ? " (sin saldo para renovarlo)"
                    : null;
                if (motivoFin != null)
                {
                    seguros.Terminar(seguro.IdSuscripcion);
                    bitacora.Registrar(seguro.IdUsuario, "sistema", "Fin del seguro " + seguro.Plan.Aseguradora + " " + seguro.Plan.Nombre
                        + motivoFin, "Informativa");
                    continue;
                }
                var pago = BilleteraBLL.Pago(seguro.IdUsuario, "Pago de seguro", tarjeta, "Renovación del seguro " + seguro.Plan.Aseguradora
                    + " " + seguro.Plan.Nombre + " (póliza " + seguro.NumeroPoliza + ")", Math.Max(0, total));
                if (seguros.Renovar(seguro, seguro.Vence.AddMonths(1), Math.Min(0, total), pago))
                    bitacora.Registrar(seguro.IdUsuario, "sistema", "Renovación del seguro " + seguro.Plan.Aseguradora + " "
                        + seguro.Plan.Nombre, "Informativa");
                else
                    BilleteraBLL.Devolver(tarjeta, Math.Max(0, total));
            }
        }
    }
}
