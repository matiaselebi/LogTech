using System;
using System.Web;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    public partial class ValidarDocumentacion : ControlBase
    {
        private const string Permiso = "Validar documentación";

        // Lo llama el panel al entrar al módulo (CU-003-019 del analista).
        public override void Abrir()
        {
            pnlRevision.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarDocumentos();
                RegistrarConsulta("Consulta de documentación para validar");
            }, lblMensaje);
        }

        private void CargarDocumentos()
        {
            gvDocumentos.DataSource = negocio.ListarDocumentosParaValidar(ddlEstado.SelectedValue);
            gvDocumentos.DataBind();
        }

        protected void ddlEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Abrir();
        }

        protected void gvDocumentos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Revisar" || !Permitir(Permiso, lblMensaje)) return;
            var id = Argumento(e);
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                foreach (var documento in negocio.ListarDocumentosParaValidar("Pendiente"))
                {
                    if (documento.IdDocumento != id) continue;
                    GuardarId("IdDocumento", id);
                    litRevisionTitulo.Text = HttpUtility.HtmlEncode(documento.Tipo + " de " + documento.Usuario);
                    litRevisionDetalle.Text = HttpUtility.HtmlEncode("Vence el " + documento.FechaVencimiento.ToString("dd/MM/yyyy")
                        + (documento.Patente == null ? "" : " · Vehículo " + documento.Patente)
                        + (documento.Conductor == null ? "" : " · Conductor " + documento.Conductor)
                        + " · Cargado el " + documento.FechaCarga.ToString("dd/MM/yyyy HH:mm"));
                    frmDocumento.Src = ResolveUrl("~/Archivo.ashx?tipo=documento&id=" + id);
                    txtMotivo.Text = string.Empty;
                    pnlRevision.Visible = true;
                    return;
                }
                Mostrar(lblMensaje, "El documento ya no está pendiente de revisión.", true);
                CargarDocumentos();
            }, lblMensaje);
        }

        // CU-003-020.
        protected void btnAprobar_Click(object sender, EventArgs e)
        {
            Validar(true);
        }

        protected void btnRechazar_Click(object sender, EventArgs e)
        {
            Validar(false);
        }

        private void Validar(bool aprobar)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.ValidarDocumento(LeerId("IdDocumento"), aprobar, txtMotivo.Text, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, aprobar ? "Documento aprobado. Le avisamos al usuario." : "Documento rechazado. Le avisamos al usuario el motivo.")) return;
                pnlRevision.Visible = false;
                CargarDocumentos();
            }, lblMensaje);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlRevision.Visible = false;
        }
    }
}
