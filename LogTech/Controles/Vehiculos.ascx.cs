using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class Vehiculos : ControlBase
    {
        private const string Permiso = "Gestionar vehículos";

        // El transportista maneja un solo camión (lo ve en Mi perfil); la flota, varios.
        protected bool UnSoloCamion { get { return Rol == "Transportista"; } }

        // La flota: al dar de baja un vehículo, el mes que ya pagó por él no se devuelve.
        protected string ConfirmarBaja
        {
            get
            {
                return UnSoloCamion ? "return confirm('¿Dar de baja este vehículo? Ya no va a poder recibir viajes.');"
                    : "return confirm('¿Dar de baja este vehículo? Ya no va a poder recibir viajes. El mes que ya pagaste por él no se devuelve: desde la próxima renovación pagás un vehículo menos.');";
            }
        }

        // Los vencimientos se eligen de mañana a 10 años (los controla validarRango).
        protected void Page_Load(object sender, EventArgs e)
        {
            foreach (var fecha in new[] { txtVenceSeguro, txtVenceVtv, txtVenceCedula })
            {
                fecha.Attributes["min"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                fecha.Attributes["max"] = DateTime.Today.AddYears(10).ToString("yyyy-MM-dd");
            }
        }

        // Lo llama el panel al entrar al módulo (CU-002-010).
        public override void Abrir()
        {
            pnlFormulario.Visible = pnlDetalle.Visible = false;
            lblMensaje.Text = string.Empty;
            if (UnSoloCamion)
            {
                litEtiqueta.Text = "MI CAMIÓN";
                litTitulo.Text = "Mi camión";
                litBajada.Text = "Tu camión y su semáforo. Para cambiarlo, dalo de baja y cargá el nuevo con su documentación.";
                btnNuevo.Text = "Agregar camión";
            }
            Intentar(() =>
            {
                CargarVehiculos();
                RegistrarConsulta("Consulta de mis vehículos");
            }, lblMensaje);
        }

        private void CargarVehiculos()
        {
            if (UnSoloCamion)
            {
                txtBuscar.Text = string.Empty;
                ddlEstado.SelectedValue = "Activo";
            }
            var lista = negocio.ListarVehiculos(IdUsuario, txtBuscar.Text, ddlEstado.SelectedValue);
            rptVehiculos.DataSource = lista;
            rptVehiculos.DataBind();
            // ERR004: sin resultados.
            lblVacio.Visible = lista.Count == 0;
            lblVacio.Text = UnSoloCamion ? "Todavía no cargaste tu camión." : "No hay vehículos que coincidan con la búsqueda.";
            pnlFiltros.Visible = !UnSoloCamion;
            btnNuevo.Visible = !UnSoloCamion || lista.Count == 0;
        }

        // El texto o el estado del filtro cambiaron (AutoPostBack): se vuelve a cargar la lista.
        protected void Filtrar(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            pnlFormulario.Visible = pnlDetalle.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarVehiculos();
                RegistrarConsulta("Consulta de mis vehículos (búsqueda)");
            }, lblMensaje);
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                GuardarId("IdVehiculo", 0);
                litTituloFormulario.Text = "Nuevo vehículo";
                txtPatente.Text = txtMarca.Text = txtModelo.Text = txtCapacidad.Text = string.Empty;
                txtVenceSeguro.Text = txtVenceVtv.Text = txtVenceCedula.Text = string.Empty;
                Cargar(ddlAcoplado, negocio.TiposDeAcoplado(), "Elegí el tipo");
                pnlDocumentos.Visible = true;
                // El pop-up de confirmación: a la flota le dice cuánto se le cobra por el vehículo nuevo.
                btnGuardar.OnClientClick = "if (!Page_ClientValidate('Vehiculo') || !confirm('"
                    + HttpUtility.JavaScriptStringEncode(negocio.AvisoAltaVehiculo(IdUsuario, Rol)) + "')) return false;";
                pnlFormulario.Visible = true;
                pnlDetalle.Visible = false;
            }, lblMensaje);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlFormulario.Visible = false;
        }

        // CU-002-007 y CU-002-008.
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var vehiculo = new VehiculoBE
            {
                IdVehiculo = LeerId("IdVehiculo"),
                Patente = txtPatente.Text,
                Marca = txtMarca.Text,
                Modelo = txtModelo.Text,
                CapacidadToneladas = LeerDecimal(txtCapacidad.Text),
                TipoAcoplado = ddlAcoplado.SelectedValue
            };
            Intentar(() =>
            {
                var nuevo = vehiculo.IdVehiculo == 0;
                var error = nuevo ? negocio.AltaVehiculo(vehiculo, DocumentosNuevos(), IdUsuario, Usuario)
                    : negocio.ModificarVehiculo(vehiculo, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, nuevo ? "El vehículo quedó registrado. Su documentación queda en revisión: cuando LogTech la apruebe, se pone en verde y puede hacer viajes."
                    : "Los datos del vehículo fueron actualizados.")) return;
                pnlFormulario.Visible = false;
                CargarVehiculos();
            }, lblMensaje);
        }

        // Los tres documentos del formulario, con su vencimiento y su archivo.
        private List<DocumentoBE> DocumentosNuevos()
        {
            var tipos = negocio.DocumentosDelVehiculo();
            var archivos = new[] { fuSeguro, fuVtv, fuCedula };
            var vencimientos = new[] { txtVenceSeguro, txtVenceVtv, txtVenceCedula };
            var lista = new List<DocumentoBE>();
            for (var i = 0; i < tipos.Length; i++)
            {
                lista.Add(new DocumentoBE
                {
                    Tipo = tipos[i],
                    FechaVencimiento = LeerFecha(vencimientos[i].Text),
                    Contenido = LeerArchivo(archivos[i]),
                    NombreArchivo = archivos[i].FileName
                });
            }
            return lista;
        }

        protected void rptVehiculos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var id = Argumento(e);
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (e.CommandName == "Modificar")
                {
                    var vehiculo = negocio.ObtenerVehiculo(id, IdUsuario);
                    if (vehiculo == null) { Mostrar(lblMensaje, "El vehículo no existe.", true); return; }
                    GuardarId("IdVehiculo", id);
                    litTituloFormulario.Text = "Modificar el vehículo " + vehiculo.Patente;
                    txtPatente.Text = vehiculo.Patente;
                    txtMarca.Text = vehiculo.Marca;
                    txtModelo.Text = vehiculo.Modelo;
                    txtCapacidad.Text = vehiculo.CapacidadToneladas.ToString("0.##", CultureInfo.InvariantCulture);
                    Cargar(ddlAcoplado, negocio.TiposDeAcoplado(), "Elegí el tipo");
                    ddlAcoplado.SelectedValue = vehiculo.TipoAcoplado;
                    // La documentación se cambia en «Mi documentación».
                    pnlDocumentos.Visible = false;
                    btnGuardar.OnClientClick = string.Empty;
                    pnlFormulario.Visible = true;
                    pnlDetalle.Visible = false;
                }
                else if (e.CommandName == "Baja")
                {
                    // CU-002-009.
                    pnlFormulario.Visible = pnlDetalle.Visible = false;
                    Resultado(lblMensaje, negocio.BajaVehiculo(id, IdUsuario, Usuario), "El vehículo fue dado de baja.");
                    CargarVehiculos();
                }
                else if (e.CommandName == "Detalles") MostrarDetalle(id);
            }, lblMensaje);
        }

        // «Ver detalles»: lo que hizo el vehículo en los últimos 5 años, su documentación y su conductor designado.
        private void MostrarDetalle(int idVehiculo)
        {
            var vehiculo = negocio.ObtenerVehiculo(idVehiculo, IdUsuario);
            if (vehiculo == null) { Mostrar(lblMensaje, "El vehículo no existe.", true); return; }
            GuardarId("IdDetalle", idVehiculo);
            var reporte = negocio.ObtenerReporteFlota(IdUsuario, PeriodoEstadistica.Anual, idVehiculo);
            litDetallePatente.Text = HttpUtility.HtmlEncode(vehiculo.Patente);
            litDetalleVehiculo.Text = HttpUtility.HtmlEncode(vehiculo.Marca + " " + vehiculo.Modelo + " · " + vehiculo.CapacidadToneladas.ToString("0.##")
                + " t · " + vehiculo.TipoAcoplado + " · cargado el " + vehiculo.FechaAlta.ToString("dd/MM/yyyy"));
            litDetalleViajes.Text = reporte.Viajes.ToString("N0");
            litDetalleNeto.Text = reporte.Neto.ToString("C0");
            litDetalleKilometros.Text = reporte.Kilometros.ToString("N0");
            litDetalleToneladas.Text = reporte.Toneladas.ToString("N1");
            litDetalleIncidencias.Text = reporte.Incidencias.ToString("N0");
            litDetalleAhora.Text = vehiculo.Estado != "Activo" ? "Dado de baja" : vehiculo.EnViaje ? "En viaje" : "Disponible";
            litDetalleViajeActual.Text = HttpUtility.HtmlEncode(reporte.ViajeEnCurso ?? "Sin viaje asignado");

            // Último viaje y conductores que lo manejaron (los viajes del transportista no tienen conductor de flota).
            DateTime? ultimo = null;
            var nombres = new List<string>();
            foreach (var fila in reporte.Conductores)
            {
                if (fila.UltimoViaje.HasValue && (!ultimo.HasValue || fila.UltimoViaje > ultimo)) ultimo = fila.UltimoViaje;
                if ((fila.Viajes > 0 || fila.EnCurso > 0) && fila.Conductor != "Sin conductor asignado")
                    nombres.Add(fila.Conductor + " (" + fila.Viajes + " entregado(s))");
            }
            litDetalleUltimo.Text = ultimo.HasValue ? ultimo.Value.ToString("dd/MM/yyyy") : "-";
            pnlDetalleConductores.Visible = !UnSoloCamion;
            litDetalleConductores.Text = HttpUtility.HtmlEncode(nombres.Count == 0 ? "Todavía ninguno." : string.Join(" · ", nombres));

            var suyos = new List<DocumentoBE>();
            foreach (var documento in negocio.ListarMisDocumentos(IdUsuario))
                if (documento.IdVehiculo == idVehiculo) suyos.Add(documento);
            gvDetalleDocumentos.DataSource = suyos;
            gvDetalleDocumentos.DataBind();

            // Solo la flota tiene conductores para designar.
            pnlConductorDesignado.Visible = !UnSoloCamion && vehiculo.Estado == "Activo";
            if (pnlConductorDesignado.Visible)
            {
                ddlConductorDesignado.Items.Clear();
                ddlConductorDesignado.Items.Add(new ListItem("Sin conductor designado", string.Empty));
                foreach (var conductor in negocio.ListarConductores(IdUsuario, string.Empty, "Activo"))
                    ddlConductorDesignado.Items.Add(new ListItem(conductor.NombreCompleto, conductor.IdConductor.ToString()));
                if (vehiculo.IdConductorDesignado.HasValue && ddlConductorDesignado.Items.FindByValue(vehiculo.IdConductorDesignado.ToString()) != null)
                    ddlConductorDesignado.SelectedValue = vehiculo.IdConductorDesignado.ToString();
            }
            pnlDetalle.Visible = true;
            pnlFormulario.Visible = false;
        }

        protected void btnGuardarDesignado_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje) || UnSoloCamion) return;
            int idConductor;
            var conductor = int.TryParse(ddlConductorDesignado.SelectedValue, out idConductor) ? idConductor : (int?)null;
            var idVehiculo = LeerId("IdDetalle");
            Intentar(() =>
            {
                var error = negocio.DesignarConductor(idVehiculo, conductor, IdUsuario, Usuario);
                Resultado(lblMensaje, error, conductor.HasValue ? "Conductor designado: el camión solo sale con él." : "El camión quedó sin conductor designado.");
                CargarVehiculos();
                MostrarDetalle(idVehiculo);
            }, lblMensaje);
        }

        protected void btnCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalle.Visible = false;
        }
    }
}
