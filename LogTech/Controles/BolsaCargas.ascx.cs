using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class BolsaCargas : ControlBase
    {
        private const int ColumnaDistancia = 3;
        private const string MiUbicacion = "gps";

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            pnlAceptar.Visible = false;
            pnlMatch.Visible = Tiene("Consultar match de cargas");
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (ddlOrigen.Items.Count == 0) CargarCiudades();
                CargarBolsa();
                RegistrarConsulta("Consulta de la bolsa de cargas");
            }, lblMensaje);
        }

        private void CargarCiudades()
        {
            var nombres = new List<string>();
            foreach (var ciudad in negocio.ListarCiudades()) nombres.Add(ciudad.Nombre);
            Cargar(ddlOrigen, nombres, "Cualquiera");
            Cargar(ddlDestino, nombres, "Cualquiera");
            Cargar(ddlReferencia, nombres, null);
            ddlReferencia.Items.Insert(0, new ListItem("Mi ubicación actual (GPS)", MiUbicacion));
            ddlReferencia.Items.Insert(0, new ListItem("Elegí dónde estás", string.Empty));
        }

        // CU-001-004.
        private void CargarBolsa()
        {
            gvBolsa.Columns[ColumnaDistancia].Visible = false;
            gvBolsa.DataSource = negocio.ListarBolsaDeCargas(ddlOrigen.SelectedValue, ddlDestino.SelectedValue, IdUsuario);
            gvBolsa.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            pnlAceptar.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarBolsa();
                RegistrarConsulta("Consulta de la bolsa de cargas (filtros: " + Texto(ddlOrigen) + " → " + Texto(ddlDestino) + ")");
            }, lblMensaje);
        }

        private static string Texto(DropDownList lista)
        {
            return lista.SelectedValue == string.Empty ? "cualquiera" : lista.SelectedValue;
        }

        // CU-001-006. R4: la ubicación del navegador o la ciudad de referencia elegida. Busca apenas se elige;
        // sin elegir nada vuelve a la bolsa completa.
        protected void ddlReferencia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Permitir("Consultar match de cargas", lblMensaje)) return;
            pnlAceptar.Visible = false;
            lblMensaje.Text = string.Empty;
            if (ddlReferencia.SelectedValue == string.Empty)
            {
                Intentar(CargarBolsa, lblMensaje);
                return;
            }
            Intentar(() =>
            {
                decimal? latitud = null, longitud = null;
                if (ddlReferencia.SelectedValue == MiUbicacion)
                {
                    latitud = LeerCoordenada(hfLatitud.Value);
                    longitud = LeerCoordenada(hfLongitud.Value);
                }
                else
                {
                    foreach (var ciudad in negocio.ListarCiudades())
                    {
                        if (ciudad.Nombre != ddlReferencia.SelectedValue) continue;
                        latitud = ciudad.Latitud;
                        longitud = ciudad.Longitud;
                    }
                }

                string aviso;
                var ofertas = negocio.ConsultarMatch(IdUsuario, latitud, longitud, out aviso);
                RegistrarConsulta("Consulta de match de cargas");
                gvBolsa.Columns[ColumnaDistancia].Visible = true;
                gvBolsa.DataSource = ofertas;
                gvBolsa.DataBind();
                if (aviso != null) Mostrar(lblMensaje, aviso, true);
                else Mostrar(lblMensaje, "Cargas compatibles con tus vehículos, de la más cercana a la más lejana.", false);
            }, lblMensaje);
        }

        // ---- CU-001-005 ----

        protected void gvBolsa_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Aceptar" || !Permitir("Aceptar ofertas de carga", lblMensaje)) return;
            var id = Argumento(e);
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                var oferta = negocio.ObtenerViaje(id);
                if (oferta == null || oferta.Estado != "Pendiente")
                {
                    Mostrar(lblMensaje, "La oferta ya no está disponible: otro transportista la aceptó o fue dada de baja.", true);
                    CargarBolsa();
                    return;
                }
                GuardarId("IdAceptar", id);
                litAceptarViaje.Text = id.ToString();
                litAceptarDetalle.Text = HttpUtility.HtmlEncode(oferta.Origen + " → " + oferta.Destino + " · " + oferta.Mercaderia + " · "
                    + oferta.PesoToneladas.ToString("0.##") + " t · carga el " + oferta.FechaCarga.ToString("dd/MM/yyyy") + " · cobrás "
                    + oferta.NetoTransportista.ToString("C0") + (oferta.MedioPago == "Efectivo"
                        ? " (en efectivo: el dador lo paga en Pago Fácil o Rapipago hasta 30 días después del remito"
                            + (EsFlota ? ")" : "; si no querés esperar, adelantalo con factoring)")
                        : " (" + (oferta.MedioPago == "Saldo" ? "con saldo" : "con tarjeta") + ": lo cobrás en tu billetera al emitir el remito)"));
                pnlVehiculo.Visible = !EsFlota;
                pnlAvisoFlota.Visible = EsFlota;
                if (!EsFlota) CargarVehiculosLibres(oferta.PesoToneladas);
                pnlAceptar.Visible = true;
            }, lblMensaje);
        }

        // Solo los camiones activos, con la documentación en verde, libres y que soportan el peso. Abajo, por qué no
        // aparecen los demás.
        private void CargarVehiculosLibres(decimal peso)
        {
            string aviso;
            ddlVehiculo.Items.Clear();
            foreach (var vehiculo in negocio.ListarVehiculosParaViaje(IdUsuario, peso, null, out aviso))
                ddlVehiculo.Items.Add(new ListItem(vehiculo.Descripcion, vehiculo.IdVehiculo.ToString()));
            if (ddlVehiculo.Items.Count == 0) ddlVehiculo.Items.Add(new ListItem("Ningún vehículo puede hacer este viaje", string.Empty));
            litAvisoVehiculos.Text = HttpUtility.HtmlEncode(aviso);
        }

        protected void btnConfirmarAceptar_Click(object sender, EventArgs e)
        {
            if (!Permitir("Aceptar ofertas de carga", lblMensaje)) return;
            int idVehiculo;
            int? vehiculo = int.TryParse(ddlVehiculo.SelectedValue, out idVehiculo) ? idVehiculo : (int?)null;
            Intentar(() =>
            {
                var exito = EsFlota ? "¡Viaje tomado! Asigná el conductor y el vehículo desde \"Mis viajes\"." : "¡Viaje tomado! Lo encontrás en \"Mis viajes\".";
                if (Resultado(lblMensaje, negocio.AceptarOferta(LeerId("IdAceptar"), vehiculo, IdUsuario, Usuario), exito))
                    pnlAceptar.Visible = false;
                CargarBolsa();
            }, lblMensaje);
        }

        protected void btnCancelarAceptar_Click(object sender, EventArgs e)
        {
            pnlAceptar.Visible = false;
        }
    }
}
