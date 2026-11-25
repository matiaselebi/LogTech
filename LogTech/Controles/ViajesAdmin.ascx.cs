using System;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    // Administrador: viajes solicitados y en progreso de toda la plataforma, la plata retenida en escrow y el detalle
    // de cada viaje con la entrega estimada.
    public partial class ViajesAdmin : ControlBase
    {
        private const string Permiso = "Supervisar viajes";

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            pnlDetalle.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                Cargar();
                RegistrarConsulta("Consulta de los viajes de la plataforma");
            }, lblMensaje);
        }

        private void Cargar()
        {
            var escrow = negocio.ObtenerEscrowTotal();
            litEscrow.Text = escrow.Monto.ToString("C0");
            litEscrowDetalle.Text = escrow.Cantidad + (escrow.Cantidad == 1 ? " viaje pagado" : " viajes pagados")
                + " con tarjeta, retenido hasta el remito";
            var solicitados = negocio.ListarViajesSolicitados();
            var enCurso = negocio.ListarViajesEnCurso();
            litSolicitados.Text = solicitados.Count.ToString("N0");
            litEnCurso.Text = enCurso.Count.ToString("N0");
            gvSolicitados.DataSource = solicitados;
            gvSolicitados.DataBind();
            gvEnCurso.DataSource = enCurso;
            gvEnCurso.DataBind();
        }

        // Lo que el viaje tiene retenido en escrow (los pagados con tarjeta o con saldo, hasta el remito).
        protected static string EnEscrow(object dato)
        {
            var viaje = (ViajeBE)dato;
            return viaje.MedioPago != "Efectivo" && (viaje.EstadoPago == "Retenido" || viaje.EstadoPago == "En disputa")
                ? viaje.PrecioDador.ToString("C0") : "-";
        }

        protected void gvEnCurso_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Detalles" || !Permitir(Permiso, lblMensaje)) return;
            var id = Argumento(e);
            Intentar(() =>
            {
                Cargar();
                MostrarDetalle(id);
                RegistrarConsulta("Consulta del viaje N° " + id + " (supervisión)");
            }, lblMensaje);
        }

        private void MostrarDetalle(int id)
        {
            var monitoreo = negocio.ConsultarViajeAdministrador(id);
            if (monitoreo == null) { Mostrar(lblMensaje, "El viaje no existe.", true); return; }
            var v = monitoreo.Viaje;
            litDetalleViaje.Text = v.IdViaje.ToString();
            if (v.Estado == "En destino")
            {
                litEntrega.Text = "Ya llegó a destino";
                litEntregaDetalle.Text = "Falta que el transportista emita el remito.";
            }
            else if (monitoreo.EntregaEstimada.HasValue)
            {
                litEntrega.Text = monitoreo.EntregaEstimada.Value.ToString("dd/MM/yyyy HH:mm");
                litEntregaDetalle.Text = "Faltan unos " + monitoreo.KmRestantes.ToString("N0") + " km (a 60 km/h de promedio"
                    + (v.Estado == "En tránsito" ? ", desde la última ubicación informada)." : ", saliendo el día de carga a las 8).");
            }
            else
            {
                litEntrega.Text = "Sin estimar";
                litEntregaDetalle.Text = "El viaje no está en curso.";
            }

            var datos = new StringBuilder();
            Dato(datos, "Recorrido", v.Origen + " → " + v.Destino);
            Dato(datos, "Carga", v.FechaCarga.ToString("dd/MM/yyyy") + " · " + v.Mercaderia + " · " + v.PesoToneladas.ToString("0.##") + " t");
            Dato(datos, "Dador de carga", v.Dador);
            Dato(datos, "Transportista", v.Transportista);
            Dato(datos, "Vehículo y conductor", (v.Patente ?? "Sin asignar") + (v.Conductor != null ? " · " + v.Conductor : string.Empty));
            Dato(datos, "Estado", v.Estado);
            Dato(datos, "Pago", v.PrecioDador.ToString("C0") + " · " + v.MedioPago + " · " + v.EstadoPago);
            Dato(datos, "Cobra el transportista", v.NetoTransportista.ToString("C0"));
            if (monitoreo.Eventos.Count > 0)
            {
                var ultimo = monitoreo.Eventos[monitoreo.Eventos.Count - 1];
                Dato(datos, "Última novedad", ultimo.Fecha.ToString("dd/MM HH:mm") + " · " + ultimo.Titulo);
            }
            if (monitoreo.Aviso != null) Dato(datos, "Aviso", monitoreo.Aviso);
            litDatos.Text = datos.ToString();
            pnlDetalle.Visible = true;
        }

        private static void Dato(StringBuilder datos, string titulo, string valor)
        {
            datos.Append("<dt>").Append(HttpUtility.HtmlEncode(titulo)).Append("</dt><dd>")
                 .Append(HttpUtility.HtmlEncode(valor ?? "-")).Append("</dd>");
        }

        protected void btnCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalle.Visible = false;
            Intentar(Cargar, lblMensaje);
        }
    }
}
