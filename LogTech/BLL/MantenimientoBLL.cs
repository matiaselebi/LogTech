using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Security.AccessControl;
using System.Web.Hosting;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class MantenimientoBLL
    {
        private readonly MantenimientoDAL mantenimiento = new MantenimientoDAL();
        private readonly IntegridadDAL integridad = new IntegridadDAL();
        private readonly BitacoraBLL bitacora = new BitacoraBLL();

        // Carpeta configurada en Web.config (CarpetaBackups).
        private static string Carpeta
        {
            get { return HostingEnvironment.MapPath(ConfigurationManager.AppSettings["CarpetaBackups"]); }
        }

        public IList<ProblemaIntegridadBE> VerificarIntegridad()
        {
            return integridad.Verificar();
        }

        // CU-010-066: el administrador o la contingencia aceptan los datos actuales y se vuelven a firmar todas las tablas.
        // Antes se pide la contraseña de quien lo hace (ERR046): si no coincide, no se recalcula nada y queda en la bitácora.
        public bool RecalcularDigitos(int? idUsuario, string usuario, string clave)
        {
            if (!ClaveCorrecta(idUsuario, usuario, clave))
            {
                Registrar(idUsuario, usuario, "Intento de recalcular los dígitos verificadores con una contraseña incorrecta", "Crítica");
                return false;
            }
            mantenimiento.RecalcularDigitos();
            Registrar(idUsuario, usuario, "Recálculo de los dígitos verificadores de toda la base", "Crítica");
            return true;
        }

        // La contingencia no tiene fila en Usuarios: su contraseña está en Web.config.
        private static bool ClaveCorrecta(int? idUsuario, string usuario, string clave)
        {
            if (string.IsNullOrEmpty(clave)) return false;
            if (idUsuario == null) return UsuarioContingencia.Coincide(usuario, clave);
            var cuenta = new UsuarioDAL().ObtenerPorId(idUsuario.Value);
            return cuenta != null && SeguridadServicio.VerificarClave(clave, cuenta.ClaveSalt, cuenta.ClaveHash);
        }

        // ERR049: anota en la bitácora lo que se rompió, una línea por tabla con sus registros.
        public void RegistrarFallas(IList<ProblemaIntegridadBE> problemas, int? idUsuario, string usuario)
        {
            var porTabla = new Dictionary<string, List<string>>();
            foreach (var problema in problemas)
            {
                if (!porTabla.ContainsKey(problema.Tabla)) porTabla[problema.Tabla] = new List<string>();
                porTabla[problema.Tabla].Add(string.IsNullOrEmpty(problema.Fila) ? "registros agregados o borrados" : problema.Fila);
            }
            foreach (var tabla in porTabla)
            {
                var texto = "Error de integridad en " + tabla.Key + ": " + string.Join("; ", tabla.Value);
                Registrar(idUsuario, usuario, texto.Length > 250 ? texto.Substring(0, 247) + "..." : texto, "Crítica");
            }
        }

        public string GenerarBackup(int? idUsuario, string usuario)
        {
            PrepararCarpeta();
            var ruta = Path.Combine(Carpeta, "LogTechDB-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak");
            mantenimiento.Backup(ruta);
            Registrar(idUsuario, usuario, "Backup de la base de datos: " + Path.GetFileName(ruta), "Informativa");
            return ruta;
        }

        // Guarda el archivo subido en la carpeta de backups y restaura la base desde él.
        // Devuelve false si el archivo no es un backup válido.
        public bool RestaurarBackup(Stream archivo, int? idUsuario, string usuario)
        {
            PrepararCarpeta();
            var ruta = Path.Combine(Carpeta, "restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak");
            using (var destino = File.Create(ruta))
            {
                archivo.CopyTo(destino);
            }

            if (!mantenimiento.ArchivoValido(ruta)) return false;
            mantenimiento.Restore(ruta);
            Registrar(idUsuario, usuario, "Restauración de la base de datos desde " + Path.GetFileName(ruta), "Crítica");
            return true;
        }

        // La cuenta de servicio de SQL Server necesita leer y escribir en la carpeta
        // de backups. Se le da permiso al crearla para no depender de una config manual.
        private void PrepararCarpeta()
        {
            Directory.CreateDirectory(Carpeta);
            var cuenta = mantenimiento.CuentaServicio();
            if (string.IsNullOrEmpty(cuenta)) return;
            try
            {
                var info = new DirectoryInfo(Carpeta);
                var acl = info.GetAccessControl();
                acl.AddAccessRule(new FileSystemAccessRule(cuenta, FileSystemRights.Modify,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.None, AccessControlType.Allow));
                info.SetAccessControl(acl);
            }
            catch (SystemException) { }
        }

        // Con la base caída (contingencia) no se puede escribir en la bitácora: se omite.
        private void Registrar(int? idUsuario, string usuario, string descripcion, string criticidad)
        {
            try { bitacora.Registrar(idUsuario, usuario, descripcion, criticidad); }
            catch (SqlException) { }
        }
    }
}
