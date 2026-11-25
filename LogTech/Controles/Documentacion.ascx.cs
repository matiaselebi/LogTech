using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class Documentacion : ControlBase
    {
        private const string Permiso = "Gestionar documentación";

        // Lo llama el panel al entrar al módulo (CU-003-019). Desde la ficha de un vehículo (?vehiculo=) o la fila de un
        // conductor (?conductor=) se ven solo sus documentos, y «Subir documento» ya los tiene elegidos.
        public override void Abrir()
        {
            pnlFormulario.Visible = false;
            lblMensaje.Text = string.Empty;
            // Solo al llegar por el enlace: si después entra desde el menú (un postback a la misma dirección), ve todo.
            GuardarId("FiltroVehiculo", IsPostBack ? 0 : LeerQuery("vehiculo"));
            GuardarId("FiltroConductor", IsPostBack ? 0 : LeerQuery("conductor"));
            Intentar(() =>
            {
                CargarDocumentos();
                RegistrarConsulta("Consulta de mi documentación");
            }, lblMensaje);
        }

        private int LeerQuery(string clave)
        {
            int id;
            return int.TryParse(Request.QueryString[clave], out id) ? id : 0;
        }

        // El calendario solo deja elegir vencimientos válidos: de mañana a 10 años (las controla validarRango).
        protected void Page_Load(object sender, EventArgs e)
        {
            txtVencimiento.Attributes["min"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            txtVencimiento.Attributes["max"] = DateTime.Today.AddYears(10).ToString("yyyy-MM-dd");
        }

        // El dador no tiene camiones: no ve la lista de vehículos. Solo la flota tiene conductores.
        private bool TieneVehiculos { get { return Rol != "Dador de carga"; } }
        private bool TieneConductores { get { return Rol == "Administrador de Flota"; } }

        // Para el script de la pantalla: los tipos como lista de JavaScript.
        protected static string TiposJs(string[] tipos)
        {
            var textos = new List<string>();
            foreach (var tipo in tipos) textos.Add(HttpUtility.JavaScriptStringEncode(tipo, true));
            return "[" + string.Join(", ", textos) + "]";
        }

        private void CargarDocumentos()
        {
            var idVehiculo = LeerId("FiltroVehiculo");
            var idConductor = LeerId("FiltroConductor");
            var lista = new List<DocumentoBE>();
            string de = null;
            foreach (var documento in negocio.ListarMisDocumentos(IdUsuario))
            {
                if (idVehiculo > 0 && documento.IdVehiculo != idVehiculo) continue;
                if (idConductor > 0 && documento.IdConductor != idConductor) continue;
                lista.Add(documento);
                de = documento.DeQuien;
            }
            pnlFiltro.Visible = idVehiculo > 0 || idConductor > 0;
            litFiltro.Text = HttpUtility.HtmlEncode(de == null ? "Todavía no tiene documentos cargados." : "Documentos de " + de + ".");
            gvDocumentos.Columns[1].Visible = TieneVehiculos;
            gvDocumentos.Columns[1].HeaderText = TieneConductores ? "Vehículo o conductor" : "Vehículo";
            gvDocumentos.DataSource = lista;
            gvDocumentos.DataBind();
            string detalle;
            ucSemaforo.Color = negocio.ObtenerSemaforoDocumentacion(IdUsuario, Rol, out detalle);
            ucSemaforo.Detalle = detalle;
        }

        private void PrepararFormulario(bool nuevo)
        {
            Cargar(ddlTipo, negocio.TiposDeDocumento(Rol), nuevo ? "Elegí el tipo" : null);
            ddlTipo.Enabled = nuevo;
            pnlVehiculo.Visible = TieneVehiculos;
            ddlVehiculo.Items.Clear();
            ddlVehiculo.Items.Add(new ListItem("No es de un vehículo", string.Empty));
            if (TieneVehiculos)
            {
                foreach (var vehiculo in negocio.ListarVehiculos(IdUsuario, string.Empty, "Activo"))
                    ddlVehiculo.Items.Add(new ListItem(vehiculo.Patente + " · " + vehiculo.Marca + " " + vehiculo.Modelo, vehiculo.IdVehiculo.ToString()));
            }
            pnlConductor.Visible = TieneConductores;
            ddlConductor.Items.Clear();
            ddlConductor.Items.Add(new ListItem("No es de un conductor", string.Empty));
            if (TieneConductores)
            {
                foreach (var conductor in negocio.ListarConductores(IdUsuario, string.Empty, "Activo"))
                    ddlConductor.Items.Add(new ListItem(conductor.NombreCompleto, conductor.IdConductor.ToString()));
            }
            litAyudaArchivo.Text = nuevo ? "PDF o JPG, hasta 5 MB." : "PDF o JPG, hasta 5 MB. Si no elegís un archivo nuevo se mantiene el actual.";
            pnlFormulario.Visible = true;
        }

        // Si la pantalla está mostrando un vehículo o un conductor, el documento nuevo ya va con él.
        private static void Elegir(DropDownList lista, int? id)
        {
            if (id.HasValue && id > 0 && lista.Items.FindByValue(id.ToString()) != null) lista.SelectedValue = id.ToString();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                GuardarId("IdDocumento", 0);
                litTituloFormulario.Text = "Nuevo documento";
                txtVencimiento.Text = string.Empty;
                PrepararFormulario(true);
                Elegir(ddlVehiculo, LeerId("FiltroVehiculo"));
                Elegir(ddlConductor, LeerId("FiltroConductor"));
            }, lblMensaje);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlFormulario.Visible = false;
        }

        // CU-003-016 y CU-003-017.
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            int idVehiculo, idConductor;
            var documento = new DocumentoBE
            {
                IdDocumento = LeerId("IdDocumento"),
                Tipo = ddlTipo.SelectedValue,
                IdVehiculo = int.TryParse(ddlVehiculo.SelectedValue, out idVehiculo) ? idVehiculo : (int?)null,
                IdConductor = int.TryParse(ddlConductor.SelectedValue, out idConductor) ? idConductor : (int?)null,
                FechaVencimiento = LeerFecha(txtVencimiento.Text),
                Contenido = LeerArchivo(fuArchivo),
                NombreArchivo = fuArchivo.FileName
            };
            // Una lista deshabilitada en la pantalla no se envía y queda lo que tenía: solo cuenta si el tipo es de un vehículo o
            // de un conductor (la capa de negocio lo vuelve a controlar).
            if (Array.IndexOf(negocio.DocumentosDelVehiculo(), documento.Tipo) < 0) documento.IdVehiculo = null;
            if (Array.IndexOf(negocio.DocumentosDelConductor(), documento.Tipo) < 0) documento.IdConductor = null;
            Intentar(() =>
            {
                var nuevo = documento.IdDocumento == 0;
                var error = nuevo ? negocio.AltaDocumento(documento, IdUsuario, Usuario) : negocio.ModificarDocumento(documento, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, "Documento guardado. Queda pendiente de revisión por LogTech.")) return;
                pnlFormulario.Visible = false;
                CargarDocumentos();
            }, lblMensaje);
        }

        protected void gvDocumentos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var id = Argumento(e);
            if (id == 0) return;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (e.CommandName == "Modificar")
                {
                    DocumentoBE documento = null;
                    foreach (var actual in negocio.ListarMisDocumentos(IdUsuario))
                    {
                        if (actual.IdDocumento == id) documento = actual;
                    }
                    if (documento == null) { Mostrar(lblMensaje, "El documento no existe.", true); return; }
                    GuardarId("IdDocumento", id);
                    litTituloFormulario.Text = "Modificar: " + documento.Tipo;
                    PrepararFormulario(false);
                    ddlTipo.SelectedValue = documento.Tipo;
                    Elegir(ddlVehiculo, documento.IdVehiculo);
                    Elegir(ddlConductor, documento.IdConductor);
                    txtVencimiento.Text = documento.FechaVencimiento.ToString("yyyy-MM-dd");
                }
                else if (e.CommandName == "Baja")
                {
                    // CU-003-018.
                    pnlFormulario.Visible = false;
                    Resultado(lblMensaje, negocio.BajaDocumento(id, IdUsuario, Usuario), "El documento fue dado de baja.");
                    CargarDocumentos();
                }
            }, lblMensaje);
        }
    }
}
