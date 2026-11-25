using System;
using System.Web.UI.WebControls;

namespace LogTech.Controles
{
    public partial class Remitos : ControlBase
    {
        private const string Permiso = "Gestionar remitos digitales";

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            ucDetalle.Ocultar();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarRemitos();
                RegistrarConsulta("Consulta de remitos digitales");
            }, lblMensaje);
        }

        private void CargarRemitos()
        {
            gvRemitos.DataSource = negocio.ListarRemitos(IdUsuario, txtBuscar.Text);
            gvRemitos.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            ucDetalle.Ocultar();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarRemitos();
                RegistrarConsulta("Consulta de remitos digitales (búsqueda)");
            }, lblMensaje);
        }

        // CU-004-023: muestra el remito con la verificación del token.
        protected void gvRemitos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            if (e.CommandName == "Ver")
            {
                if (!ucDetalle.MostrarRemito(Argumento(e))) Mostrar(lblMensaje, "El remito no existe.", true);
                return;
            }
            if (e.CommandName == "Xml") DescargarXml(Argumento(e));
            if (e.CommandName == "Imprimir") Imprimir(Argumento(e));
        }

        // RemitoDigital.xml (anexo 12.5.3).
        private void DescargarXml(int idViaje)
        {
            byte[] xml = null;
            Intentar(() => xml = negocio.ExportarRemitoXml(idViaje, IdUsuario, Usuario), lblMensaje);
            if (xml == null)
            {
                if (string.IsNullOrEmpty(lblMensaje.Text)) Mostrar(lblMensaje, "El remito no existe.", true);
                return;
            }
            Response.Clear();
            Response.ContentType = "application/xml";
            Response.AppendHeader("Content-Disposition", "attachment; filename=RemitoDigital-viaje-" + idViaje + ".xml");
            Response.BinaryWrite(xml);
            Response.End();
        }

        // El remito pasado por RemitoDigital.xslt: se abre en otra pestaña, listo para imprimir.
        private void Imprimir(int idViaje)
        {
            string html = null;
            Intentar(() => html = negocio.ImprimirRemito(idViaje, IdUsuario, Usuario), lblMensaje);
            if (html == null)
            {
                if (string.IsNullOrEmpty(lblMensaje.Text)) Mostrar(lblMensaje, "El remito no existe.", true);
                return;
            }
            Response.Clear();
            Response.ContentType = "text/html";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Write(html);
            Response.End();
        }
    }
}
