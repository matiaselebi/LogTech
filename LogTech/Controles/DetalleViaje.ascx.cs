using System;
using System.Globalization;
using System.Web;

namespace LogTech.Controles
{
    public partial class DetalleViaje : ControlBase
    {
        // CU-007-036. Devuelve false si el viaje no es del usuario.
        public bool MostrarMonitoreo(int idViaje)
        {
            Ocultar();
            var ok = false;
            Intentar(() =>
            {
                var monitoreo = negocio.ConsultarMonitoreo(idViaje, IdUsuario);
                if (monitoreo == null) return;
                var viaje = monitoreo.Viaje;
                litViaje.Text = viaje.IdViaje.ToString();
                litRecorrido.Text = HttpUtility.HtmlEncode(viaje.Origen + " → " + viaje.Destino + " · " + viaje.Mercaderia
                    + " (" + viaje.PesoToneladas.ToString("0.##") + " t)" + (viaje.Patente == null ? "" : " · Camión " + viaje.Patente));
                ucEstado.Estado = viaje.Estado;
                // R18 (ERR020): sin ubicación reciente se avisa y se muestra la última guardada.
                lblAviso.Text = monitoreo.Aviso;
                lblAviso.Visible = monitoreo.Aviso != null;
                frmMapa.Src = UrlMapa(monitoreo.Latitud, monitoreo.Longitud);
                rptEventos.DataSource = monitoreo.Eventos;
                rptEventos.DataBind();
                pnlMonitoreo.Visible = true;
                RegistrarConsulta("Consulta del monitoreo del viaje N° " + idViaje);
                ok = true;
            }, lblDetalle);
            return ok;
        }

        // CU-004-023. Devuelve false si el viaje no es del usuario o no tiene remito.
        public bool MostrarRemito(int idViaje)
        {
            Ocultar();
            var ok = false;
            Intentar(() =>
            {
                var remito = negocio.ConsultarRemito(idViaje, IdUsuario);
                if (remito == null) return;
                litRemitoViaje.Text = remito.IdViaje.ToString();
                litRemitoRecorrido.Text = HttpUtility.HtmlEncode(remito.Origen + " → " + remito.Destino);
                litRemitoMercaderia.Text = HttpUtility.HtmlEncode(remito.Mercaderia);
                litRemitoDador.Text = HttpUtility.HtmlEncode(remito.Dador);
                litRemitoTransportista.Text = HttpUtility.HtmlEncode(remito.Transportista);
                litRemitoReceptor.Text = HttpUtility.HtmlEncode(remito.Receptor + " (DNI " + remito.DniReceptor + ")");
                litRemitoFecha.Text = remito.FechaEmision.ToString("dd/MM/yyyy HH:mm:ss");
                litRemitoToken.Text = remito.Token;
                lblIntegridad.Text = remito.Valido
                    ? "Token verificado: el remito no fue modificado desde que se emitió."
                    : "Atención: el token no coincide con los datos. El remito fue alterado.";
                lblIntegridad.CssClass = remito.Valido ? "panel-message success" : "panel-message error";
                lnkRemitoFoto.NavigateUrl = "~/Archivo.ashx?tipo=remito&id=" + remito.IdViaje;
                pnlRemito.Visible = true;
                RegistrarConsulta("Consulta del remito digital del viaje N° " + idViaje);
                ok = true;
            }, lblDetalle);
            return ok;
        }

        public void Ocultar()
        {
            pnlMonitoreo.Visible = false;
            pnlRemito.Visible = false;
            lblDetalle.Text = string.Empty;
        }

        protected void btnCerrar_Click(object sender, EventArgs e)
        {
            Ocultar();
        }

        // Mapa de OpenStreetMap centrado en el punto, con un marcador.
        private static string UrlMapa(decimal latitud, decimal longitud)
        {
            var c = CultureInfo.InvariantCulture;
            return string.Format(c, "https://www.openstreetmap.org/export/embed.html?bbox={0},{1},{2},{3}&layer=mapnik&marker={4},{5}",
                longitud - 0.08m, latitud - 0.05m, longitud + 0.08m, latitud + 0.05m, latitud, longitud);
        }
    }
}
