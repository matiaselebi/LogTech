using System;
using System.Web;
using LogTech.BE;

namespace LogTech.Servicios
{
    // Patrón Singleton: existe una sola instancia del gestor para toda la aplicación.
    // Los datos no se guardan en el objeto sino en la sesión ASP.NET de cada navegador,
    // así que cada usuario ve su propia sesión.
    public sealed class GestorSesion
    {
        private static GestorSesion instancia;

        // Constructor privado: nadie puede hacer "new GestorSesion()" desde afuera.
        private GestorSesion() { }

        public static GestorSesion Instancia
        {
            get
            {
                if (instancia == null) instancia = new GestorSesion();
                return instancia;
            }
        }

        public bool HaySesion { get { return HttpContext.Current.Session["Usuario"] != null; } }
        public int IdUsuario { get { return Convert.ToInt32(HttpContext.Current.Session["IdUsuario"]); } }
        public string Usuario { get { return Convert.ToString(HttpContext.Current.Session["Usuario"]); } }
        public string Rol { get { return Convert.ToString(HttpContext.Current.Session["Rol"]); } }

        public void Iniciar(UsuarioBE usuario)
        {
            HttpContext.Current.Session["IdUsuario"] = usuario.IdUsuario;
            HttpContext.Current.Session["Usuario"] = usuario.Usuario;
            HttpContext.Current.Session["Rol"] = usuario.Rol;
        }

        // El dador que contrata la Flota SaaS sigue con la misma sesión, ahora como Administrador de Flota.
        public void CambiarRol(string rol)
        {
            HttpContext.Current.Session["Rol"] = rol;
        }

        // El administrador o la contingencia entraron con la base dañada: el panel muestra el error de integridad
        // hasta que se recalculen los dígitos o se restaure un backup.
        public bool IntegridadComprometida
        {
            get { return HttpContext.Current.Session["IntegridadComprometida"] != null; }
            set
            {
                if (value) HttpContext.Current.Session["IntegridadComprometida"] = true;
                else HttpContext.Current.Session.Remove("IntegridadComprometida");
            }
        }

        public void Cerrar()
        {
            HttpContext.Current.Session.Remove("IntegridadComprometida");
            HttpContext.Current.Session.Remove("IdUsuario");
            HttpContext.Current.Session.Remove("Usuario");
            HttpContext.Current.Session.Remove("Rol");
        }
    }
}
