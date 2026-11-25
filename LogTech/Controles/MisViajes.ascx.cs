using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class MisViajes : ControlBase
    {
        private const int ColumnaConductor = 7;
        private string[] recorrido;

        // Estados del viaje en curso, en orden (R17).
        private string[] Recorrido { get { return recorrido ?? (recorrido = negocio.EstadosDelRecorrido()); } }

        // Lo llama el panel al entrar al módulo.
        public override void Abrir()
        {
            OcultarPaneles();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarViajes();
                RegistrarConsulta("Consulta de mis viajes");
            }, lblMensaje);
        }

        protected static bool Es(object estado, string valor)
        {
            return Convert.ToString(estado) == valor;
        }

        protected bool EnCurso(object estado)
        {
            return Array.IndexOf(Recorrido, Convert.ToString(estado)) >= 0;
        }

        private void CargarViajes()
        {
            gvViajes.Columns[ColumnaConductor].Visible = EsFlota;
            gvViajes.DataSource = negocio.ListarMisViajes(IdUsuario, ddlVista.SelectedValue == "entregados");
            gvViajes.DataBind();
        }

        private void OcultarPaneles()
        {
            pnlEstado.Visible = false;
            pnlIncidencia.Visible = false;
            pnlRemito.Visible = false;
            pnlAsignar.Visible = false;
            ucDetalle.Ocultar();
        }

        protected void ddlVista_SelectedIndexChanged(object sender, EventArgs e)
        {
            Abrir();
        }

        protected void btnCerrarPanel_Click(object sender, EventArgs e)
        {
            OcultarPaneles();
        }

        protected void gvViajes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var id = Argumento(e);
            if (id == 0) return;
            OcultarPaneles();
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                var viaje = negocio.ObtenerViaje(id);
                if (viaje == null || viaje.IdTransportista != IdUsuario) { Mostrar(lblMensaje, "El viaje no existe o no está a tu cargo.", true); return; }
                GuardarId("IdViaje", id);
                var resumen = HttpUtility.HtmlEncode(viaje.Origen + " → " + viaje.Destino + " · " + viaje.Mercaderia + " (" + viaje.PesoToneladas.ToString("0.##") + " t)");

                if (e.CommandName == "Estado" && Permitir("Actualizar estado de viaje", lblMensaje))
                {
                    litEstadoViaje.Text = id.ToString();
                    litEstadoActual.Text = HttpUtility.HtmlEncode(viaje.Estado);
                    // Se ofrecen todos los estados; R17 controla que se elija el que sigue.
                    var opciones = new string[Recorrido.Length - 1];
                    Array.Copy(Recorrido, 1, opciones, 0, opciones.Length);
                    Cargar(ddlEstadoNuevo, opciones, null);
                    var siguiente = negocio.SiguienteEstado(viaje.Estado);
                    if (siguiente != null) ddlEstadoNuevo.SelectedValue = siguiente;
                    hfLatitud.Value = hfLongitud.Value = string.Empty;
                    pnlEstado.Visible = true;
                }
                else if (e.CommandName == "Incidencia" && Permitir("Reportar incidencias de viaje", lblMensaje))
                {
                    litIncidenciaViaje.Text = id.ToString();
                    Cargar(ddlTipoIncidencia, negocio.TiposDeIncidencia(), "Elegí el tipo");
                    txtDescripcion.Text = string.Empty;
                    pnlIncidencia.Visible = true;
                }
                else if (e.CommandName == "Remito" && Permitir("Gestionar remitos digitales", lblMensaje))
                {
                    litRemitoViaje.Text = id.ToString();
                    txtReceptor.Text = txtDniReceptor.Text = string.Empty;
                    pnlRemito.Visible = true;
                }
                else if (e.CommandName == "Asignar" && EsFlota && Permitir("Asignar viajes internos", lblMensaje))
                {
                    litAsignarViaje.Text = id.ToString();
                    litAsignarDetalle.Text = resumen;
                    CargarParaAsignar(viaje);
                    pnlAsignar.Visible = true;
                }
                else if (e.CommandName == "VerRemito" && Permitir("Gestionar remitos digitales", lblMensaje))
                {
                    if (!ucDetalle.MostrarRemito(id)) Mostrar(lblMensaje, "El viaje todavía no tiene remito.", true);
                }
                else if (e.CommandName == "Monitorear" && Permitir("Consultar monitoreo de viaje", lblMensaje)) ucDetalle.MostrarMonitoreo(id);
            }, lblMensaje);
        }

        // CU-007-035.
        protected void btnGuardarEstado_Click(object sender, EventArgs e)
        {
            if (!Permitir("Actualizar estado de viaje", lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.ActualizarEstadoViaje(LeerId("IdViaje"), ddlEstadoNuevo.SelectedValue,
                    LeerCoordenada(hfLatitud.Value), LeerCoordenada(hfLongitud.Value), IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, "Estado actualizado. Le avisamos al dador de carga.")) return;
                pnlEstado.Visible = false;
                CargarViajes();
            }, lblMensaje);
        }

        // CU-004-024.
        protected void btnReportar_Click(object sender, EventArgs e)
        {
            if (!Permitir("Reportar incidencias de viaje", lblMensaje)) return;
            Intentar(() =>
            {
                var error = negocio.ReportarIncidencia(LeerId("IdViaje"), ddlTipoIncidencia.SelectedValue, txtDescripcion.Text,
                    LeerArchivo(fuFotoIncidencia), fuFotoIncidencia.FileName, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, "Incidencia registrada. Le avisamos al dador de carga.")) return;
                pnlIncidencia.Visible = false;
            }, lblMensaje);
        }

        // CU-004-022.
        protected void btnEmitirRemito_Click(object sender, EventArgs e)
        {
            if (!Permitir("Gestionar remitos digitales", lblMensaje)) return;
            Intentar(() =>
            {
                var id = LeerId("IdViaje");
                var error = negocio.AltaRemito(id, txtReceptor.Text, txtDniReceptor.Text, LeerArchivo(fuFotoRemito), fuFotoRemito.FileName, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, "Remito emitido: el viaje quedó entregado.")) return;
                pnlRemito.Visible = false;
                CargarViajes();
                ucDetalle.MostrarRemito(id);
            }, lblMensaje);
        }

        // ---- CU-002-015 (flota) ----

        // Los conductores y vehículos que pueden hacer el viaje: habilitados (semáforo en verde), libres y, el vehículo, que
        // soporte el peso. Los que ya tiene asignados este viaje también aparecen (y quedan elegidos) para poder cambiar
        // solo uno de los dos. Abajo, por qué no aparecen los demás.
        private void CargarParaAsignar(ViajeBE viaje)
        {
            string avisoConductores, avisoVehiculos;
            ddlConductor.Items.Clear();
            foreach (var conductor in negocio.ListarConductoresParaViaje(IdUsuario, viaje.IdConductor, out avisoConductores))
                ddlConductor.Items.Add(new ListItem(conductor.NombreCompleto + " (DNI " + conductor.Dni + ")", conductor.IdConductor.ToString()));
            ddlVehiculo.Items.Clear();
            foreach (var vehiculo in negocio.ListarVehiculosParaViaje(IdUsuario, viaje.PesoToneladas, viaje.IdVehiculo, out avisoVehiculos))
            {
                var texto = vehiculo.Descripcion + (vehiculo.ConductorDesignado == null ? "" : " · conductor designado: " + vehiculo.ConductorDesignado);
                ddlVehiculo.Items.Add(new ListItem(texto, vehiculo.IdVehiculo.ToString()));
            }
            if (ddlConductor.Items.Count == 0) ddlConductor.Items.Add(new ListItem("Ningún conductor puede hacer este viaje", string.Empty));
            if (ddlVehiculo.Items.Count == 0) ddlVehiculo.Items.Add(new ListItem("Ningún vehículo puede hacer este viaje", string.Empty));
            if (viaje.IdConductor.HasValue && ddlConductor.Items.FindByValue(viaje.IdConductor.ToString()) != null)
                ddlConductor.SelectedValue = viaje.IdConductor.ToString();
            if (viaje.IdVehiculo.HasValue && ddlVehiculo.Items.FindByValue(viaje.IdVehiculo.ToString()) != null)
                ddlVehiculo.SelectedValue = viaje.IdVehiculo.ToString();
            var avisos = new List<string>();
            if (avisoConductores != null) avisos.Add("Conductores. " + avisoConductores);
            if (avisoVehiculos != null) avisos.Add("Vehículos. " + avisoVehiculos);
            litAvisoAsignar.Text = HttpUtility.HtmlEncode(string.Join(" ", avisos));
        }

        protected void btnAsignar_Click(object sender, EventArgs e)
        {
            if (!EsFlota || !Permitir("Asignar viajes internos", lblMensaje)) return;
            int idConductor, idVehiculo;
            int.TryParse(ddlConductor.SelectedValue, out idConductor);
            int.TryParse(ddlVehiculo.SelectedValue, out idVehiculo);
            Intentar(() =>
            {
                var error = negocio.AsignarViajeInterno(LeerId("IdViaje"), idConductor, idVehiculo, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, "Viaje asignado: ya puede salir.")) return;
                pnlAsignar.Visible = false;
                CargarViajes();
            }, lblMensaje);
        }
    }
}
