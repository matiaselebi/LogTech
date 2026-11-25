using System;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class MisCargas : ControlBase
    {
        private static readonly string[] Estados = { "Pendiente", "Asignado", "En camino a carga", "En origen", "En tránsito", "En destino", "Entregado", "Cancelado" };

        // Lo llama el panel al entrar al módulo (CU-001-004).
        public override void Abrir()
        {
            btnNueva.Visible = Tiene("Publicar ofertas de carga");
            if (ddlEstado.Items.Count == 0) Cargar(ddlEstado, Estados, "Todos");
            OcultarPaneles();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarOfertas();
                RegistrarConsulta("Consulta de mis ofertas de carga");
            }, lblMensaje);
        }

        // El calendario solo deja elegir fechas de carga válidas: de hoy a 6 meses (las controla validarRango).
        protected void Page_Load(object sender, EventArgs e)
        {
            txtFechaCarga.Attributes["min"] = DateTime.Today.ToString("yyyy-MM-dd");
            txtFechaCarga.Attributes["max"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");
        }

        protected static bool Es(object estado, string valor)
        {
            return Convert.ToString(estado) == valor;
        }

        private void CargarOfertas()
        {
            gvOfertas.DataSource = negocio.ListarOfertasDelDador(IdUsuario, ddlEstado.SelectedValue);
            gvOfertas.DataBind();
        }

        private void OcultarPaneles()
        {
            pnlFormulario.Visible = false;
            pnlMatch.Visible = false;
            pnlCalificar.Visible = false;
            ucDetalle.Ocultar();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            OcultarPaneles();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarOfertas();
                RegistrarConsulta("Consulta de mis ofertas de carga (" + (ddlEstado.SelectedValue == string.Empty ? "todas" : ddlEstado.SelectedValue) + ")");
            }, lblMensaje);
        }

        // ---- CU-001-001 y CU-001-002 ----

        protected void btnNueva_Click(object sender, EventArgs e)
        {
            if (!Permitir("Publicar ofertas de carga", lblMensaje)) { btnNueva.Visible = false; return; }
            OcultarPaneles();
            Intentar(() =>
            {
                CargarCiudades();
                GuardarId("IdOferta", 0);
                litTituloFormulario.Text = "Nueva oferta de carga";
                btnGuardar.Text = "Guardar y publicar";
                txtMercaderia.Text = txtPeso.Text = txtTarifa.Text = string.Empty;
                txtFechaCarga.Text = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                CargarMedioPago("Efectivo", null);
                pnlFormulario.Visible = true;
                lblCotizacion.Text = string.Empty;
            }, lblMensaje);
        }

        private void CargarCiudades()
        {
            var nombres = new System.Collections.Generic.List<string>();
            foreach (var ciudad in negocio.ListarCiudades()) nombres.Add(ciudad.Nombre);
            Cargar(ddlOrigen, nombres, "Elegí el origen");
            Cargar(ddlDestino, nombres, "Elegí el destino");
        }

        // Cotizador (WS_CotizadorFletes): completa la tarifa con la sugerida para el origen, el destino y el peso.
        protected void btnCotizar_Click(object sender, EventArgs e)
        {
            if (!Tiene("Publicar ofertas de carga") && !Permitir("Gestionar ofertas de carga", lblMensaje)) return;
            Intentar(() =>
            {
                string error;
                var cotizacion = negocio.CotizarFlete(ddlOrigen.SelectedValue, ddlDestino.SelectedValue, LeerDecimal(txtPeso.Text), out error);
                if (cotizacion == null) { Mostrar(lblCotizacion, error, true); return; }
                txtTarifa.Text = cotizacion.Tarifa.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                Mostrar(lblCotizacion, string.Format("Precio sugerido para unos {0:N0} km: {1:C0}. LogTech se queda con {2:C0} (7 %) y {3:C0} del remito digital, y el transportista cobra {4:C0}.",
                    cotizacion.DistanciaKm, cotizacion.Tarifa, cotizacion.Comision, cotizacion.CargoRemito, cotizacion.NetoTransportista), false);
            }, lblCotizacion);
        }

        // CU-005-025: efectivo, una tarjeta guardada o el saldo de la billetera. Mercado Pago aparece pero está deshabilitado.
        private void CargarMedioPago(string medio, int? idTarjeta)
        {
            ddlTarjeta.Items.Clear();
            foreach (var tarjeta in negocio.ListarTarjetas(IdUsuario))
                ddlTarjeta.Items.Add(new ListItem(tarjeta.Descripcion, tarjeta.IdTarjeta.ToString()));
            if (idTarjeta.HasValue && ddlTarjeta.Items.FindByValue(idTarjeta.ToString()) != null) ddlTarjeta.SelectedValue = idTarjeta.ToString();
            rblMedioPago.SelectedValue = medio;
            pnlTarjeta.Visible = medio == "Tarjeta";
            pnlSaldo.Visible = medio == "Saldo";
            if (pnlSaldo.Visible)
                litSaldo.Text = "Tenés " + negocio.ObtenerBilletera(IdUsuario).Saldo.ToString("C0") + " de saldo. Se descuenta cuando un"
                    + " transportista acepta el viaje y queda retenido hasta el remito.";
        }

        protected void rblMedioPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            Intentar(() =>
            {
                var medio = rblMedioPago.SelectedValue;
                if (medio == "MercadoPago")
                {
                    Mostrar(lblCotizacion, "Mercado Pago está deshabilitado momentáneamente. Podés pagar en efectivo, con tarjeta o con el saldo.", true);
                    medio = "Efectivo";
                }
                else if (medio == "Tarjeta" && negocio.ListarTarjetas(IdUsuario).Count == 0)
                {
                    Mostrar(lblCotizacion, "Todavía no tenés tarjetas: agregá una en Billetera.", true);
                    medio = "Efectivo";
                }
                else lblCotizacion.Text = string.Empty;
                CargarMedioPago(medio, null);
            }, lblCotizacion);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            var id = LeerId("IdOferta");
            if (!Permitir(id == 0 ? "Publicar ofertas de carga" : "Gestionar ofertas de carga", lblMensaje)) return;
            var oferta = new ViajeBE
            {
                IdViaje = id,
                Origen = ddlOrigen.SelectedValue,
                Destino = ddlDestino.SelectedValue,
                Mercaderia = txtMercaderia.Text,
                PesoToneladas = LeerDecimal(txtPeso.Text),
                FechaCarga = LeerFecha(txtFechaCarga.Text),
                TarifaAcordada = LeerDecimal(txtTarifa.Text),
                MedioPago = rblMedioPago.SelectedValue,
                IdTarjeta = rblMedioPago.SelectedValue == "Tarjeta" && ddlTarjeta.SelectedValue != string.Empty ? int.Parse(ddlTarjeta.SelectedValue) : (int?)null
            };
            Intentar(() =>
            {
                var error = id == 0 ? negocio.PublicarOferta(oferta, IdUsuario, Usuario) : negocio.ModificarOferta(oferta, IdUsuario, Usuario);
                var exito = (id == 0 ? "La oferta fue publicada en la bolsa de cargas. " : "La oferta fue actualizada. ")
                    + "Pagás " + oferta.PrecioDador.ToString("C0") + (oferta.DescuentoDador > 0 ? " (sin comisión: es uno de tus 3 primeros viajes)" : string.Empty)
                    + " y el transportista cobra " + oferta.NetoTransportista.ToString("C0") + ".";
                if (!Resultado(lblMensaje, error, exito)) return;
                pnlFormulario.Visible = false;
                CargarOfertas();
            }, lblMensaje);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlFormulario.Visible = false;
        }

        protected void gvOfertas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var id = Argumento(e);
            if (id == 0) return;
            OcultarPaneles();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (e.CommandName == "Modificar" && Permitir("Gestionar ofertas de carga", lblMensaje)) AbrirModificacion(id);
                else if (e.CommandName == "Baja" && Permitir("Gestionar ofertas de carga", lblMensaje))
                {
                    Resultado(lblMensaje, negocio.CancelarOferta(id, IdUsuario, Usuario), "La oferta fue dada de baja.");
                    CargarOfertas();
                }
                else if (e.CommandName == "Match" && Permitir("Consultar match de cargas", lblMensaje)) MostrarMatch(id);
                else if (e.CommandName == "Monitorear" && Permitir("Consultar monitoreo de viaje", lblMensaje))
                {
                    if (!ucDetalle.MostrarMonitoreo(id)) Mostrar(lblMensaje, "El viaje no existe.", true);
                }
                else if (e.CommandName == "Remito" && Permitir("Gestionar remitos digitales", lblMensaje))
                {
                    if (!ucDetalle.MostrarRemito(id)) Mostrar(lblMensaje, "El viaje todavía no tiene remito.", true);
                }
                else if (e.CommandName == "Calificar" && Permitir("Calificar servicio de transporte", lblMensaje)) AbrirCalificacion(id);
            }, lblMensaje);
        }

        private void AbrirModificacion(int id)
        {
            var oferta = negocio.ObtenerViaje(id);
            if (oferta == null || oferta.IdDador != IdUsuario) { Mostrar(lblMensaje, "La oferta no existe.", true); return; }
            if (oferta.Estado != "Pendiente") { Mostrar(lblMensaje, "La oferta ya no está disponible: fue aceptada o dada de baja.", true); return; }
            CargarCiudades();
            GuardarId("IdOferta", id);
            litTituloFormulario.Text = "Modificar la oferta N° " + id;
            btnGuardar.Text = "Guardar cambios";
            ddlOrigen.SelectedValue = oferta.Origen;
            ddlDestino.SelectedValue = oferta.Destino;
            txtMercaderia.Text = oferta.Mercaderia;
            txtPeso.Text = oferta.PesoToneladas.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            txtFechaCarga.Text = oferta.FechaCarga.ToString("yyyy-MM-dd");
            txtTarifa.Text = oferta.TarifaAcordada.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            CargarMedioPago(oferta.MedioPago, oferta.IdTarjeta);
            pnlFormulario.Visible = true;
            lblCotizacion.Text = string.Empty;
        }

        // ---- CU-001-006 (dador): camiones compatibles ----

        private void MostrarMatch(int id)
        {
            string aviso;
            var camiones = negocio.ConsultarCamionesCompatibles(id, IdUsuario, out aviso);
            RegistrarConsulta("Consulta de camiones compatibles con la oferta N° " + id);
            if (aviso != null) { Mostrar(lblMensaje, aviso, true); return; }
            litMatchViaje.Text = id.ToString();
            rptMatch.DataSource = camiones;
            rptMatch.DataBind();
            pnlMatch.Visible = true;
        }

        protected void btnCerrarMatch_Click(object sender, EventArgs e)
        {
            pnlMatch.Visible = false;
        }

        // ---- CU-007-037 ----

        private void AbrirCalificacion(int id)
        {
            var viaje = negocio.ObtenerViaje(id);
            if (viaje == null || viaje.IdDador != IdUsuario) { Mostrar(lblMensaje, "El viaje no existe.", true); return; }
            GuardarId("IdCalificar", id);
            litCalificarViaje.Text = id.ToString();
            litCalificarDetalle.Text = HttpUtility.HtmlEncode(viaje.Origen + " → " + viaje.Destino + " · Transportista: " + viaje.Transportista);
            rblPuntaje.ClearSelection();
            txtResena.Text = string.Empty;
            pnlCalificar.Visible = true;
        }

        protected void btnCalificar_Click(object sender, EventArgs e)
        {
            if (!Permitir("Calificar servicio de transporte", lblMensaje)) return;
            int puntaje;
            int.TryParse(rblPuntaje.SelectedValue, out puntaje);
            Intentar(() =>
            {
                if (!Resultado(lblMensaje, negocio.CalificarViaje(LeerId("IdCalificar"), puntaje, txtResena.Text, IdUsuario, Usuario),
                    "¡Gracias! La calificación quedó registrada en el perfil del transportista.")) return;
                pnlCalificar.Visible = false;
                CargarOfertas();
            }, lblMensaje);
        }

        protected void btnCancelarCalificacion_Click(object sender, EventArgs e)
        {
            pnlCalificar.Visible = false;
        }
    }
}
