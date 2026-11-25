using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using LogTech.BE;
using LogTech.Fachada;
using LogTech.Servicios;

namespace LogTech.Controles
{
    // Base de los módulos del panel (un control .ascx por módulo). Junta lo que repiten
    // todos: la fachada, los datos de la sesión, los permisos y cómo mostrar un mensaje.
    public class ControlBase : UserControl
    {
        protected readonly FachadaNegocio negocio = new FachadaNegocio();

        // Permisos del rol del usuario. Los carga Panel.aspx en cada pedido, antes de los eventos.
        public List<string> Permisos { get; set; }

        protected int IdUsuario { get { return GestorSesion.Instancia.IdUsuario; } }
        protected string Usuario { get { return GestorSesion.Instancia.Usuario; } }
        protected string Rol { get { return GestorSesion.Instancia.Rol; } }
        protected bool EsFlota { get { return Rol == "Administrador de Flota"; } }

        // El panel lo llama al entrar al módulo: carga la pantalla y registra la consulta (R1).
        public virtual void Abrir()
        {
        }

        protected bool Tiene(string permiso)
        {
            return Permisos != null && Permisos.Contains(permiso);
        }

        // Segunda barrera de cada opción: si se llega a una opción sin su permiso (el botón no debería verse), no se hace
        // nada, se avisa «No tenés los permisos necesarios» y queda en la bitácora.
        protected bool Permitir(string permiso, Label etiqueta)
        {
            if (Tiene(permiso)) return true;
            Mostrar(etiqueta, "No tenés los permisos necesarios para esta opción.", true);
            try { new FachadaSeguridad().RegistrarEvento(IdUsuario, Usuario, "Intento de usar una opción sin el permiso «" + permiso + "»", "Advertencia"); }
            catch (SqlException) { }
            return false;
        }

        protected static void Mostrar(Label etiqueta, string mensaje, bool esError)
        {
            // Label.Text no codifica: se codifica acá por si el mensaje trae un dato del usuario.
            etiqueta.Text = HttpUtility.HtmlEncode(mensaje);
            etiqueta.CssClass = esError ? "panel-message error" : "panel-message success";
        }

        // ERR002: si se cae la conexión (o no se puede guardar un archivo) se cancela y se avisa.
        protected void Intentar(Action accion, Label etiqueta)
        {
            try { accion(); }
            catch (SqlException) { Mostrar(etiqueta, "No se pudo conectar con la base LogTechDB. Intentá más tarde.", true); }
            catch (IntegridadException ex) { Mostrar(etiqueta, ex.Message, true); }
            catch (IOException) { Mostrar(etiqueta, "No se pudo guardar el archivo en el servidor. Intentá más tarde.", true); }
        }

        // Muestra el resultado de una operación de la fachada (null = salió bien) y devuelve si salió bien.
        protected static bool Resultado(Label etiqueta, string error, string exito)
        {
            Mostrar(etiqueta, error ?? exito, error != null);
            return error == null;
        }

        // R1 de las consultas.
        protected void RegistrarConsulta(string consulta)
        {
            negocio.RegistrarConsulta(IdUsuario, Usuario, consulta);
        }

        // Los campos numéricos del navegador (type="number") siempre usan punto decimal.
        protected static decimal LeerDecimal(string texto)
        {
            decimal valor;
            return decimal.TryParse((texto ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor) ? valor : 0;
        }

        protected static decimal? LeerCoordenada(string texto)
        {
            decimal valor;
            return decimal.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out valor) ? valor : (decimal?)null;
        }

        // Los campos de fecha del navegador (type="date") mandan yyyy-MM-dd.
        protected static DateTime LeerFecha(string texto)
        {
            DateTime valor;
            return DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor) ? valor : DateTime.MinValue;
        }

        // El vencimiento de la tarjeta se elige con el selector de mes (type="month"), que manda yyyy-MM.
        // La tarjeta lo guarda como MM/AA.
        protected static string LeerVencimiento(string texto)
        {
            DateTime mes;
            return DateTime.TryParseExact(texto, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out mes)
                ? mes.ToString("MM/yy", CultureInfo.InvariantCulture) : string.Empty;
        }

        // El selector de mes deja elegir de este mes a 10 años (validarRango lo vuelve a controlar) y arranca en el mes actual.
        protected static void PrepararVencimiento(TextBox campo)
        {
            campo.Attributes["min"] = DateTime.Today.ToString("yyyy-MM");
            campo.Attributes["max"] = DateTime.Today.AddYears(10).ToString("yyyy-MM");
            if (campo.Text.Length == 0) campo.Text = DateTime.Today.ToString("yyyy-MM");
        }

        // Id guardado en el ViewState del control (por ejemplo, la fila que se está editando).
        protected int LeerId(string clave)
        {
            return ViewState[clave] == null ? 0 : (int)ViewState[clave];
        }

        protected void GuardarId(string clave, int valor)
        {
            ViewState[clave] = valor;
        }

        protected static int Argumento(CommandEventArgs e)
        {
            int valor;
            return int.TryParse(Convert.ToString(e.CommandArgument), out valor) ? valor : 0;
        }

        protected static void Cargar(DropDownList lista, IEnumerable<string> valores, string primero)
        {
            lista.Items.Clear();
            if (primero != null) lista.Items.Add(new ListItem(primero, string.Empty));
            foreach (var valor in valores) lista.Items.Add(new ListItem(valor, valor));
        }

        // Archivo elegido en un FileUpload (null si no se eligió ninguno).
        protected static byte[] LeerArchivo(FileUpload control)
        {
            return control.HasFile ? control.FileBytes : null;
        }
    }
}
