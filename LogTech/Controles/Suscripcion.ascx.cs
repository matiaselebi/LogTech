using System;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;
using LogTech.Servicios;

namespace LogTech.Controles
{
    // CU-008-040 a 043: la flota contrata la Flota SaaS (paga por cada vehículo) y el transportista la membresía Premium.
    // El dador también puede contratar la Flota SaaS (desde «Suscripción» de arriba): al pagarla, su cuenta pasa a flota.
    // Muestra la vigente con lo que paga por mes, deja cambiar con qué se paga y darla de baja.
    public partial class Suscripcion : ControlBase
    {
        // La flota y el dador ven la Flota SaaS; el transportista, la Premium.
        private bool DeFlota { get { return Rol != "Transportista"; } }
        private bool EsDador { get { return Rol == "Dador de carga"; } }

        private bool PuedeGestionar
        {
            get { return Tiene("Gestionar suscripción SaaS") || Tiene("Gestionar suscripción premium"); }
        }

        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            pnlConfirmarBaja.Visible = false;
            Intentar(() =>
            {
                Cargar();
                RegistrarConsulta("Consulta de la suscripción");
            }, lblMensaje);
        }

        private void Cargar()
        {
            var vigente = negocio.ObtenerSuscripcion(IdUsuario, Rol);
            litTitulo.Text = DeFlota ? "Flota SaaS" : "Premium transportista";
            var precio = negocio.CalcularMontoSuscripcion(Rol, 1).ToString("C0");   // por vehículo, o el fijo de la Premium
            litBajada.Text = DeFlota
                ? "Gestioná tu flota en LogTech por " + precio + " por vehículo por mes."
                : "Membresía para transportistas por " + precio + " por mes.";
            litBeneficios.Text = DeFlota
                ? "Pagás por cada vehículo que tenés cargado. Cada vehículo que sumes se cobra en el momento; si das uno de baja, "
                    + "ese mes ya está pagado y la próxima renovación cobra uno menos. Se paga con tarjeta o con el saldo y se renueva sola cada mes."
                    + (EsDador ? " Al pagarla, tu cuenta pasa a ser de flota: cargás tus vehículos y conductores, tomás cargas"
                        + " de la bolsa y dejás de publicar cargas." : string.Empty)
                : "No pagás la comisión del 7 % en tus viajes y te avisamos de las cargas de alta rentabilidad.";

            ddlTarjeta.Items.Clear();
            ddlTarjeta.Items.Add(new ListItem("Elegí cómo pagás", string.Empty));
            foreach (var tarjeta in negocio.ListarTarjetas(IdUsuario))
                ddlTarjeta.Items.Add(new ListItem(tarjeta.Descripcion, tarjeta.IdTarjeta.ToString()));
            // 0: con el saldo de la billetera (también las renovaciones).
            ddlTarjeta.Items.Add(new ListItem("Saldo de la billetera (" + negocio.ObtenerBilletera(IdUsuario).Saldo.ToString("C0") + ")", "0"));

            pnlVigente.Visible = vigente != null;
            pnlBaja.Visible = vigente != null && vigente.Estado == "Activa";
            pnlFormulario.Visible = vigente == null || vigente.Estado == "Activa";
            litFormulario.Text = vigente == null ? "Contratar" : "Cambiar con qué pagás";
            btnGuardar.Text = vigente == null ? "Pagar y contratar" : "Guardar cambios";
            if (vigente != null) MostrarVigente(vigente);
            litTotal.Text = vigente != null ? string.Empty : "Total por mes: " + TotalAlContratar();

            var facturas = negocio.ListarMisFacturas(IdUsuario).FindAll(f => f.IdSuscripcion.HasValue);
            gvFacturas.DataSource = facturas;
            gvFacturas.DataBind();
        }

        // Lo que pagaría al contratarla: la Flota SaaS, sus vehículos activos (como mínimo uno).
        private string TotalAlContratar()
        {
            if (!DeFlota) return negocio.CalcularMontoSuscripcion(Rol, 1).ToString("C0");
            var vehiculos = negocio.VehiculosACobrar(IdUsuario);
            return negocio.CalcularMontoSuscripcion(Rol, vehiculos).ToString("C0") + " (" + vehiculos + " vehículo(s) × "
                + negocio.CalcularMontoSuscripcion(Rol, 1).ToString("C0") + ")";
        }

