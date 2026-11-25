using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;
using LogTech.BE;
using LogTech.Controles;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech
{
    public partial class Panel : PaginaBase
    {
        private const int EventosPorPagina = 10;
        private readonly FachadaSeguridad fachada = new FachadaSeguridad();
        private readonly FachadaNegocio negocio = new FachadaNegocio();
        // Permisos del rol del usuario (Composite). Se leen en cada pedido: si el administrador
        // le quita un permiso al rol, la opción desaparece en el próximo clic.
        private List<string> permisos = new List<string>();
        private bool EsAdministrador { get { return GestorSesion.Instancia.Rol == "Administrador"; } }
        private bool EsContingencia { get { return GestorSesion.Instancia.Rol == UsuarioContingencia.RolContingencia; } }
        private bool EsTransportista { get { return GestorSesion.Instancia.Rol == "Transportista"; } }

        // El transportista tiene un solo camión: lo ve, con su documentación, abajo de Mi perfil y no en la barra.
        private bool VaEnMiPerfil(string seccion)
        {
            return EsTransportista && (seccion == "Vehiculos" || seccion == "Documentacion");
        }
        // Las secciones de administración y de mantenimiento también dependen de los permisos (los del rol más los
        // particulares del usuario), no del nombre del rol: si se le da «Realizar backup» a otro rol, ese rol ve
        // «Base de datos». El usuario de contingencia entra sin base, así que mantenimiento lo tiene siempre.
        private bool PuedeUsuarios { get { return Tiene("Administrar usuarios"); } }
        private bool PuedeRoles { get { return Tiene("Administrar roles") || Tiene("Administrar permisos") || Tiene("Administrar familias"); } }
        private bool PuedeBitacora { get { return Tiene("Consultar bitácora"); } }
        private bool PuedeBackup { get { return EsContingencia || Tiene("Realizar backup"); } }
        private bool PuedeRestaurar { get { return EsContingencia || Tiene("Restaurar base de datos"); } }
        private bool PuedeRecalcular { get { return EsContingencia || Tiene("Recuperar dígitos verificadores"); } }
        private bool PuedeMantenimiento { get { return PuedeBackup || PuedeRestaurar || PuedeRecalcular; } }
        private bool PuedeCambiarClave { get { return !EsAdministrador && !EsContingencia; } }
        private int PaginaBitacora
        {
            get { return ViewState["PaginaBitacora"] == null ? 0 : (int)ViewState["PaginaBitacora"]; }
            set { ViewState["PaginaBitacora"] = value; }
        }
        private int UsuarioSeleccionado
        {
            get { return ViewState["UsuarioSeleccionado"] == null ? 0 : (int)ViewState["UsuarioSeleccionado"]; }
            set { ViewState["UsuarioSeleccionado"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorSesion.Instancia.HaySesion)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            // La cuenta pudo quedar bloqueada o inactiva por un administrador mientras
            // la sesión seguía abierta: se corta el acceso en el próximo pedido.
            if (!EsContingencia)
            {
                try
                {
                    if (!fachada.CuentaActiva(GestorSesion.Instancia.IdUsuario))
                    {
                        GestorSesion.Instancia.Cerrar();
                        Response.Redirect("~/Login.aspx");
                        return;
                    }
                }
                catch (SqlException) { }
            }

            if (!EsContingencia)
            {
                try { permisos = fachada.ObtenerPermisosDeUsuario(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Rol); }
                catch (SqlException) { }
            }
            foreach (var modulo in Modulos.Values) modulo.Permisos = permisos;

            AplicarIdioma();
            MostrarFotoMenu();
            btnIrCambiarClave.Visible = PuedeCambiarClave;
            btnMenuUsuarios.Visible = PuedeUsuarios;
            btnMenuRoles.Visible = PuedeRoles;
            btnMenuBitacora.Visible = PuedeBitacora;
            pnlMenuAdmin.Visible = PuedeUsuarios || PuedeRoles || PuedeBitacora;
            pnlMenuMantenimiento.Visible = PuedeMantenimiento;
            secBackup.Visible = PuedeBackup;
            secRestaurar.Visible = PuedeRestaurar;
            btnRecalcular.Visible = PuedeRecalcular;
            MostrarOpcionesDeRoles();
            var hayOperacion = false;
            foreach (var boton in BotonesDelMenu)
            {
                // Se usa el permiso y no boton.Visible: el botón da false mientras su panel esté oculto.
                var puede = PuedeVer(boton.Key);
                boton.Value.Visible = puede && !VaEnMiPerfil(boton.Key);
                if (puede) hayOperacion = true;
            }
            pnlMenuOperacion.Visible = hayOperacion;
            pnlMenuVacio.Visible = !pnlMenuAdmin.Visible && !PuedeMantenimiento && !hayOperacion;
            if (!IsPostBack)
            {
                lblSinPermiso.Visible = Request.QueryString["sinpermiso"] == "1";
                MostrarFallaIntegridad();
                // La barra de arriba abre Panel.aspx?seccion=Perfil, Suscripcion o Seguros. MostrarSeccion controla el permiso.
                MostrarSeccion(Request.QueryString["seccion"] ?? "Inicio");
                return;
            }
            // Si el rol perdió el permiso del módulo abierto, se vuelve al inicio.
            foreach (var modulo in Modulos)
            {
                if (modulo.Value.Visible && !PuedeVer(modulo.Key)) MostrarSeccion("Inicio");
            }
        }

        // Segunda barrera de cada opción: si se llega a una opción sin el permiso (el botón no debería verse), no se hace
        // nada, se avisa «No tenés los permisos necesarios» y queda en la bitácora.
        private bool Permitir(bool puede, Action<string, bool> mostrar, string seccion)
        {
            if (puede) return true;
            MostrarSeccion(seccion, false);
            mostrar("No tenés los permisos necesarios para esta opción.", true);
            try { Registrar("Intento de usar una opción de " + seccion + " sin permiso", "Advertencia"); }
            catch (SqlException) { }
            return false;
        }

        // Eliminar un rol pide «Administrar roles»; eliminar una familia, «Administrar familias».
        private bool PuedeEliminarSeleccionado()
        {
            var seleccionado = fachada.ObtenerRoles().FirstOrDefault(x => x.IdRol.ToString() == ddlRoles.SelectedValue);
            return seleccionado != null && Tiene(seleccionado.EsFamilia ? "Administrar familias" : "Administrar roles");
        }

        // Roles y permisos: cada parte de la pantalla aparece solo con su permiso.
        private void MostrarOpcionesDeRoles()
        {
            var conRoles = Tiene("Administrar roles");
            var conPermisos = Tiene("Administrar permisos");
            var conFamilias = Tiene("Administrar familias");
            secNuevoRol.Visible = conRoles || conFamilias;
            // Qué se puede crear: la lista se arma de nuevo en cada pedido (si se quitara una opción, quedaría guardada en
            // el ViewState y no volvería aunque después le den el permiso). Se conserva lo que el usuario había elegido.
            var elegido = ddlTipoComponente.SelectedValue;
            ddlTipoComponente.Items.Clear();
            if (conRoles) ddlTipoComponente.Items.Add(new ListItem("Rol (perfil)", "Rol"));
            if (conFamilias) ddlTipoComponente.Items.Add(new ListItem("Familia de permisos", "Familia"));
            if (ddlTipoComponente.Items.FindByValue(elegido) != null) ddlTipoComponente.SelectedValue = elegido;
            lblPermisosRol.Visible = ddlPermisos.Visible = btnAgregarPermiso.Visible = conPermisos;
            lblFamiliasRol.Visible = ddlFamilias.Visible = btnAgregarFamilia.Visible = conFamilias;
            btnQuitarElemento.Visible = conPermisos || conFamilias;
            btnEliminarRol.Visible = conRoles || conFamilias;
        }

        protected void btnMenuInicio_Click(object sender, EventArgs e) { MostrarSeccion("Inicio"); }
        protected void btnIrCambiarClave_Click(object sender, EventArgs e) { MostrarSeccion("Cuenta"); }
        protected void btnVolverPerfil_Click(object sender, EventArgs e) { MostrarSeccion("Perfil"); }
        protected void btnMenuUsuarios_Click(object sender, EventArgs e) { MostrarSeccion("Usuarios"); }
        protected void btnMenuRoles_Click(object sender, EventArgs e) { MostrarSeccion("Roles"); }
        protected void btnMenuBitacora_Click(object sender, EventArgs e) { MostrarSeccion("Bitacora"); }
        protected void btnMenuBaseDatos_Click(object sender, EventArgs e) { MostrarSeccion("BaseDatos"); }

        protected void btnAgregarRol_Click(object sender, EventArgs e)
        {
            if (!Permitir(ddlTipoComponente.SelectedValue == "Familia" ? Tiene("Administrar familias") : Tiene("Administrar roles"), MostrarMensaje, "Roles")) return;
            Intentar(() =>
            {
                if (!fachada.AgregarRol(txtRol.Text, ddlTipoComponente.SelectedValue == "Familia"))
                {
                    MostrarMensaje("Ingresá un nombre nuevo de hasta 50 caracteres.", true);
                    return;
                }
                Registrar("Alta de " + ddlTipoComponente.SelectedValue + ": " + txtRol.Text.Trim());
                txtRol.Text = string.Empty;
                MostrarMensaje("Rol o familia creado.", false);
                CargarRoles();
            });
            MostrarSeccion("Roles", false);
        }

        protected void btnAgregarPermiso_Click(object sender, EventArgs e)
        {
            if (!Permitir(Tiene("Administrar permisos"), MostrarMensaje, "Roles")) return;
            Intentar(() =>
            {
                int idRol;
                if (!int.TryParse(ddlRoles.SelectedValue, out idRol) || !fachada.AgregarPermiso(idRol, ddlPermisos.SelectedValue))
                {
                    MostrarMensaje("Elegí un permiso que todavía no esté asignado.", true);
                    return;
                }
                Registrar("Permiso " + ddlPermisos.SelectedValue + " agregado a " + ddlRoles.SelectedItem.Text);
                MostrarMensaje("Permiso agregado.", false);
                CargarRoles();
            });
            MostrarSeccion("Roles", false);
        }

        protected void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            try { Registrar("Cierre de sesión"); }
            catch (SqlException) { }
            GestorSesion.Instancia.Cerrar();
            Response.Redirect("~/Login.aspx");
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            if (PaginaBitacora > 0) PaginaBitacora--;
            Intentar(CargarBitacora, MostrarMensajeBitacora);
            MostrarSeccion("Bitacora", false);
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            PaginaBitacora++;
            Intentar(CargarBitacora, MostrarMensajeBitacora);
            MostrarSeccion("Bitacora", false);
        }

        protected void btnFiltrarBitacora_Click(object sender, EventArgs e)
        {
            PaginaBitacora = 0;
            Intentar(CargarBitacora, MostrarMensajeBitacora);
            MostrarSeccion("Bitacora", false);
        }

        // HistorialAuditoria_Seguridad.xml: descarga los eventos que cumplen los filtros de la pantalla.
        protected void btnExportarBitacora_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeBitacora, MostrarMensajeBitacora, "Bitacora")) return;
            var filtro = FiltroActual();
            byte[] xml = null;
            if (!(filtro.Desde > filtro.Hasta))
                Intentar(() => xml = fachada.ExportarBitacoraXml(filtro, UsuarioBitacora, GestorSesion.Instancia.Usuario), MostrarMensajeBitacora);

            if (xml == null)
            {
                // Sin eventos, fechas al revés o sin conexión: se vuelve a mostrar la bitácora con el aviso.
                var mensaje = lblBitacora.Text;
                Intentar(CargarBitacora, MostrarMensajeBitacora);
                MostrarSeccion("Bitacora", false);
                if (lblBitacora.Text.Length == 0)
                    MostrarMensajeBitacora(mensaje.Length > 0 ? mensaje : "No hay eventos para exportar con los filtros elegidos.", true);
                return;
            }

            Response.Clear();
            Response.ContentType = "application/xml";
            Response.AppendHeader("Content-Disposition", "attachment; filename=HistorialAuditoria_Seguridad-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".xml");
            Response.BinaryWrite(xml);
            Response.End();
        }

        private void MostrarSeccion(string seccion, bool cargar = true)
        {
            pnlConfirmarRecalculo.Visible = false;
            if (VaEnMiPerfil(seccion)) seccion = "Perfil";
            var permitida = seccion == "Inicio"
                || (seccion == "Cuenta" && PuedeCambiarClave)
                || (seccion == "Perfil" && !EsContingencia)
                || (seccion == "BaseDatos" && PuedeMantenimiento)
                || (seccion == "Usuarios" && PuedeUsuarios)
                || (seccion == "Roles" && PuedeRoles)
                || (seccion == "Bitacora" && PuedeBitacora)
                || PuedeVer(seccion);
            lblSinPermiso.Visible = !permitida;
            if (!permitida)
            {
                // Segunda barrera: aunque el botón no se dibuje, cada sección vuelve a controlar el permiso en el servidor.
                try { Registrar("Intento de entrar a la sección " + seccion + " sin permiso", "Advertencia"); }
                catch (SqlException) { }
                seccion = "Inicio";
            }

            pnlInicio.Visible = seccion == "Inicio";
            pnlTablero.Visible = EsAdministrador;
            pnlCuenta.Visible = seccion == "Cuenta";
            pnlPerfil.Visible = seccion == "Perfil";
            pnlUsuarios.Visible = seccion == "Usuarios";
            pnlRoles.Visible = seccion == "Roles";
            pnlBitacora.Visible = seccion == "Bitacora";
            pnlBaseDatos.Visible = seccion == "BaseDatos";
            btnMenuInicio.CssClass = seccion == "Inicio" ? "active" : string.Empty;
            btnMenuUsuarios.CssClass = seccion == "Usuarios" ? "active" : string.Empty;
            btnMenuRoles.CssClass = seccion == "Roles" ? "active" : string.Empty;
            btnMenuBitacora.CssClass = seccion == "Bitacora" ? "active" : string.Empty;
            btnMenuBaseDatos.CssClass = seccion == "BaseDatos" ? "active" : string.Empty;
            pnlAvisos.Visible = seccion == "Inicio" && !EsAdministrador && !EsContingencia;
            foreach (var modulo in Modulos) modulo.Value.Visible = modulo.Key == seccion;
            foreach (var boton in BotonesDelMenu) boton.Value.CssClass = boton.Key == seccion ? "active" : string.Empty;
            var conCamion = seccion == "Perfil" && EsTransportista;
            if (conCamion) ucVehiculos.Visible = ucDocumentacion.Visible = true;

            if (!cargar) return;
            if (seccion == "Inicio" && EsAdministrador) Intentar(CargarTablero, MostrarMensajeTablero);
            if (seccion == "Roles") Intentar(CargarRoles);
            if (seccion == "Perfil") Intentar(CargarPerfil, MostrarMensajePerfil);
            if (conCamion) { ucVehiculos.Abrir(); ucDocumentacion.Abrir(); }
            // Al abrir la bitácora, los eventos de más de 30 días pasan de la tabla al historial XML.
            if (seccion == "Bitacora")
                Intentar(() => { fachada.ArchivarBitacora(UsuarioBitacora, GestorSesion.Instancia.Usuario); CargarBitacora(); }, MostrarMensajeBitacora);
            if (seccion == "Usuarios") Intentar(CargarUsuarios, MostrarMensajeUsuarios);
            if (seccion == "Inicio" && !EsAdministrador && !EsContingencia) Intentar(CargarAvisos, MostrarMensajeAvisos);
            if (Modulos.ContainsKey(seccion)) Modulos[seccion].Abrir();
        }

        private void CargarRoles()
        {
            var seleccionado = ddlRoles.SelectedValue;
            var roles = fachada.ObtenerRoles();
            rptRoles.DataSource = roles.Select(x => new
            {
                Nombre = x.Nombre + (x.EsFamilia ? " (familia)" : " (rol)"),
                Inicial = x.Nombre.Substring(0, 1).ToUpperInvariant(),
                PermisosTexto = x.ObtenerPermisos().Any() ? string.Join(" · ", x.ObtenerPermisos()) : "Sin permisos"
            });
            rptRoles.DataBind();
            ddlRoles.DataSource = roles;
            ddlRoles.DataTextField = "Nombre";
            ddlRoles.DataValueField = "IdRol";
            ddlRoles.DataBind();
            if (ddlRoles.Items.FindByValue(seleccionado) != null) ddlRoles.SelectedValue = seleccionado;
            ddlPermisos.DataSource = fachada.ObtenerPermisos();
            ddlPermisos.DataTextField = "Nombre";
            ddlPermisos.DataValueField = "Nombre";
            ddlPermisos.DataBind();
            ddlFamilias.DataSource = roles.Where(x => x.EsFamilia && x.IdRol.ToString() != ddlRoles.SelectedValue);
            ddlFamilias.DataTextField = "Nombre";
            ddlFamilias.DataValueField = "IdRol";
            ddlFamilias.DataBind();
            btnAgregarFamilia.Enabled = ddlFamilias.Items.Count > 0;
            tvPermisos.Nodes.Clear();
            var actual = roles.SingleOrDefault(x => x.IdRol.ToString() == ddlRoles.SelectedValue);
            if (actual != null) tvPermisos.Nodes.Add(CrearNodo(actual, true));
            tvPermisos.ExpandAll();
        }

        // seleccionable: en Roles y permisos se elige una rama para quitarla; en el perfil del usuario solo se mira.
        private TreeNode CrearNodo(ComponentePermisoBE componente, bool seleccionable)
        {
            var grupo = componente as RolBE;
            var nodo = new TreeNode(Server.HtmlEncode(componente.Nombre),
                grupo == null ? "P:" + componente.Nombre : "F:" + grupo.IdRol);
            if (!seleccionable) nodo.SelectAction = TreeNodeSelectAction.None;
            if (grupo != null)
                foreach (var hijo in grupo.Hijos) nodo.ChildNodes.Add(CrearNodo(hijo, seleccionable));
            return nodo;
        }

        protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Permitir(PuedeRoles, MostrarMensaje, "Roles")) return;
            Intentar(CargarRoles);
        }

        protected void btnAgregarFamilia_Click(object sender, EventArgs e)
        {
            if (!Permitir(Tiene("Administrar familias"), MostrarMensaje, "Roles")) return;
            Intentar(() =>
            {
                int padre, familia;
                if (!int.TryParse(ddlRoles.SelectedValue, out padre) || !int.TryParse(ddlFamilias.SelectedValue, out familia)) return;
                if (!fachada.AgregarFamilia(padre, familia))
                {
                    MostrarMensaje("No se agregó: revisá duplicados o ciclos entre familias.", true);
                    return;
                }
                Registrar("Familia " + ddlFamilias.SelectedItem.Text + " agregada a " + ddlRoles.SelectedItem.Text);
                CargarRoles();
                MostrarMensaje("Familia agregada con sus permisos.", false);
            });
        }

        protected void btnQuitarElemento_Click(object sender, EventArgs e)
        {
            if (!Permitir(tvPermisos.SelectedNode != null && tvPermisos.SelectedNode.Value.StartsWith("F:") ? Tiene("Administrar familias") : Tiene("Administrar permisos"), MostrarMensaje, "Roles")) return;
            Intentar(() =>
            {
                var nodo = tvPermisos.SelectedNode;
                if (nodo == null || nodo.Parent == null || nodo.Parent.Parent != null)
                {
                    MostrarMensaje("Seleccioná un hijo directo. Para editar una familia interna, elegila en la lista.", true);
                    return;
                }
                int padre;
                if (!int.TryParse(ddlRoles.SelectedValue, out padre)) return;
                // Los permisos que un rol base trae por defecto están protegidos.
                if (nodo.Value.StartsWith("P:") && fachada.EsPermisoPorDefecto(padre, nodo.Value.Substring(2)))
                {
                    MostrarMensaje("«" + nodo.Value.Substring(2) + "» es un permiso por defecto de " + ddlRoles.SelectedItem.Text
                        + ": no se puede quitar. Podés quitar los que se le agregaron.", true);
                    return;
                }
                var eliminado = nodo.Value.StartsWith("F:")
                    ? fachada.QuitarFamilia(padre, int.Parse(nodo.Value.Substring(2)))
                    : fachada.QuitarPermiso(padre, nodo.Value.Substring(2));
                if (eliminado) Registrar("Elemento quitado de " + ddlRoles.SelectedItem.Text);
                CargarRoles();
                MostrarMensaje(eliminado ? "Elemento quitado." : "El elemento ya no estaba asignado.", false);
            });
        }

        protected void btnEliminarRol_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeEliminarSeleccionado(), MostrarMensaje, "Roles")) return;
            Intentar(() =>
            {
                int idRol;
                if (!int.TryParse(ddlRoles.SelectedValue, out idRol))
                {
                    MostrarMensaje("Seleccioná un rol o una familia.", true);
                    return;
                }
                var nombre = ddlRoles.SelectedItem.Text;
                if (!fachada.EliminarRol(idRol))
                {
                    MostrarMensaje("No se puede eliminar: es un rol base, tiene usuarios asignados o ya no existe.", true);
                    return;
                }
                CargarRoles();
                MostrarMensaje("Se eliminó " + Server.HtmlEncode(nombre) + ". Sus permisos y familias internas se conservaron.", false);
                try { Registrar("Eliminación de rol o familia: " + nombre, "Crítica"); }
                catch (SqlException) { MostrarMensaje("El elemento fue eliminado, pero no se pudo registrar el evento en la bitácora.", true); }
            });
        }

        protected void btnGenerarBackup_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeBackup, MostrarMensajeMantenimiento, "BaseDatos")) return;
            MostrarSeccion("BaseDatos", false);
            try
            {
                var ruta = fachada.GenerarBackup(UsuarioBitacora, GestorSesion.Instancia.Usuario);
                Response.ContentType = "application/octet-stream";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(ruta));
                Response.TransmitFile(ruta);
                Response.End();
            }
            catch (SqlException) { MostrarMensajeMantenimiento("No se pudo generar el backup. Revisá el espacio en disco o los permisos de la carpeta.", true); }
            catch (IOException) { MostrarMensajeMantenimiento("No se pudo escribir el archivo de backup en el servidor.", true); }
        }

        protected void btnVerificarIntegridad_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeMantenimiento, MostrarMensajeMantenimiento, "BaseDatos")) return;
            MostrarSeccion("BaseDatos", false);
            Intentar(() =>
            {
                var problemas = fachada.VerificarIntegridad();
                gvIntegridad.DataSource = problemas;
                gvIntegridad.DataBind();
                gvIntegridad.Visible = problemas.Count > 0;
                MostrarMensajeMantenimiento(problemas.Count == 0
                    ? "La base de datos está íntegra."
                    : "Se detectaron " + problemas.Count + " problema(s) de integridad.", problemas.Count > 0);
            }, MostrarMensajeMantenimiento);
        }

        protected void btnRestaurar_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeRestaurar, MostrarMensajeMantenimiento, "BaseDatos")) return;
            MostrarSeccion("BaseDatos", false);
            if (!fuBackup.HasFile || !fuBackup.FileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                MostrarMensajeMantenimiento("Elegí un archivo .bak.", true);
                return;
            }
            try
            {
                if (fachada.RestaurarBackup(fuBackup.FileContent, UsuarioBitacora, GestorSesion.Instancia.Usuario))
                {
                    GestorSesion.Instancia.IntegridadComprometida = false;
                    pnlFallaIntegridad.Visible = false;
                    MostrarMensajeMantenimiento("La base de datos fue restaurada.", false);
                }
                else
                    MostrarMensajeMantenimiento("El archivo está dañado o no es un backup de LogTechDB.", true);
            }
            catch (SqlException) { MostrarMensajeMantenimiento("No se pudo restaurar. Verificá que SQL Server pueda leer la carpeta de backups.", true); }
            catch (IOException) { MostrarMensajeMantenimiento("No se pudo guardar el archivo subido.", true); }
        }

        // ERR049: apenas entra el administrador con la base dañada (o la contingencia) ve qué tablas y registros se
        // rompieron, con las opciones de recalcular los dígitos o restaurar un backup. La falla ya quedó en la bitácora
        // al iniciar sesión (la contingencia la anota acá, porque entra sin consultar la base).
        private void MostrarFallaIntegridad()
        {
            if (!(EsAdministrador && GestorSesion.Instancia.IntegridadComprometida) && !EsContingencia) return;
            IList<ProblemaIntegridadBE> problemas;
            try { problemas = fachada.VerificarIntegridad(); }
            catch (SqlException)
            {
                litFallaIntegridad.Text = "La base de datos LogTechDB no responde: restaurá un backup.";
                gvFallas.Visible = btnRecalcularFalla.Visible = false;
                pnlFallaIntegridad.Visible = true;
                return;
            }
            GestorSesion.Instancia.IntegridadComprometida = problemas.Count > 0;
            pnlFallaIntegridad.Visible = problemas.Count > 0;
            if (problemas.Count == 0) return;
            if (EsContingencia)
            {
                try { fachada.RegistrarFallasIntegridad(problemas, null, GestorSesion.Instancia.Usuario); }
                catch (SqlException) { }
            }
            litFallaIntegridad.Text = "Se detectaron " + problemas.Count + " problema(s) en los dígitos verificadores: alguien modificó la "
                + "base por afuera del sistema. Mientras tanto, los demás usuarios no pueden iniciar sesión.";
            gvFallas.DataSource = problemas;
            gvFallas.DataBind();
            btnRecalcularFalla.Visible = PuedeRecalcular;
            btnIrRestaurar.Visible = PuedeRestaurar;
        }

        // CU-010-066 Recuperación de DV: primero pide la contraseña (ERR046) y después acepta los datos actuales
        // y vuelve a firmar todas las tablas.
        protected void btnRecalcular_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeRecalcular, MostrarMensajeMantenimiento, "BaseDatos")) return;
            if (sender == btnRecalcular) MostrarSeccion("BaseDatos", false);
            lblRecalculo.Text = string.Empty;
            pnlConfirmarRecalculo.Visible = true;
            txtClaveRecalculo.Focus();
        }

        protected void btnConfirmarRecalculo_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeRecalcular, MostrarMensajeMantenimiento, "BaseDatos") || !Page.IsValid) return;
            try
            {
                if (!fachada.RecalcularDigitos(UsuarioBitacora, GestorSesion.Instancia.Usuario, txtClaveRecalculo.Text))
                {
                    lblRecalculo.Text = "La contraseña no es correcta: no se recalcularon los dígitos. El intento quedó registrado en la bitácora.";
                    pnlConfirmarRecalculo.Visible = true;
                    return;
                }
                GestorSesion.Instancia.IntegridadComprometida = false;
                pnlFallaIntegridad.Visible = false;
                gvIntegridad.Visible = false;
                MostrarSeccion("BaseDatos", false);
                MostrarMensajeMantenimiento("Listo: se recalcularon los dígitos verificadores y la base volvió a quedar íntegra.", false);
            }
            catch (SqlException)
            {
                MostrarSeccion("BaseDatos", false);
                MostrarMensajeMantenimiento("No se pudo recalcular: la base no responde. Restaurá un backup.", true);
            }
        }

        protected void btnCancelarRecalculo_Click(object sender, EventArgs e)
        {
            pnlConfirmarRecalculo.Visible = false;
        }

        protected void btnIrRestaurar_Click(object sender, EventArgs e)
        {
            MostrarSeccion("BaseDatos", false);
            MostrarMensajeMantenimiento("Elegí el archivo .bak y presioná «Restaurar».", false);
        }

        // La contingencia no tiene fila en Usuarios: se registra sin IdUsuario.
        private int? UsuarioBitacora { get { return EsContingencia ? (int?)null : GestorSesion.Instancia.IdUsuario; } }

        private void MostrarMensajeMantenimiento(string mensaje, bool esError)
        {
            lblMantenimiento.Text = mensaje;
            lblMantenimiento.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        // ---- Cambiar contraseña (roles distintos de Administrador) ----

        protected void btnCambiarClave_Click(object sender, EventArgs e)
        {
            if (!PuedeCambiarClave) return;
            MostrarSeccion("Cuenta", false);
            if (string.IsNullOrWhiteSpace(txtClaveActual.Text) || string.IsNullOrWhiteSpace(txtClaveNueva.Text)
                || string.IsNullOrWhiteSpace(txtClaveConfirmar.Text))
            {
                MostrarMensajeCuenta("Completá los tres campos.", true);
                return;
            }
            if (!fachada.ClaveCumplePolitica(txtClaveNueva.Text))
            {
                MostrarMensajeCuenta("La contraseña debe tener al menos 8 caracteres, una mayúscula y un carácter especial (por ejemplo, ! # $ o @).", true);
                return;
            }
            if (txtClaveNueva.Text != txtClaveConfirmar.Text)
            {
                MostrarMensajeCuenta("Las contraseñas nuevas no coinciden.", true);
                return;
            }
            Intentar(() =>
            {
                if (!fachada.CambiarClave(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario,
                    txtClaveActual.Text, txtClaveNueva.Text))
                {
                    MostrarMensajeCuenta("La contraseña actual no es correcta.", true);
                    return;
                }
                txtClaveActual.Text = txtClaveNueva.Text = txtClaveConfirmar.Text = string.Empty;
                MostrarMensajeCuenta("La contraseña fue actualizada.", false);
            });
        }

        // ---- Mi perfil (CU-009-053) y clave de API del dador (CU-009-054) ----

        private void CargarPerfil()
        {
            var u = fachada.ObtenerUsuario(GestorSesion.Instancia.IdUsuario);
            if (u == null) return;
            litPerfilUsuario.Text = Server.HtmlEncode(u.Usuario);
            litPerfilRol.Text = Server.HtmlEncode(u.Rol);
            txtPerfilNombre.Text = u.Nombre;
            txtPerfilApellido.Text = u.Apellido;
            txtPerfilCorreo.Text = u.Correo;
            txtPerfilTelefono.Text = u.Telefono;
            txtPerfilRazonSocial.Text = u.RazonSocial ?? string.Empty;
            ddlPerfilCondicionIva.SelectedValue = u.CondicionIva;
            txtPerfilLicencia.Text = u.NumeroLicencia ?? string.Empty;
            var foto = UrlFotoPerfil();
            imgFotoPerfil.Visible = foto != null;
            imgFotoPerfil.ImageUrl = foto;
            pnlPerfilRazonSocial.Visible = u.Rol == "Dador de carga";
            pnlPerfilLicencia.Visible = u.Rol == "Transportista";
        }

        protected void btnGuardarPerfil_Click(object sender, EventArgs e)
        {
            if (EsContingencia) return;
            MostrarSeccion("Perfil", false);
            Intentar(() =>
            {
                var datos = new UsuarioBE
                {
                    Nombre = txtPerfilNombre.Text,
                    Apellido = txtPerfilApellido.Text,
                    Correo = txtPerfilCorreo.Text,
                    Telefono = txtPerfilTelefono.Text,
                    RazonSocial = txtPerfilRazonSocial.Text,
                    CondicionIva = ddlPerfilCondicionIva.SelectedValue,
                    NumeroLicencia = txtPerfilLicencia.Text
                };
                var error = fachada.ModificarMiPerfil(datos, GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario);
                if (error != null) { MostrarMensajePerfil(error, true); return; }
                CargarPerfil();
                MostrarMensajePerfil("Datos actualizados.", false);
            }, MostrarMensajePerfil);
        }

        protected void btnCambiarFoto_Click(object sender, EventArgs e)
        {
            if (EsContingencia) return;
            MostrarSeccion("Perfil", false);
            Intentar(() =>
            {
                var error = fachada.CambiarFotoPerfil(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario,
                    fuFotoPerfil.HasFile ? fuFotoPerfil.FileBytes : null, fuFotoPerfil.FileName);
                if (error != null) { MostrarMensajePerfil(error, true); return; }
                CargarPerfil();
                MostrarFotoMenu();
                MostrarMensajePerfil("Foto actualizada.", false);
            }, MostrarMensajePerfil);
        }

        // La foto sale por Archivo.ashx (App_Data no se publica). "v" cambia con cada foto nueva para que el navegador no muestre la vieja.
        private string UrlFotoPerfil()
        {
            if (EsContingencia) return null;
            var version = fachada.VersionFotoPerfil(GestorSesion.Instancia.IdUsuario);
            return version == null ? null : "~/Archivo.ashx?tipo=foto&id=" + GestorSesion.Instancia.IdUsuario + "&v=" + version;
        }

        private void MostrarFotoMenu()
        {
            var foto = UrlFotoPerfil();
            imgFotoMenu.Visible = foto != null;
            imgFotoMenu.ImageUrl = foto;
            spnLogoMenu.Visible = foto == null;
        }

        private void MostrarMensajePerfil(string mensaje, bool esError)
        {
            lblPerfil.Text = mensaje;
            lblPerfil.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        private void MostrarMensajeCuenta(string mensaje, bool esError)
        {
            lblCuenta.Text = mensaje;
            lblCuenta.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        // ---- Gestión de usuarios (Administrador) ----

        // El texto o el estado del filtro cambiaron (AutoPostBack): se vuelve a cargar la grilla.
        protected void FiltrarUsuarios(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios")) return;
            pnlEditarUsuario.Visible = false;
            Intentar(CargarUsuarios, MostrarMensajeUsuarios);
            MostrarSeccion("Usuarios", false);
        }

        private void CargarUsuarios()
        {
            var filtro = new FiltroUsuario { Texto = txtUsuarioFiltro.Text, Estado = ddlEstadoFiltro.SelectedValue };
            gvUsuarios.DataSource = fachada.ListarUsuarios(filtro);
            gvUsuarios.DataBind();
        }

        // Los enlaces de la grilla aparecen solo cuando la acción aplica (las mismas reglas que
        // GestionUsuariosBLL): no se ofrece dar de baja ni bloquear la propia cuenta.
        protected bool EsMiCuenta(object idUsuario)
        {
            return (int)idUsuario == GestorSesion.Instancia.IdUsuario;
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios")) return;
            int idUsuario;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idUsuario)) return;
            MostrarSeccion("Usuarios", false);

            pnlPerfilUsuario.Visible = false;
            if (e.CommandName == "Perfil")
            {
                Intentar(() => MostrarPerfilUsuario(idUsuario), MostrarMensajeUsuarios);
                return;
            }
            if (e.CommandName == "Editar")
            {
                Intentar(() => AbrirEdicion(idUsuario), MostrarMensajeUsuarios);
                return;
            }

            Intentar(() =>
            {
                string error;
                if (e.CommandName == "Baja")
                    error = fachada.DarDeBajaUsuario(idUsuario, GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario);
                else if (e.CommandName == "Bloquear")
                    error = fachada.BloquearUsuario(idUsuario, GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario);
                else if (e.CommandName == "Desbloquear")
                    error = fachada.DesbloquearUsuario(idUsuario, GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario);
                else
                    return;

                pnlEditarUsuario.Visible = false;
                CargarUsuarios();
                MostrarMensajeUsuarios(error ?? "Listo.", error != null);
            }, MostrarMensajeUsuarios);
        }

        private void AbrirEdicion(int idUsuario)
        {
            var u = fachada.ObtenerUsuario(idUsuario);
            if (u == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
            UsuarioSeleccionado = idUsuario;
            litUsuarioEditado.Text = Server.HtmlEncode(u.Usuario);
            txtEditNombre.Text = u.Nombre;
            txtEditApellido.Text = u.Apellido;
            txtEditCorreo.Text = u.Correo;
            txtEditTelefono.Text = u.Telefono;
            txtEditRazonSocial.Text = u.RazonSocial ?? string.Empty;
            txtEditLicencia.Text = u.NumeroLicencia ?? string.Empty;
            pnlEditarUsuario.Visible = true;
            lblUsuarios.Text = string.Empty;
            CargarPermisosUsuario(u);
            ddlPermisoUsuario.DataSource = fachada.ObtenerPermisos();
            ddlPermisoUsuario.DataTextField = "Nombre";
            ddlPermisoUsuario.DataValueField = "Nombre";
            ddlPermisoUsuario.DataBind();
            ddlFamiliaUsuario.DataSource = fachada.ObtenerRoles().Where(x => x.EsFamilia);
            ddlFamiliaUsuario.DataTextField = "Nombre";
            ddlFamiliaUsuario.DataValueField = "IdRol";
            ddlFamiliaUsuario.DataBind();
            btnAgregarFamiliaUsuario.Enabled = ddlFamiliaUsuario.Items.Count > 0;
        }

        // Árbol de permisos del usuario en «Editar» (patrón Composite): el usuario es la raíz; adentro está su rol con
        // sus familias y, colgando directo del usuario, las familias y los permisos que se le dieron a él. Solo esos se
        // pueden quitar desde acá; lo que está adentro del rol se cambia en «Roles y permisos».
        private void CargarPermisosUsuario(UsuarioBE u)
        {
            litPermisosDe.Text = Server.HtmlEncode(u.Usuario);
            tvPermisosUsuario.Nodes.Clear();
            var raiz = CrearNodo(fachada.ObtenerPerfilDeUsuario(u.IdUsuario, u.Usuario, u.Rol), true);
            raiz.SelectAction = TreeNodeSelectAction.None;
            if (raiz.ChildNodes.Count <= 1)
                raiz.ChildNodes.Add(new TreeNode("(sin permisos ni familias propias)", string.Empty) { SelectAction = TreeNodeSelectAction.None });
            tvPermisosUsuario.Nodes.Add(raiz);
            tvPermisosUsuario.ExpandAll();
        }

        protected void btnAgregarPermisoUsuario_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var u = fachada.ObtenerUsuario(UsuarioSeleccionado);
                if (u == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
                var permiso = ddlPermisoUsuario.SelectedValue;
                if (fachada.ObtenerPermisosDeUsuario(u.IdUsuario, u.Rol).Contains(permiso))
                {
                    MostrarMensajeUsuarios("El usuario ya tiene ese permiso (por su rol o como particular).", true);
                    return;
                }
                if (!fachada.AgregarPermisoUsuario(u.IdUsuario, permiso))
                {
                    MostrarMensajeUsuarios("Elegí un permiso del catálogo.", true);
                    return;
                }
                Registrar("Permiso particular " + permiso + " agregado a " + u.Usuario);
                CargarPermisosUsuario(u);
                MostrarMensajeUsuarios("Permiso agregado: el usuario lo tiene desde su próximo clic.", false);
            }, MostrarMensajeUsuarios);
        }

        protected void btnAgregarFamiliaUsuario_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var u = fachada.ObtenerUsuario(UsuarioSeleccionado);
                if (u == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
                int idFamilia;
                if (!int.TryParse(ddlFamiliaUsuario.SelectedValue, out idFamilia) || !fachada.AgregarFamiliaUsuario(u.IdUsuario, idFamilia))
                {
                    MostrarMensajeUsuarios("El usuario ya tiene esa familia, o no es una familia.", true);
                    return;
                }
                Registrar("Familia " + ddlFamiliaUsuario.SelectedItem.Text + " agregada a " + u.Usuario);
                CargarPermisosUsuario(u);
                MostrarMensajeUsuarios("Familia agregada: el usuario tiene sus permisos desde su próximo clic.", false);
            }, MostrarMensajeUsuarios);
        }

        protected void btnEliminarPermisoUsuario_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var u = fachada.ObtenerUsuario(UsuarioSeleccionado);
                if (u == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
                // Lo del usuario cuelga directo de él (permisos «P:» y familias «F:»); su rol también, pero no se quita acá.
                var nodo = tvPermisosUsuario.SelectedNode;
                var delUsuario = nodo != null && nodo.Parent != null && nodo.Parent.Parent == null && nodo.Text != Server.HtmlEncode(u.Rol)
                    && (nodo.Value.StartsWith("P:") || nodo.Value.StartsWith("F:"));
                if (!delUsuario)
                {
                    MostrarMensajeUsuarios(nodo != null && nodo.Parent != null
                        ? "Eso viene del rol " + u.Rol + ": para quitárselo a todos los de ese rol, usá «Roles y permisos»."
                        : "Seleccioná en el árbol un permiso o una familia del usuario.", true);
                    return;
                }
                var esFamilia = nodo.Value.StartsWith("F:");
                var quitado = esFamilia ? fachada.QuitarFamiliaUsuario(u.IdUsuario, int.Parse(nodo.Value.Substring(2)))
                    : fachada.QuitarPermisoUsuario(u.IdUsuario, nodo.Value.Substring(2));
                if (quitado) Registrar((esFamilia ? "Familia " : "Permiso particular ") + nodo.Text + " quitado a " + u.Usuario);
                CargarPermisosUsuario(u);
                MostrarMensajeUsuarios(quitado ? "Quitado: el usuario deja de tenerlo en su próximo clic."
                    : "El usuario ya no lo tenía.", !quitado);
            }, MostrarMensajeUsuarios);
        }

        // El perfil del usuario: su rol o familia con las familias y permisos que contiene (patrón Composite).
        private void MostrarPerfilUsuario(int idUsuario)
        {
            var u = fachada.ObtenerUsuario(idUsuario);
            if (u == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
            RolBE perfil = null;
            foreach (var rol in fachada.ObtenerRoles())
                if (rol.Nombre == u.Rol) perfil = rol;

            pnlEditarUsuario.Visible = false;
            pnlPerfilUsuario.Visible = true;
            lblUsuarios.Text = string.Empty;
            UsuarioSeleccionado = idUsuario;
            pnlClaveApiNueva.Visible = false;
            pnlClaveApiUsuario.Visible = u.Rol == "Dador de carga";
            if (pnlClaveApiUsuario.Visible) MostrarClaveApi(idUsuario);
            litPerfilDe.Text = Server.HtmlEncode(u.Usuario);
            litPerfilNombre.Text = Server.HtmlEncode(u.Rol);
            var perfilUsuario = fachada.ObtenerPerfilDeUsuario(idUsuario, u.Usuario, u.Rol);
            // Lo que se le dio a él: los hijos directos del compuesto, menos su rol.
            var particulares = perfilUsuario.Hijos.Where(x => x.Nombre != u.Rol).Select(x => x is RolBE ? x.Nombre + " (familia)" : x.Nombre).ToList();
            litPermisosParticulares.Text = particulares.Count == 0
                ? "no tiene. Se agregan desde «Editar»."
                : Server.HtmlEncode(string.Join(" · ", particulares)) + " (además de los de su perfil).";
            tvPerfilUsuario.Nodes.Clear();
            if (perfil == null) { litPerfilResumen.Text = "sin permisos"; return; }
            litPerfilResumen.Text = (perfil.EsFamilia ? "familia" : "rol") + " con " + ContarFamilias(perfil) + " familia(s) y "
                + perfil.ObtenerPermisos().Count + " permiso(s) en total";
            tvPerfilUsuario.Nodes.Add(CrearNodo(perfilUsuario, false));
            tvPerfilUsuario.ExpandAll();
            Registrar("Consulta del perfil del usuario " + u.Usuario);
        }

        // Familias que tiene adentro, en cualquier nivel.
        private static int ContarFamilias(RolBE rol)
        {
            var total = 0;
            foreach (var hijo in rol.Hijos)
            {
                var familia = hijo as RolBE;
                if (familia != null) total += 1 + ContarFamilias(familia);
            }
            return total;
        }

        // Estado de la clave de API del dador: nunca se muestra entera, solo los últimos 4.
        private void MostrarClaveApi(int idDador)
        {
            var clave = negocio.ObtenerClaveApi(idDador);
            btnRevocarClaveApi.Visible = clave != null;
            litClaveApiEstado.Text = clave == null ? "No tiene una clave activa."
                : "Clave activa terminada en <strong>" + Server.HtmlEncode(clave.Ultimos4) + "</strong>, creada el "
                    + clave.FechaAlta.ToString("dd/MM/yyyy") + ". " + (clave.FechaUltimoUso.HasValue
                        ? "Último uso: " + clave.FechaUltimoUso.Value.ToString("dd/MM/yyyy HH:mm") + "." : "Todavía no se usó.");
        }

        protected void btnGenerarClaveApi_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var clave = negocio.GenerarClaveApi(UsuarioSeleccionado, GestorSesion.Instancia.Usuario);
                if (clave == null) { MostrarMensajeUsuarios("Solo los dadores de carga tienen clave de API.", true); return; }
                MostrarClaveApi(UsuarioSeleccionado);
                txtClaveApiNueva.Text = clave;
                pnlClaveApiNueva.Visible = true;
                MostrarMensajeUsuarios("Clave generada. Pasásela a la empresa del dador: no se vuelve a mostrar.", false);
            }, MostrarMensajeUsuarios);
        }

        protected void btnRevocarClaveApi_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var revocada = negocio.RevocarClaveApi(UsuarioSeleccionado, GestorSesion.Instancia.Usuario);
                pnlClaveApiNueva.Visible = false;
                MostrarClaveApi(UsuarioSeleccionado);
                MostrarMensajeUsuarios(revocada ? "Clave revocada: el sistema del dador ya no puede consultar sus viajes." : "No tiene una clave activa.", !revocada);
            }, MostrarMensajeUsuarios);
        }

        protected void btnCerrarPerfilUsuario_Click(object sender, EventArgs e)
        {
            pnlPerfilUsuario.Visible = false;
            MostrarSeccion("Usuarios", false);
        }

        protected void btnGuardarUsuario_Click(object sender, EventArgs e)
        {
            if (!Permitir(PuedeUsuarios, MostrarMensajeUsuarios, "Usuarios") || UsuarioSeleccionado == 0) return;
            MostrarSeccion("Usuarios", false);
            Intentar(() =>
            {
                var actual = fachada.ObtenerUsuario(UsuarioSeleccionado);
                if (actual == null) { MostrarMensajeUsuarios("El usuario ya no existe.", true); return; }
                actual.Nombre = txtEditNombre.Text;
                actual.Apellido = txtEditApellido.Text;
                actual.Correo = txtEditCorreo.Text;
                actual.Telefono = txtEditTelefono.Text;
                actual.RazonSocial = string.IsNullOrWhiteSpace(txtEditRazonSocial.Text) ? null : txtEditRazonSocial.Text.Trim();
                actual.NumeroLicencia = string.IsNullOrWhiteSpace(txtEditLicencia.Text) ? null : txtEditLicencia.Text.Trim();

                var error = fachada.ModificarUsuario(actual, GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario);
                if (error != null) { MostrarMensajeUsuarios(error, true); return; }
                pnlEditarUsuario.Visible = false;
                CargarUsuarios();
                MostrarMensajeUsuarios("Datos actualizados.", false);
            }, MostrarMensajeUsuarios);
        }

        protected void btnCancelarEdicion_Click(object sender, EventArgs e)
        {
            pnlEditarUsuario.Visible = false;
            MostrarSeccion("Usuarios", false);
        }

        private void MostrarMensajeUsuarios(string mensaje, bool esError)
        {
            lblUsuarios.Text = mensaje;
            lblUsuarios.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        private FiltroBitacora FiltroActual()
        {
            DateTime fecha;
            return new FiltroBitacora
            {
                Desde = DateTime.TryParse(txtBitacoraDesde.Text, out fecha) ? fecha : (DateTime?)null,
                Hasta = DateTime.TryParse(txtBitacoraHasta.Text, out fecha) ? fecha : (DateTime?)null,
                Usuario = txtBitacoraUsuario.Text,
                Criticidad = ddlBitacoraCriticidad.SelectedValue,
                Historial = ddlBitacoraOrigen.SelectedValue == "Historial"
            };
        }

        private void CargarBitacora()
        {
            lblBitacora.Text = string.Empty;
            int total;
            var filtro = FiltroActual();
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                MostrarMensajeBitacora("La fecha \"Desde\" no puede ser posterior a la fecha \"Hasta\".", true);
                return;
            }
            var eventos = fachada.ObtenerBitacora(PaginaBitacora, EventosPorPagina, filtro, out total);
            var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)EventosPorPagina));
            if (PaginaBitacora >= paginas)
            {
                PaginaBitacora = paginas - 1;
                eventos = fachada.ObtenerBitacora(PaginaBitacora, EventosPorPagina, filtro, out total);
            }
            gvBitacora.DataSource = eventos;
            gvBitacora.DataBind();
            litPagina.Text = "Página " + (PaginaBitacora + 1) + " de " + paginas;
            btnAnterior.Enabled = PaginaBitacora > 0;
            btnSiguiente.Enabled = PaginaBitacora + 1 < paginas;
        }

        private void MostrarMensajeBitacora(string mensaje, bool esError)
        {
            lblBitacora.Text = mensaje;
            lblBitacora.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        // ---- Operación logística (Parte 1): menú por permisos y módulos .ascx ----

        // Sección del menú -> control del módulo.
        private Dictionary<string, ControlBase> Modulos
        {
            get
            {
                return new Dictionary<string, ControlBase>
                {
                    { "Cargas", ucMisCargas }, { "Bolsa", ucBolsaCargas }, { "Viajes", ucMisViajes },
                    { "Vehiculos", ucVehiculos }, { "Conductores", ucConductores }, { "Documentacion", ucDocumentacion },
                    { "Validar", ucValidarDocumentacion }, { "Remitos", ucRemitos },
                    { "ReporteFlota", ucReporteFlota }, { "Billetera", ucBilletera }, { "Disputas", ucDisputas },
                    { "ViajesAdmin", ucViajesAdmin }, { "Seguros", ucSeguros },
                    { "Facturas", ucFacturas }, { "Suscripcion", ucSuscripcion }
                };
            }
        }

        private Dictionary<string, LinkButton> BotonesDelMenu
        {
            get
            {
                return new Dictionary<string, LinkButton>
                {
                    { "Cargas", btnMenuCargas }, { "Bolsa", btnMenuBolsa }, { "Viajes", btnMenuViajes },
                    { "Vehiculos", btnMenuVehiculos }, { "Conductores", btnMenuConductores }, { "Documentacion", btnMenuDocumentacion },
                    { "Validar", btnMenuValidar }, { "Remitos", btnMenuRemitos },
                    { "ReporteFlota", btnMenuReporteFlota }, { "Billetera", btnMenuBilletera }, { "Disputas", btnMenuDisputas },
                    { "ViajesAdmin", btnMenuViajesAdmin }, { "Seguros", btnMenuSeguros },
                    { "Facturas", btnMenuFacturas }, { "Suscripcion", btnMenuSuscripcion }
                };
            }
        }

        private bool Tiene(string permiso)
        {
            return permisos.Contains(permiso);
        }

        // Permisos que habilitan cada módulo: la pantalla aparece con cualquiera de los suyos y, adentro, cada opción
        // aparece solo con su permiso. Los permisos que se usan en varias pantallas (ver el remito, monitorear, el
        // match, calificar) son opciones dentro de ellas, no abren una pantalla por sí solos.
        private bool PuedeVer(string seccion)
        {
            switch (seccion)
            {
                case "Cargas": return Tiene("Publicar ofertas de carga") || Tiene("Gestionar ofertas de carga");
                case "Bolsa": return Tiene("Aceptar ofertas de carga");
                case "Viajes": return Tiene("Actualizar estado de viaje") || Tiene("Reportar incidencias de viaje") || Tiene("Asignar viajes internos");
                case "Vehiculos": return Tiene("Gestionar vehículos");
                case "Conductores": return Tiene("Gestionar transportistas");
                case "Documentacion": return Tiene("Gestionar documentación");
                case "Validar": return Tiene("Validar documentación");
                case "Remitos": return Tiene("Gestionar remitos digitales");
                // El reporte es de la flota propia: el administrador del sistema tiene el permiso para su tablero,
                // pero no maneja conductores.
                case "ReporteFlota": return Tiene("Generar reportes gerenciales") && Tiene("Gestionar transportistas");
                case "Billetera": return Tiene("Consultar estado de pago") || Tiene("Gestionar factoring");
                case "Disputas": return Tiene("Gestionar disputas comerciales") || Tiene("Resolver disputas comerciales");
                case "ViajesAdmin": return Tiene("Supervisar viajes");
                case "Seguros": return Tiene("Gestionar pólizas de seguro");
                case "Facturas": return Tiene("Consultar facturas") || Tiene("Generar facturas corporativas");
                case "Suscripcion": return Tiene("Gestionar suscripción SaaS") || Tiene("Gestionar suscripción premium");
                default: return false;
            }
        }

        protected void btnMenuModulo_Click(object sender, EventArgs e)
        {
            MostrarSeccion(((LinkButton)sender).CommandArgument);
        }

        // Avisos que dejaron los observadores del viaje, el analista y la rutina de vencimientos.
        private void CargarAvisos()
        {
            lblAvisos.Text = string.Empty;
            var avisos = negocio.ListarNotificaciones(GestorSesion.Instancia.IdUsuario);
            rptAvisos.DataSource = avisos;
            rptAvisos.DataBind();
            var sinLeer = false;
            foreach (var aviso in avisos)
            {
                if (!aviso.Leida) sinLeer = true;
            }
            rptAvisos.Visible = avisos.Count > 0;
            lblSinAvisos.Visible = avisos.Count == 0;
            btnMarcarLeidas.Visible = sinLeer;

            var conDocumentacion = Tiene("Gestionar documentación");
            ucSemaforoInicio.Visible = conDocumentacion;
            if (!conDocumentacion) return;
            string detalle;
            ucSemaforoInicio.Color = negocio.ObtenerSemaforoDocumentacion(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Rol, out detalle);
            ucSemaforoInicio.Detalle = detalle;
        }

        protected void btnMarcarLeidas_Click(object sender, EventArgs e)
        {
            Intentar(() =>
            {
                negocio.MarcarNotificacionesLeidas(GestorSesion.Instancia.IdUsuario);
                CargarAvisos();
            }, MostrarMensajeAvisos);
        }

        private void MostrarMensajeAvisos(string mensaje, bool esError)
        {
            lblAvisos.Text = mensaje;
            lblAvisos.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        // ---- Tablero (pantalla de inicio del Administrador) ----

        // Los tres desplegables del tablero (período, tipo de suscripción y tipo de carga)
        // vuelven a cargar todo el tablero.
        protected void ddlTablero_SelectedIndexChanged(object sender, EventArgs e)
        {
            MostrarSeccion("Inicio");
        }

        // CU-008-044 Generar Reporte Gerencial: descarga el tablero con los filtros elegidos.
        protected void btnExportarReporte_Click(object sender, EventArgs e)
        {
            if (!EsAdministrador) return;
            var periodo = (PeriodoEstadistica)Enum.Parse(typeof(PeriodoEstadistica), ddlPeriodo.SelectedValue);
            var carga = (TipoCarga)Enum.Parse(typeof(TipoCarga), ddlTipoCarga.SelectedValue);
            string csv = null;
            Intentar(() => csv = negocio.ExportarReporteGerencial(periodo, ddlTipoSuscripcion.SelectedValue, carga,
                GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario), MostrarMensajeTablero);

            if (csv == null)
            {
                // Sin datos o sin conexión: se vuelve a mostrar el tablero con el aviso.
                var mensaje = lblTablero.Text;
                MostrarSeccion("Inicio");
                MostrarMensajeTablero(mensaje.Length > 0 ? mensaje : "No hay información para exportar con los filtros elegidos.", true);
                return;
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.ContentEncoding = Encoding.UTF8;
            Response.AppendHeader("Content-Disposition", "attachment; filename=reporte-gerencial-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv");
            // La marca BOM hace que Excel muestre bien los acentos.
            Response.BinaryWrite(Encoding.UTF8.GetPreamble());
            Response.Write(csv);
            Response.End();
        }

        private void CargarTablero()
        {
            lblTablero.Text = string.Empty;
            var periodo = (PeriodoEstadistica)Enum.Parse(typeof(PeriodoEstadistica), ddlPeriodo.SelectedValue);
            CargarResumen();
            CargarSuscripciones(periodo);
            CargarViajes(periodo);
        }

        // Tarjetas del mes y torta "de dónde sale la plata".
        private void CargarResumen()
        {
            var resumen = negocio.ObtenerResumenTablero();
            litSuscriptoresActivos.Text = resumen.SuscriptoresActivos.ToString("N0");
            litIngresoSuscriptores.Text = resumen.IngresoMensualSuscriptores.ToString("C0");
            litSuscripcionesMes.Text = resumen.SuscripcionesDelMes.ToString("N0");
            litViajesMes.Text = resumen.ViajesDelMes.ToString("N0");
            litMontoViajesMes.Text = resumen.MontoViajesDelMes.ToString("C0");
            litComisionesMes.Text = resumen.ComisionesDelMes.ToString("C0");
            litSegurosActivos.Text = resumen.SegurosActivos.ToString("N0");
            litComisionSeguros.Text = resumen.ComisionSeguros.ToString("C0");

            var ingresos = negocio.ObtenerIngresosDelMes();
            decimal total = 0;
            foreach (var ingreso in ingresos) total += ingreso.Monto;
            litIngresosMes.Text = total.ToString("C0");
            MostrarTorta(chrIngresos, ingresos);
        }

        private void CargarSuscripciones(PeriodoEstadistica periodo)
        {
            // Valor vacío = "Todas".
            var tipo = ddlTipoSuscripcion.SelectedValue;
            var total = negocio.ObtenerTotalSuscriptores(tipo);
            litTotalSuscripciones.Text = total.Monto.ToString("C0");
            litDetalleSuscripciones.Text = total.Cantidad.ToString("N0") + " suscriptores activos" + (tipo == string.Empty ? "" : " de " + tipo);

            // La torta reparte el total entre los tipos: con un tipo elegido no aporta nada.
            pnlTortaSuscripciones.Visible = tipo == string.Empty;
            if (pnlTortaSuscripciones.Visible)
                MostrarTorta(chrSuscripcionesTipos, negocio.ObtenerSuscriptoresPorTipo());

            MostrarEstadistica(negocio.ObtenerEstadisticaSuscripciones(periodo, tipo), chrSuscripcionesCantidad, chrSuscripcionesMonto, gvSuscripciones);

            // Flota SaaS se cobra por camión: ahí se agregan los gráficos de patentes.
            pnlPatentes.Visible = tipo == "Flota SaaS";
            if (pnlPatentes.Visible)
                MostrarEstadistica(negocio.ObtenerEstadisticaPatentes(periodo), chrPatentesCantidad, chrPatentesMonto, gvPatentes);
        }

        private void CargarViajes(PeriodoEstadistica periodo)
        {
            var carga = (TipoCarga)Enum.Parse(typeof(TipoCarga), ddlTipoCarga.SelectedValue);
            var total = negocio.ObtenerTotalViajes(periodo, carga);
            litTotalViajes.Text = total.Monto.ToString("C0");
            litComisionViajes.Text = total.Comision.ToString("C0");
            litDetalleViajes.Text = total.Cantidad.ToString("N0") + " viajes en " + TextoPeriodo(periodo);

            // Igual que en suscripciones: la torta por tipo de carga solo va con "Todas".
            pnlTortaViajes.Visible = carga == TipoCarga.Todas;
            if (pnlTortaViajes.Visible)
                MostrarTorta(chrViajesCargas, negocio.ObtenerViajesPorCarga(periodo));

            MostrarEstadistica(negocio.ObtenerEstadisticaViajes(periodo, carga), chrViajesCantidad, chrViajesMonto, gvViajes);
        }

        private static string TextoPeriodo(PeriodoEstadistica periodo)
        {
            if (periodo == PeriodoEstadistica.Diario) return "los últimos 30 días";
            if (periodo == PeriodoEstadistica.Mensual) return "los últimos 12 meses";
            return "los últimos 5 años";
        }

        private static void MostrarTorta(Chart torta, IList<TotalEstadisticaBE> porciones)
        {
            torta.DataSource = porciones;
            torta.DataBind();
        }

        // La misma serie va a sus dos gráficos (cantidad y plata) y a la tabla del reporte.
        private static void MostrarEstadistica(IList<PuntoEstadisticaBE> datos, Chart cantidad, Chart monto, GridView reporte)
        {
            CargarGrafico(cantidad, datos);
            CargarGrafico(monto, datos);
            reporte.DataSource = datos;
            reporte.DataBind();
        }

        private static void CargarGrafico(Chart grafico, IList<PuntoEstadisticaBE> datos)
        {
            // Con muchos períodos las etiquetas no entran derechas: se giran.
            grafico.ChartAreas[0].AxisX.LabelStyle.Angle = datos.Count > 6 ? -90 : 0;
            grafico.DataSource = datos;
            grafico.DataBind();
        }

        private void MostrarMensajeTablero(string mensaje, bool esError)
        {
            lblTablero.Text = mensaje;
            lblTablero.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        private void Registrar(string descripcion, string criticidad = "Informativa")
        {
            fachada.RegistrarEvento(GestorSesion.Instancia.IdUsuario, GestorSesion.Instancia.Usuario, descripcion, criticidad);
        }

        private void AplicarIdioma()
        {
            litRolMenu.Text = GestorSesion.Instancia.Rol;
            // El texto pertenece al botón para conservarlo en cada postback.
            btnMenuInicio.Text = "Inicio";
            btnMenuUsuarios.Text = "Usuarios";
            btnMenuRoles.Text = "Roles y permisos";
            btnMenuBitacora.Text = "Bitácora";
            btnMenuBaseDatos.Text = "Base de datos";
            litEstado.Text = "Sesión activa";
            litSesion.Text = "Se inició sesión con <span class='no-translate'>" + Server.HtmlEncode(GestorSesion.Instancia.Usuario)
                + "</span> y el rol <span class='no-translate'>" + Server.HtmlEncode(GestorSesion.Instancia.Rol) + "</span>";
            btnCerrarSesion.Text = "Cerrar sesión";
            litEtiquetaAdmin.Text = "Administración del sistema";
            litTituloAdmin.Text = "Roles y permisos";
            litBajadaAdmin.Text = "Creá roles y asignales los permisos necesarios.";
            litNuevoRol.Text = "Crear rol o familia";
            litNuevoPermiso.Text = "Asignar permiso";
            litRoles.Text = "Roles y familias configurados";
            litBitacora.Text = "Bitácora de eventos";
            txtRol.Attributes["placeholder"] = "Nombre del rol";
            txtBitacoraUsuario.Attributes["placeholder"] = "Nombre de usuario o ID";
            btnAgregarRol.Text = "Agregar";
            btnAgregarPermiso.Text = "Agregar";
            btnFiltrarBitacora.Text = "Filtrar";
            btnAnterior.Text = "Anterior";
            btnSiguiente.Text = "Siguiente";
            gvBitacora.Columns[0].HeaderText = "Fecha";
            gvBitacora.Columns[1].HeaderText = "ID";
            gvBitacora.Columns[2].HeaderText = "Usuario";
            gvBitacora.Columns[3].HeaderText = "Evento";
            gvBitacora.Columns[4].HeaderText = "Criticidad";
        }

        private void Intentar(Action accion)
        {
            Intentar(accion, MostrarMensaje);
        }

        // El mensaje de error tiene que aparecer en la sección visible: sin esto, un
        // SqlException en una sección que no sea "Roles" se muestra en lblMensaje (oculta
        // ahí) y la pantalla parece trabada, aunque el error ya se manejó bien.
        private void Intentar(Action accion, Action<string, bool> mostrar)
        {
            try { accion(); }
            catch (SqlException) { mostrar("No se pudo conectar con la base LogTechDB.", true); }
            catch (InvalidOperationException ex) { mostrar(Server.HtmlEncode(ex.Message), true); }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.CssClass = esError ? "panel-message error" : "panel-message success";
        }
    }
}
