using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    // CU-008-044 Generar Reporte Gerencial para el administrador de flota: viajes entregados, lo que
    // cobró la flota y el rendimiento de cada conductor, con exportación a CSV (R41). Puede ser de toda la flota o de
    // uno de sus vehículos, por día, mes o año.
    public partial class ReporteFlota : ControlBase
    {
        private const string Permiso = "Generar reportes gerenciales";

        private PeriodoEstadistica Periodo
        {
            get { return (PeriodoEstadistica)Enum.Parse(typeof(PeriodoEstadistica), ddlPeriodo.SelectedValue); }
        }

        // 0 = toda la flota.
        private int IdVehiculo
        {
            get
            {
                int id;
                return int.TryParse(ddlVehiculo.SelectedValue, out id) ? id : 0;
            }
        }

        // Lo llama el panel al entrar al módulo (y cada vez que cambia el período o el vehículo).
        public override void Abrir()
        {
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (ddlVehiculo.Items.Count == 0) CargarVehiculos();
                Cargar();
                RegistrarConsulta("Consulta del reporte de la flota (" + Periodo + ", " + ddlVehiculo.SelectedItem.Text + ")");
            }, lblMensaje);
        }

        // Todos los vehículos de la flota, también los dados de baja (tienen viajes en el período).
        private void CargarVehiculos()
        {
            ddlVehiculo.Items.Add(new ListItem("Toda la flota", "0"));
            foreach (var vehiculo in negocio.ListarVehiculos(IdUsuario, string.Empty, string.Empty))
                ddlVehiculo.Items.Add(new ListItem(vehiculo.Patente + " · " + vehiculo.Marca + " " + vehiculo.Modelo
                    + (vehiculo.Estado == "Activo" ? "" : " (dado de baja)"), vehiculo.IdVehiculo.ToString()));
        }

        protected void ddlPeriodo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            Abrir();
        }

        private void Cargar()
        {
            var reporte = negocio.ObtenerReporteFlota(IdUsuario, Periodo, IdVehiculo);
            litViajes.Text = reporte.Viajes.ToString("N0");
            litPeriodo.Text = Periodo == PeriodoEstadistica.Diario ? "En los últimos 30 días"
                : Periodo == PeriodoEstadistica.Mensual ? "En los últimos 12 meses" : "En los últimos 5 años";
            litNeto.Text = reporte.Neto.ToString("C0");
            litFacturado.Text = reporte.Facturado.ToString("C0");
            litComision.Text = reporte.Comision.ToString("C0");
            litKilometros.Text = reporte.Kilometros.ToString("N0");
            litToneladas.Text = reporte.Toneladas.ToString("N1");
            litIncidencias.Text = reporte.Incidencias.ToString("N0");
            litEnCurso.Text = reporte.EnCurso.ToString("N0");
            litGastos.Text = reporte.GastosDelMes.ToString("C0");
            litGastosDetalle.Text = "Flota SaaS " + reporte.GastoSuscripcion.ToString("C0") + " y seguro " + reporte.GastoSeguro.ToString("C0");
            CargarGrafico(chrViajes, reporte.PorPeriodo);
            CargarGrafico(chrNeto, reporte.PorPeriodo);
            gvPeriodos.DataSource = reporte.PorPeriodo;
            gvPeriodos.DataBind();
            gvConductores.DataSource = reporte.Conductores;
            gvConductores.DataBind();
        }

        private static void CargarGrafico(Chart grafico, IList<PuntoEstadisticaBE> datos)
        {
            // Con muchos períodos las etiquetas no entran derechas: se giran.
            grafico.ChartAreas[0].AxisX.LabelStyle.Angle = datos.Count > 6 ? -90 : 0;
            grafico.DataSource = datos;
            grafico.DataBind();
        }

        protected void btnExportar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            string csv = null;
            lblMensaje.Text = string.Empty;
            Intentar(() => csv = negocio.ExportarReporteFlota(IdUsuario, Periodo, IdVehiculo, ddlVehiculo.SelectedItem.Text, Usuario), lblMensaje);
            if (csv == null)
            {
                // Sin viajes o sin conexión: se vuelve a mostrar el reporte con el aviso.
                var mensaje = lblMensaje.Text.Length > 0 ? lblMensaje.Text : "No hay viajes de la flota para exportar en este período.";
                Intentar(Cargar, lblMensaje);
                Mostrar(lblMensaje, mensaje, true);
                return;
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.ContentEncoding = Encoding.UTF8;
            Response.AppendHeader("Content-Disposition", "attachment; filename=reporte-flota-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv");
            // La marca BOM hace que Excel muestre bien los acentos.
            Response.BinaryWrite(Encoding.UTF8.GetPreamble());
            Response.Write(csv);
            Response.End();
        }
    }
}
