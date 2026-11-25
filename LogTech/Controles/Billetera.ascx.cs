using System;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    // CU-005-025 (tarjetas) y CU-005-026 Consultar Estado de Pago. El transportista y la flota tienen saldo:
    // cobran ahí los viajes, cargan saldo y retiran; el transportista además adelanta cobros con factoring
    // (CU-006-029). El dador ve sus tarjetas y sus pagos, y paga los viajes en efectivo en el punto de pago.
    public partial class Billetera : ControlBase
    {
        private const string Permiso = "Consultar estado de pago";
        private const string PermisoFactoring = "Gestionar factoring";

        // Todos tienen saldo (lo cargan con tarjeta y pagan con él). El transportista y la flota además cobran ahí los viajes;
        // el dador paga en el punto de pago los viajes en efectivo.
        private bool TomaViajes { get { return Tiene("Aceptar ofertas de carga"); } }

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                Cargar();
                RegistrarConsulta("Consulta de la billetera");
            }, lblMensaje);
        }

        private void Cargar()
        {
            var resumen = negocio.ObtenerBilletera(IdUsuario);
            var cbu = negocio.ObtenerCbuDeposito(IdUsuario);
            txtCbu.Text = cbu ?? string.Empty;
            btnEliminarCbu.Visible = cbu != null;
            if (string.IsNullOrEmpty(txtDestino.Text)) txtDestino.Text = cbu ?? string.Empty;
            ucResumen.MostrarSaldo = true;
            ucResumen.SaldoDisponible = resumen.Saldo;
            ucResumen.SaldoRetenido = resumen.EnEscrow;
            ucResumen.DetalleRetenido = TomaViajes
                ? "Lo que vas a cobrar de viajes pagados con tarjeta o con saldo cuando emitas el remito."
                : "Lo que pagaste con tarjeta o con saldo por viajes que todavía no se entregaron.";
            litBajada.Text = TomaViajes ? "Tu saldo, lo que vas a cobrar, tus tarjetas y tus movimientos." : "Tu saldo, tus tarjetas y los pagos de tus viajes.";
            litMovimientos.Text = TomaViajes
                ? "Cobros de viajes, adelantos de factoring, cargas, retiros y pagos con saldo."
                : "Pagos de tus viajes y servicios con tarjeta, con saldo o en un punto de pago, cargas, retiros y devoluciones.";

            var tarjetas = negocio.ListarTarjetas(IdUsuario);
            gvTarjetas.DataSource = tarjetas;
            gvTarjetas.DataBind();
            ddlTarjetaCarga.Items.Clear();
            foreach (var tarjeta in tarjetas) ddlTarjetaCarga.Items.Add(new ListItem(tarjeta.Descripcion, tarjeta.IdTarjeta.ToString()));
            gvMovimientos.DataSource = negocio.ListarMovimientos(IdUsuario);
            gvMovimientos.DataBind();

            // Cada parte de la billetera aparece solo con su permiso: el saldo, las cargas, los retiros, los movimientos y
            // las tarjetas con «Consultar estado de pago»; el factoring, con «Gestionar factoring».
            var conEstadoDePago = Tiene(Permiso);
            ucResumen.Visible = pnlCargar.Visible = pnlRetirar.Visible = pnlCbu.Visible = conEstadoDePago;
            secMovimientos.Visible = secMediosPago.Visible = conEstadoDePago;

            // El dador paga en el punto de pago los viajes en efectivo ya entregados.
            pnlAPagar.Visible = !TomaViajes && conEstadoDePago;
            if (!TomaViajes)
            {
                gvAPagar.DataSource = negocio.ListarViajesAPagar(IdUsuario);
                gvAPagar.DataBind();
            }

            // Factoring: solo el transportista (CU-006-029).
            pnlFactoring.Visible = Tiene(PermisoFactoring);
            if (pnlFactoring.Visible)
            {
                var puntaje = negocio.ObtenerPuntajeFactoring(IdUsuario);
                litPuntaje.Text = puntaje.Total.ToString();
                litPuntajeReputacion.Text = puntaje.Reputacion.ToString();
                litPuntajeViajes.Text = (puntaje.Viajes + puntaje.SinDisputas).ToString();
                gvAdelantables.DataSource = negocio.ListarViajesAdelantables(IdUsuario);
                gvAdelantables.DataBind();
                gvSolicitudes.DataSource = negocio.ListarSolicitudesFactoring(IdUsuario);
                gvSolicitudes.DataBind();
            }
        }

        // Lo que recibe hoy el transportista si adelanta ese cobro.
        protected decimal Adelanto(decimal neto)
        {
            return negocio.CotizarFactoring(neto).MontoAdelantado;
        }

        protected void gvAPagar_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if ((e.CommandName != "Pagar" && e.CommandName != "PagarSaldo") || !Permitir(Permiso, lblMensaje) || TomaViajes) return;
            Intentar(() => Terminar(e.CommandName == "PagarSaldo"
                    ? negocio.PagarViajeConSaldo(Argumento(e), IdUsuario, Usuario)
                    : negocio.PagarEnPuntoDePago(Argumento(e), IdUsuario, Usuario),
                "Pago registrado: el transportista ya tiene la plata."), lblMensaje);
        }

        protected void gvAdelantables_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Adelantar" || !Permitir(PermisoFactoring, lblMensaje)) return;
            Intentar(() => Terminar(negocio.SolicitarFactoring(Argumento(e), IdUsuario, Usuario),
                "Adelanto aprobado: ya está en tu saldo disponible."), lblMensaje);
        }

        // Después de cada operación: el mensaje y la billetera actualizada.
        private void Terminar(string error, string exito)
        {
            Cargar();
            Resultado(lblMensaje, error, exito);
        }

        // En PreRender: después de agregar la tarjeta, el vencimiento vuelve al mes actual.
        protected void Page_PreRender(object sender, EventArgs e)
        {
            PrepararVencimiento(txtVencimiento);
        }

        protected void btnAgregarTarjeta_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.AgregarTarjeta(IdUsuario, Usuario, txtTitular.Text, txtNumero.Text, LeerVencimiento(txtVencimiento.Text), txtCodigo.Text);
                if (error == null) txtTitular.Text = txtVencimiento.Text = string.Empty;
                txtNumero.Text = string.Empty;   // el número no queda en la pantalla, salga bien o mal
                Terminar(error, "Tarjeta agregada.");
            }, lblMensaje);
        }

        protected void gvTarjetas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Quitar" || !Permitir(Permiso, lblMensaje)) return;
            Intentar(() => Terminar(negocio.QuitarTarjeta(Argumento(e), IdUsuario, Usuario), "Tarjeta quitada."), lblMensaje);
        }

        protected void btnMercadoPago_Click(object sender, EventArgs e)
        {
            Mostrar(lblMensaje, "Mercado Pago está deshabilitado momentáneamente. Podés pagar en efectivo o con tarjeta.", true);
            Intentar(Cargar, lblMensaje);
        }

        protected void btnCargar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            int idTarjeta;
            int.TryParse(ddlTarjetaCarga.SelectedValue, out idTarjeta);
            Intentar(() =>
            {
                var error = negocio.CargarSaldo(IdUsuario, Usuario, idTarjeta, LeerDecimal(txtMontoCarga.Text));
                if (error == null) txtMontoCarga.Text = string.Empty;
                Terminar(error, "Saldo cargado.");
            }, lblMensaje);
        }

        protected void btnGuardarCbu_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var escrito = txtCbu.Text;
            Intentar(() =>
            {
                var error = negocio.GuardarCbuDeposito(IdUsuario, Usuario, escrito);
                if (error == null) txtDestino.Text = string.Empty;   // el retiro toma el CBU nuevo
                Terminar(error, "CBU guardado: los retiros se depositan ahí.");
                if (error != null) txtCbu.Text = escrito;
            }, lblMensaje);
        }

        protected void btnEliminarCbu_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.EliminarCbuDeposito(IdUsuario, Usuario);
                if (error == null) txtDestino.Text = string.Empty;
                Terminar(error, "CBU eliminado.");
            }, lblMensaje);
        }

        protected void btnRetirar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.RetirarSaldo(IdUsuario, Usuario, LeerDecimal(txtMontoRetiro.Text), txtDestino.Text);
                if (error == null) txtMontoRetiro.Text = txtDestino.Text = string.Empty;
                Terminar(error, "Listo: la transferencia está en camino.");
            }, lblMensaje);
        }
    }
}
