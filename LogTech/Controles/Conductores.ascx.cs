using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using LogTech.BE;

namespace LogTech.Controles
{
    public partial class Conductores : ControlBase
    {
        private const string Permiso = "Gestionar transportistas";

        // Los vencimientos se eligen de mañana a 10 años (los controla validarRango).
        protected void Page_Load(object sender, EventArgs e)
        {
            foreach (var fecha in new[] { txtVenceDni, txtVenceLicencia, txtVenceAntecedentes })
            {
                fecha.Attributes["min"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                fecha.Attributes["max"] = DateTime.Today.AddYears(10).ToString("yyyy-MM-dd");
            }
        }

        // Lo llama el panel al entrar al módulo (CU-002-014).
        public override void Abrir()
        {
            pnlFormulario.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarConductores();
                RegistrarConsulta("Consulta de conductores de la flota");
            }, lblMensaje);
        }

        private void CargarConductores()
        {
            gvConductores.DataSource = negocio.ListarConductores(IdUsuario, txtBuscar.Text, ddlEstado.SelectedValue);
            gvConductores.DataBind();
        }

        protected static string TextoSemaforo(object semaforo)
        {
            if ((string)semaforo == "Verde") return "Habilitado";
            return (string)semaforo == "Amarillo" ? "En revisión" : "Inhabilitado";
        }

        // El texto o el estado del filtro cambiaron (AutoPostBack): se vuelve a cargar la grilla.
        protected void Filtrar(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            pnlFormulario.Visible = false;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                CargarConductores();
                RegistrarConsulta("Consulta de conductores de la flota (búsqueda)");
            }, lblMensaje);
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            GuardarId("IdConductor", 0);
            litTituloFormulario.Text = "Nuevo conductor";
            txtNombre.Text = txtApellido.Text = txtDni.Text = txtLicencia.Text = txtTelefono.Text = string.Empty;
            txtVenceDni.Text = txtVenceLicencia.Text = txtVenceAntecedentes.Text = string.Empty;
            pnlDocumentos.Visible = true;
            pnlFormulario.Visible = true;
            lblMensaje.Text = string.Empty;
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlFormulario.Visible = false;
        }

        // CU-002-011 y CU-002-012.
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var conductor = new ConductorBE
            {
                IdConductor = LeerId("IdConductor"),
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                Dni = txtDni.Text,
                NumeroLicencia = txtLicencia.Text,
                Telefono = txtTelefono.Text
            };
            Intentar(() =>
            {
                var nuevo = conductor.IdConductor == 0;
                var error = nuevo ? negocio.AltaConductor(conductor, DocumentosNuevos(), IdUsuario, Usuario)
                    : negocio.ModificarConductor(conductor, IdUsuario, Usuario);
                if (!Resultado(lblMensaje, error, nuevo ? "El conductor quedó registrado. Su documentación queda en revisión: cuando LogTech la apruebe, se le pueden asignar viajes."
                    : "Los datos del conductor fueron actualizados.")) return;
                pnlFormulario.Visible = false;
                CargarConductores();
            }, lblMensaje);
        }

        // Los tres documentos del formulario, con su vencimiento y su archivo.
        private List<DocumentoBE> DocumentosNuevos()
        {
            var tipos = negocio.DocumentosDelConductor();
            var archivos = new[] { fuDni, fuLicencia, fuAntecedentes };
            var vencimientos = new[] { txtVenceDni, txtVenceLicencia, txtVenceAntecedentes };
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

        protected void gvConductores_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!Permitir(Permiso, lblMensaje)) return;
            var id = Argumento(e);
            if (id == 0) return;
            lblMensaje.Text = string.Empty;
            Intentar(() =>
            {
                if (e.CommandName == "Modificar")
                {
                    var conductor = negocio.ObtenerConductor(id, IdUsuario);
                    if (conductor == null) { Mostrar(lblMensaje, "El conductor no existe.", true); return; }
                    GuardarId("IdConductor", id);
                    litTituloFormulario.Text = "Modificar a " + conductor.NombreCompleto;
                    txtNombre.Text = conductor.Nombre;
                    txtApellido.Text = conductor.Apellido;
                    txtDni.Text = conductor.Dni;
                    txtLicencia.Text = conductor.NumeroLicencia;
                    txtTelefono.Text = conductor.Telefono;
                    // La documentación se cambia en «Mi documentación».
                    pnlDocumentos.Visible = false;
                    pnlFormulario.Visible = true;
                }
                else if (e.CommandName == "Baja")
                {
                    // CU-002-013.
                    pnlFormulario.Visible = false;
                    Resultado(lblMensaje, negocio.BajaConductor(id, IdUsuario, Usuario), "El conductor fue dado de baja.");
                    CargarConductores();
                }
            }, lblMensaje);
        }
    }
}
