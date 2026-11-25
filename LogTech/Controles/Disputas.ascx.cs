using System;
using System.Web;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    // CU-005-027 Gestionar Disputa Comercial. El dador abre una disputa por un viaje pagado con tarjeta con la
    // plata retenida (R13); el administrador la resuelve: devuelve el pago al dador o se lo paga al transportista.
    public partial class Disputas : ControlBase
    {
        private const string PermisoAbrir = "Gestionar disputas comerciales";
        private const string PermisoResolver = "Resolver disputas comerciales";

        protected bool PuedeResolver { get { return Tiene(PermisoResolver); } }

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            pnlResolver.Visible = false;
            Intentar(() =>
            {
                Cargar();
                RegistrarConsulta("Consulta de disputas comerciales");
            }, lblMensaje);
        }

        private void Cargar()
        {
            litBajada.Text = PuedeResolver ? "Reclamos de los dadores por viajes pagados con tarjeta o con saldo. Decidí a quién va el pago."
                                           : "Reclamá por un viaje pagado con tarjeta o con saldo antes de que se libere el pago.";
            pnlAbrir.Visible = Tiene(PermisoAbrir);
            if (pnlAbrir.Visible)
            {
                ddlViaje.Items.Clear();
                foreach (var viaje in negocio.ListarViajesReclamables(IdUsuario))
                    ddlViaje.Items.Add(new ListItem("N° " + viaje.IdViaje + " · " + viaje.Origen + " → " + viaje.Destino + " · "
                        + viaje.PrecioDador.ToString("C0"), viaje.IdViaje.ToString()));
                btnAbrir.Enabled = ddlViaje.Items.Count > 0;
                if (ddlViaje.Items.Count == 0) ddlViaje.Items.Add(new ListItem("No tenés viajes con el pago retenido", string.Empty));
            }
            // El administrador ve todas; el dador, las suyas.
            gvDisputas.DataSource = negocio.ListarDisputas(PuedeResolver ? (int?)null : IdUsuario);
            gvDisputas.DataBind();
        }

        protected void btnAbrir_Click(object sender, EventArgs e)
        {
            if (!Permitir(PermisoAbrir, lblMensaje)) return;
            int idViaje;
            int.TryParse(ddlViaje.SelectedValue, out idViaje);
            Intentar(() =>
            {
                var error = negocio.AbrirDisputa(idViaje, IdUsuario, Usuario, txtMotivo.Text);
                if (error == null) txtMotivo.Text = string.Empty;
                Cargar();
                Resultado(lblMensaje, error, "Disputa abierta: el pago queda congelado hasta que la resuelva el administrador.");
            }, lblMensaje);
        }

        protected void gvDisputas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Resolver" || !Permitir(PermisoResolver, lblMensaje)) return;
            var id = Argumento(e);
            Intentar(() =>
            {
                Cargar();
                var disputa = negocio.ListarDisputas(null).Find(d => d.IdDisputa == id);
                if (disputa == null || disputa.Estado != "Abierta") { Mostrar(lblMensaje, "La disputa ya fue resuelta.", true); return; }
                GuardarId("IdDisputa", id);
                litResolverViaje.Text = disputa.IdViaje.ToString();
                litResolverDetalle.Text = HttpUtility.HtmlEncode(disputa.Recorrido + " · el dador " + disputa.Dador + " pagó "
                    + disputa.Monto.ToString("C0") + " · transportista: " + (disputa.Transportista ?? "-") + " · viaje " + disputa.EstadoViaje
                    + ". Motivo: " + disputa.Motivo);
                txtComentario.Text = string.Empty;
                pnlResolver.Visible = true;
            }, lblMensaje);
        }

        protected void btnDevolver_Click(object sender, EventArgs e)
        {
            Resolver(true);
        }

        protected void btnPagar_Click(object sender, EventArgs e)
        {
            Resolver(false);
        }

        private void Resolver(bool devolverAlDador)
        {
            if (!Permitir(PermisoResolver, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.ResolverDisputa(LeerId("IdDisputa"), devolverAlDador, txtComentario.Text, IdUsuario, Usuario);
                Cargar();
                pnlResolver.Visible = error != null;
                Resultado(lblMensaje, error, devolverAlDador ? "Listo: se le devuelve el pago al dador." : "Listo: se le paga al transportista.");
            }, lblMensaje);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlResolver.Visible = false;
            Intentar(Cargar, lblMensaje);
        }
    }
}
