using System;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    // CU-006-031 a CU-006-034: el transportista, la flota o el dador elige una aseguradora (simulada), contrata un plan por
    // mes y lo paga con una tarjeta guardada o una nueva (Mercado Pago aparece pero está deshabilitado). Ve su seguro
    // activo con el detalle del plan y puede cancelarlo: sigue hasta que vence el mes pagado y no se renueva.
    public partial class Seguros : ControlBase
    {
        // En PreRender: después de contratar, el vencimiento de la tarjeta nueva vuelve al mes actual.
        protected void Page_PreRender(object sender, EventArgs e)
        {
            PrepararVencimiento(txtVencimiento);
        }

        private const string Permiso = "Gestionar pólizas de seguro";

        // El usuario ya tiene un seguro vigente: no puede contratar otro (R16).
        protected bool TieneSeguro { get; private set; }

        // CU-006-032: con el seguro activo (y no cancelado) se puede pasar a cualquier otro plan.
        private bool PuedeCambiar { get; set; }
        private int IdPlanActivo { get; set; }

        protected bool PuedeCambiarA(int idPlan)
        {
            return PuedeCambiar && idPlan != IdPlanActivo;
        }

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            pnlPlanes.Visible = pnlContratar.Visible = pnlCambiar.Visible = false;
            Intentar(() =>
            {
                Cargar();
                RegistrarConsulta("Consulta de seguros");
            }, lblMensaje);
        }

        private void Cargar()
        {
            var activo = negocio.ObtenerSeguroActivo(IdUsuario);
            TieneSeguro = activo != null;
            PuedeCambiar = activo != null && !activo.CancelaAlVencer;
            IdPlanActivo = activo == null ? 0 : activo.Plan.IdPlan;
            pnlActivo.Visible = TieneSeguro;
            if (TieneSeguro)
            {
                litActivoPlan.Text = HttpUtility.HtmlEncode(activo.Plan.Nombre);
                litActivoAseguradora.Text = HttpUtility.HtmlEncode(activo.Plan.Aseguradora);
                litActivoPrecio.Text = activo.Monto.ToString("C0");
                litActivoTarjeta.Text = HttpUtility.HtmlEncode("Con " + (activo.IdTarjeta.HasValue ? activo.Tarjeta ?? "tarjeta" : "el saldo de la billetera"));
                litActivoVence.Text = activo.Vence.ToString("dd/MM/yyyy");
                litActivoRenovacion.Text = activo.CancelaAlVencer ? "Cancelado: no se renueva" : "Se renueva solo ese día";
                litActivoEstado.Text = activo.CancelaAlVencer
                    ? "Lo cancelaste: te cubre hasta el " + activo.Vence.ToString("dd/MM/yyyy") + " y después termina."
                    : "Te cubre hasta el " + activo.Vence.ToString("dd/MM/yyyy") + " y se renueva solo cada mes.";
                var datos = new StringBuilder();
                Dato(datos, "Cobertura por viaje", activo.Plan.CoberturaPorViaje.ToString("C0"));
                Dato(datos, "Viajes por mes", activo.Plan.TextoViajes);
                Dato(datos, "Qué cubre", activo.Plan.Detalle);
                Dato(datos, "N° de póliza", activo.NumeroPoliza);
                Dato(datos, "Contratado el", activo.FechaAlta.ToString("dd/MM/yyyy"));
                if (activo.AjustePendiente != 0)
                    Dato(datos, "Ajuste en la próxima renovación", (activo.AjustePendiente > 0 ? "suma " : "descuenta ")
                        + Math.Abs(activo.AjustePendiente).ToString("C0") + " por el cambio de plan");
                litActivoDetalle.Text = datos.ToString();
                btnCancelarSeguro.Visible = !activo.CancelaAlVencer;
            }

            rptAseguradoras.DataSource = negocio.ListarAseguradoras();
            rptAseguradoras.DataBind();
            var aseguradora = ViewState["Aseguradora"] as string;
            if (pnlPlanes.Visible && aseguradora != null) MostrarPlanes(aseguradora);

            gvHistorial.DataSource = negocio.ListarSeguros(IdUsuario);
            gvHistorial.DataBind();
        }

        private void MostrarPlanes(string aseguradora)
        {
            ViewState["Aseguradora"] = aseguradora;
            litAseguradora.Text = HttpUtility.HtmlEncode(aseguradora);
            gvPlanes.DataSource = negocio.ListarPlanesSeguro(aseguradora);
            gvPlanes.DataBind();
            pnlPlanes.Visible = true;
        }

        private static void Dato(StringBuilder datos, string titulo, string valor)
        {
            datos.Append("<dt>").Append(HttpUtility.HtmlEncode(titulo)).Append("</dt><dd>")
                 .Append(HttpUtility.HtmlEncode(valor ?? "-")).Append("</dd>");
        }

        protected void rptAseguradoras_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ver" || !Permitir(Permiso, lblMensaje)) return;
            pnlContratar.Visible = false;
            Intentar(() =>
            {
                pnlPlanes.Visible = true;
                ViewState["Aseguradora"] = Convert.ToString(e.CommandArgument);
                Cargar();
            }, lblMensaje);
        }

        protected void gvPlanes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            if (e.CommandName == "Cambiar")
            {
                PrepararCambio(Argumento(e));
                return;
            }
            if (e.CommandName != "Contratar") return;
            var idPlan = Argumento(e);
            Intentar(() =>
            {
                var plan = negocio.ObtenerPlanSeguro(idPlan);
                if (plan == null) return;
                GuardarId("IdPlan", idPlan);
                litPlanElegido.Text = HttpUtility.HtmlEncode(plan.Nombre + " de " + plan.Aseguradora);
                litPlanResumen.Text = HttpUtility.HtmlEncode(plan.Precio.ToString("C0") + " por mes · cubre hasta "
                    + plan.CoberturaPorViaje.ToString("C0") + " por viaje · " + plan.TextoViajes.ToLower() + " · " + plan.Detalle);
                ddlTarjeta.Items.Clear();
                foreach (var tarjeta in negocio.ListarTarjetas(IdUsuario))
                    ddlTarjeta.Items.Add(new ListItem(tarjeta.Descripcion, tarjeta.IdTarjeta.ToString()));
                // Sin tarjetas guardadas, arranca con «Otra tarjeta».
                rblPago.SelectedValue = ddlTarjeta.Items.Count > 0 ? "Guardada" : "Nueva";
                MostrarMedioPago();
                pnlContratar.Visible = true;
                pnlPlanes.Visible = true;
                Cargar();
            }, lblMensaje);
        }

        // CU-006-032: antes de confirmar muestra el plan nuevo y cuánto se ajusta en la próxima renovación (R39).
        private void PrepararCambio(int idPlan)
        {
            Intentar(() =>
            {
                var plan = negocio.ObtenerPlanSeguro(idPlan);
                var activo = negocio.ObtenerSeguroActivo(IdUsuario);
                if (plan == null || activo == null) return;
                GuardarId("IdPlanCambio", idPlan);
                litCambioPlan.Text = HttpUtility.HtmlEncode(plan.Nombre + " de " + plan.Aseguradora);
                litCambioResumen.Text = HttpUtility.HtmlEncode("Ahora tenés " + activo.Plan.Nombre + " de " + activo.Plan.Aseguradora + " por "
                    + activo.Monto.ToString("C0") + " por mes. El plan nuevo cuesta " + plan.Precio.ToString("C0") + " por mes y cubre hasta "
                    + plan.CoberturaPorViaje.ToString("C0") + " por viaje.");
                var ajuste = negocio.CalcularAjusteCambioSeguro(IdUsuario, idPlan);
                litCambioAjuste.Text = HttpUtility.HtmlEncode("La cobertura cambia ahora, con una póliza nueva. " + (ajuste == 0
                    ? "No hay diferencia de precio para ajustar."
                    : "Por los días que faltan de este mes, la renovación del " + activo.Vence.ToString("dd/MM/yyyy")
                        + (ajuste > 0 ? " suma " : " descuenta ") + Math.Abs(ajuste).ToString("C0") + "."));
                pnlContratar.Visible = false;
                pnlCambiar.Visible = true;
                pnlPlanes.Visible = true;
                Cargar();
            }, lblMensaje);
        }

        protected void btnConfirmarCambio_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.CambiarPlanSeguro(IdUsuario, Usuario, LeerId("IdPlanCambio"));
                if (error != null)
                {
                    Mostrar(lblMensaje, error, true);
                    return;
                }
                pnlCambiar.Visible = false;
                Cargar();
                Mostrar(lblMensaje, "Listo: cambiaste de plan. La aseguradora emitió la póliza nueva.", false);
            }, lblMensaje);
        }

        protected void btnCerrarCambio_Click(object sender, EventArgs e)
        {
            pnlCambiar.Visible = false;
        }

        protected void rblPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rblPago.SelectedValue == "MercadoPago")
            {
                Mostrar(lblMensaje, "Mercado Pago está deshabilitado momentáneamente. Podés pagar con tarjeta o con el saldo.", true);
                rblPago.SelectedValue = ddlTarjeta.Items.Count > 0 ? "Guardada" : "Nueva";
            }
            MostrarMedioPago();
        }

        private void MostrarMedioPago()
        {
            pnlTarjetaGuardada.Visible = rblPago.SelectedValue == "Guardada";
            pnlTarjetaNueva.Visible = rblPago.SelectedValue == "Nueva";
            pnlSaldo.Visible = rblPago.SelectedValue == "Saldo";
            if (pnlSaldo.Visible) litSaldo.Text = "Tenés " + negocio.ObtenerBilletera(IdUsuario).Saldo.ToString("C0") + " de saldo.";
            // La confirmación dice de dónde sale el primer mes: la tarjeta o el saldo.
            var cobro = pnlSaldo.Visible ? "Se descuenta el primer mes de tu saldo" : "Se cobra el primer mes a la tarjeta";
            btnConfirmar.OnClientClick = "if (!Page_ClientValidate('SeguroTarjeta') || !confirm('¿Contratar este seguro? " + cobro
                + " y se renueva solo cada mes.')) return false;";
        }

        protected void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                int idTarjeta;
                if (rblPago.SelectedValue == "Nueva")
                {
                    // La tarjeta nueva se valida y se guarda como en la Billetera (R21); se paga con ella.
                    var error = negocio.AgregarTarjeta(IdUsuario, Usuario, txtTitular.Text, txtNumero.Text, LeerVencimiento(txtVencimiento.Text), txtCodigo.Text);
                    txtNumero.Text = txtCodigo.Text = string.Empty;
                    if (error != null) { Mostrar(lblMensaje, error, true); return; }
                    idTarjeta = 0;
                    foreach (var tarjeta in negocio.ListarTarjetas(IdUsuario)) idTarjeta = Math.Max(idTarjeta, tarjeta.IdTarjeta);
                }
                else if (rblPago.SelectedValue == "Saldo") idTarjeta = 0;   // 0: con el saldo de la billetera
                else int.TryParse(ddlTarjeta.SelectedValue, out idTarjeta);

                var resultado = negocio.ContratarSeguro(IdUsuario, Usuario, LeerId("IdPlan"), idTarjeta);
                if (resultado == null)
                {
                    pnlContratar.Visible = pnlPlanes.Visible = false;
                    txtTitular.Text = txtVencimiento.Text = string.Empty;
                }
                Cargar();
                Resultado(lblMensaje, resultado, "¡Listo! Tu seguro está activo: lo ves arriba con su póliza y su vencimiento.");
            }, lblMensaje);
        }

        protected void btnCerrarContratar_Click(object sender, EventArgs e)
        {
            pnlContratar.Visible = false;
            Intentar(Cargar, lblMensaje);
        }

        protected void btnCancelarSeguro_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.CancelarSeguro(IdUsuario, Usuario);
                Cargar();
                Resultado(lblMensaje, error, "Listo: el seguro sigue activo hasta que vence el mes pagado y después no se renueva.");
            }, lblMensaje);
        }
    }
}
