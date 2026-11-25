using System;
using System.Globalization;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    // CU-008-039: el cliente (dador, transportista o flota) ve y descarga sus facturas. El administrador ve las de cada
    // mes, exporta ReporteFacturacion.xml y vuelve a pedir el CAE de las que ARCA no autorizó (ERR022).
    public partial class Facturas : ControlBase
    {
        private bool EsAdministrador { get { return Tiene("Generar facturas corporativas"); } }

        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            pnlAdministrador.Visible = EsAdministrador;
            if (EsAdministrador && ddlMes.Items.Count == 0) CargarMeses();
            Intentar(() =>
            {
                CargarFacturas();
                RegistrarConsulta("Consulta de facturas");
            }, lblMensaje);
        }

        // Los últimos 12 meses (valor yyyy-MM).
        private void CargarMeses()
        {
            var mes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            for (var i = 0; i < 12; i++)
                ddlMes.Items.Add(new ListItem(mes.AddMonths(-i).ToString("MM/yyyy"), mes.AddMonths(-i).ToString("yyyy-MM")));
        }

        private DateTime MesElegido
        {
            get { return DateTime.ParseExact(ddlMes.SelectedValue, "yyyy-MM", CultureInfo.InvariantCulture); }
        }

        private void CargarFacturas()
        {
            var lista = EsAdministrador ? negocio.ListarFacturasDelMes(MesElegido) : negocio.ListarMisFacturas(IdUsuario);
            gvFacturas.Columns[2].Visible = EsAdministrador;   // el cliente
            gvFacturas.DataSource = lista;
            gvFacturas.DataBind();
            if (!EsAdministrador) return;

            decimal total = 0, iva = 0;
            var pendientes = 0;
            foreach (var factura in lista)
            {
                total += factura.Total;
                iva += factura.Iva;
                if (factura.Estado != "Emitida") pendientes++;
            }
            litCantidad.Text = lista.Count.ToString();
            litPendientes.Text = pendientes == 0 ? "Todas con CAE" : pendientes + " pendientes de CAE";
            litTotal.Text = total.ToString("C0");
            litIva.Text = iva.ToString("C0");
        }

        protected void ddlMes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Permitir("Generar facturas corporativas", lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            Intentar(CargarFacturas, lblMensaje);
        }

        protected void btnReintentar_Click(object sender, EventArgs e)
        {
            if (!Permitir("Generar facturas corporativas", lblMensaje)) return;
            Intentar(() =>
            {
                negocio.ReintentarFacturasPendientes();
                CargarFacturas();
                Mostrar(lblMensaje, "Se volvió a pedir el CAE de las facturas pendientes.", false);
            }, lblMensaje);
        }

        protected void btnExportarXml_Click(object sender, EventArgs e)
        {
            if (!Permitir("Generar facturas corporativas", lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            byte[] xml = null;
            Intentar(() => xml = negocio.ExportarReporteFacturacion(MesElegido, IdUsuario, Usuario), lblMensaje);
            if (xml == null)
            {
                if (string.IsNullOrEmpty(lblMensaje.Text)) Mostrar(lblMensaje, "No hay facturas en el mes elegido.", true);
                return;
            }
            Descargar(xml, "application/xml", "ReporteFacturacion-" + MesElegido.ToString("yyyy-MM") + ".xml");
        }

        protected void gvFacturas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Pdf" || (!Tiene("Consultar facturas") && !Permitir("Generar facturas corporativas", lblMensaje))) return;
            lblMensaje.Text = string.Empty;
            var id = Argumento(e);
            byte[] pdf = null;
            Intentar(() => pdf = negocio.ObtenerFacturaPdf(id, IdUsuario, EsAdministrador, Usuario), lblMensaje);
            if (pdf == null)
            {
                if (string.IsNullOrEmpty(lblMensaje.Text)) Mostrar(lblMensaje, "La factura no existe o todavía no tiene CAE.", true);
                return;
            }
            Descargar(pdf, "application/pdf", "Factura-LogTech-" + id + ".pdf");
        }

        private void Descargar(byte[] archivo, string tipo, string nombre)
        {
            Response.Clear();
            Response.ContentType = tipo;
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + nombre);
            Response.BinaryWrite(archivo);
            Response.End();
        }
    }
}