        private void MostrarVigente(SuscripcionBE vigente)
        {
            litEstadoCorto.Text = vigente.Estado;
            litDesde.Text = "Desde el " + vigente.FechaAlta.ToString("dd/MM/yyyy");
            litPrecio.Text = vigente.Monto.ToString("C0");
            litCupo.Text = vigente.CantidadPatentes.HasValue
                ? vigente.CantidadPatentes + " vehículo(s) × " + negocio.CalcularMontoSuscripcion(Rol, 1).ToString("C0")
                : "Precio fijo";
            litVence.Text = vigente.Vence.ToString("dd/MM/yyyy");
            litTarjeta.Text = HttpUtility.HtmlEncode(vigente.IdTarjeta.HasValue ? vigente.Tarjeta ?? "Sin tarjeta" : "Saldo de la billetera");
            litEstado.Text = vigente.CancelaAlVencer
                ? "Dada de baja: la usás hasta el " + vigente.Vence.ToString("dd/MM/yyyy") + " y no se vuelve a cobrar."
                : "Pagás " + vigente.Monto.ToString("C0") + " por mes. Se renueva sola el " + vigente.Vence.ToString("dd/MM/yyyy")
                    + (vigente.CantidadPatentes.HasValue ? " con los vehículos que tengas cargados ese día." : ".");
            var medio = vigente.IdTarjeta.HasValue ? vigente.IdTarjeta.ToString() : "0";
            if (ddlTarjeta.Items.FindByValue(medio) != null) ddlTarjeta.SelectedValue = medio;
        }

        // CU-008-040 (contratar) y CU-008-041 (cambiar con qué se paga).
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Tiene("Gestionar suscripción SaaS") && !Permitir("Gestionar suscripción premium", lblMensaje)) return;
            int idTarjeta;
            int.TryParse(ddlTarjeta.SelectedValue, out idTarjeta);
            Intentar(() =>
            {
                var nueva = negocio.ObtenerSuscripcion(IdUsuario, Rol) == null;
                var error = nueva
                    ? negocio.ContratarSuscripcion(IdUsuario, Usuario, Rol, idTarjeta)
                    : negocio.ModificarSuscripcion(IdUsuario, Usuario, Rol, idTarjeta);
                if (error != null) { Mostrar(lblMensaje, error, true); return; }
                if (EsDador)
                {
                    // Ya es de flota: se vuelve a abrir el panel para que cargue el menú del rol nuevo.
                    GestorSesion.Instancia.CambiarRol("Administrador de Flota");
                    Response.Redirect("~/Panel.aspx?seccion=Suscripcion", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                Cargar();
                Mostrar(lblMensaje, nueva ? "Suscripción contratada. Te enviamos la factura y la ves en Facturas."
                    : "Listo: desde la próxima renovación se cobra con lo que elegiste.", false);
            }, lblMensaje);
        }

        // CU-008-042, primer paso: avisa la fecha de corte (R40) y pide el motivo.
        protected void btnBaja_Click(object sender, EventArgs e)
        {
            if (!Tiene("Gestionar suscripción SaaS") && !Permitir("Gestionar suscripción premium", lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                var vigente = negocio.ObtenerSuscripcion(IdUsuario, Rol);
                if (vigente == null) return;
                litAvisoBaja.Text = "Vas a seguir usando " + (DeFlota ? "Mis vehículos, Conductores y el reporte de la flota" : "los beneficios Premium")
                    + " hasta el <strong>" + vigente.Vence.ToString("dd/MM/yyyy") + "</strong>. Después no se renueva ni se cobra.";
                pnlConfirmarBaja.Visible = true;
            }, lblMensaje);
        }

        protected void btnConfirmarBaja_Click(object sender, EventArgs e)
        {
            if (!Tiene("Gestionar suscripción SaaS") && !Permitir("Gestionar suscripción premium", lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.CancelarSuscripcion(IdUsuario, Usuario, Rol, ddlMotivo.SelectedValue);
                if (error != null) { Mostrar(lblMensaje, error, true); return; }
                pnlConfirmarBaja.Visible = false;
                Cargar();
                Mostrar(lblMensaje, "La suscripción quedó dada de baja: la usás hasta que venza el mes pagado.", false);
            }, lblMensaje);
        }

        protected void btnCancelarBaja_Click(object sender, EventArgs e)
        {
            pnlConfirmarBaja.Visible = false;
        }
    }
}
