using System.Collections.Generic;
using System.IO;
using LogTech.BE;
using LogTech.BLL;
using LogTech.Servicios;

namespace LogTech.Fachada
{
    // Patrón Facade: es la única puerta de entrada de la capa de presentación a la
    // lógica de negocio. Las páginas no conocen las clases de la BLL: le piden todo a
    // la fachada y la fachada le delega el trabajo a la clase que corresponda.
    public class FachadaSeguridad
    {
        private readonly Autenticar autenticar = new Autenticar();
        private readonly RegistrarUsuario registrar = new RegistrarUsuario();
        private readonly RecuperarClaveBLL recuperar = new RecuperarClaveBLL();
        private readonly CuentaBLL cuenta = new CuentaBLL();
        private readonly GestionUsuariosBLL usuarios = new GestionUsuariosBLL();
        private readonly RolPermisoBLL roles = new RolPermisoBLL();
        private readonly FamiliaBLL familias = new FamiliaBLL();
        private readonly BitacoraBLL bitacora = new BitacoraBLL();
        private readonly MantenimientoBLL mantenimiento = new MantenimientoBLL();
        private readonly IdiomaBLL idioma = new IdiomaBLL();

        // ---- Acceso ----

        public ResultadoAutenticacionBE IniciarSesion(string usuario, string clave)
        {
            return autenticar.Ejecutar(usuario, clave);
        }

        public bool CrearCuenta(UsuarioBE usuario, string clave, out string mensaje)
        {
            return registrar.Ejecutar(usuario, clave, out mensaje);
        }

        public bool ClaveCumplePolitica(string clave)
        {
            return SeguridadServicio.ClaveCumplePolitica(clave);
        }

        public void GuardarIdioma(int idUsuario, string codigoIdioma)
        {
            idioma.Actualizar(idUsuario, codigoIdioma);
        }

        // ---- Contraseña ----

        public void SolicitarRecuperacion(string correo, string urlBase)
        {
            recuperar.Solicitar(correo, urlBase);
        }

        public bool TokenVigente(string token)
        {
            return recuperar.TokenVigente(token);
        }

        public bool RestablecerClave(string token, string nuevaClave)
        {
            return recuperar.Restablecer(token, nuevaClave);
        }

        public bool CambiarClave(int idUsuario, string usuario, string claveActual, string claveNueva)
        {
            return cuenta.CambiarClave(idUsuario, usuario, claveActual, claveNueva);
        }

        public bool CuentaActiva(int idUsuario)
        {
            return cuenta.SigueActiva(idUsuario);
        }

        // ---- Gestión de usuarios ----

        public IList<UsuarioBE> ListarUsuarios(FiltroUsuario filtro)
        {
            return usuarios.Listar(filtro);
        }

        public UsuarioBE ObtenerUsuario(int idUsuario)
        {
            return usuarios.Obtener(idUsuario);
        }

        public string ModificarUsuario(UsuarioBE datos, int idEjecutor, string ejecutor)
        {
            return usuarios.Modificar(datos, idEjecutor, ejecutor);
        }

        // Mi perfil: el usuario cambia sus propios datos de contacto.
        public string ModificarMiPerfil(UsuarioBE datos, int idUsuario, string usuario)
        {
            return usuarios.ModificarPerfil(datos, idUsuario, usuario);
        }

        public string CambiarFotoPerfil(int idUsuario, string usuario, byte[] foto, string nombreArchivo)
        {
            return usuarios.CambiarFoto(idUsuario, usuario, foto, nombreArchivo);
        }

        public string RutaFotoPerfil(int idUsuario)
        {
            return usuarios.RutaFoto(idUsuario);
        }

        public string VersionFotoPerfil(int idUsuario)
        {
            return usuarios.VersionFoto(idUsuario);
        }

        public string DarDeBajaUsuario(int idUsuario, int idEjecutor, string ejecutor)
        {
            return usuarios.DarDeBaja(idUsuario, idEjecutor, ejecutor);
        }

        public string BloquearUsuario(int idUsuario, int idEjecutor, string ejecutor)
        {
            return usuarios.Bloquear(idUsuario, idEjecutor, ejecutor);
        }

        public string DesbloquearUsuario(int idUsuario, int idEjecutor, string ejecutor)
        {
            return usuarios.Desbloquear(idUsuario, idEjecutor, ejecutor);
        }

        // ---- Roles, permisos y familias ----

        public IList<RolBE> ObtenerRoles()
        {
            return roles.ObtenerTodos();
        }

        public IList<PermisoBE> ObtenerPermisos()
        {
            return roles.ObtenerCatalogoPermisos();
        }

        // Lo que puede hacer el rol (arma el menú del panel).
        public List<string> ObtenerPermisosDeRol(string rol)
        {
            return roles.ObtenerPermisosDeRol(rol);
        }

        // Lo que puede hacer el usuario: su rol más sus permisos particulares (arma el menú del panel).
        public List<string> ObtenerPermisosDeUsuario(int idUsuario, string rol)
        {
            return roles.ObtenerPermisosDeUsuario(idUsuario, rol);
        }

        // El perfil del usuario (patrón Composite): su rol con sus familias y sus permisos particulares.
        public RolBE ObtenerPerfilDeUsuario(int idUsuario, string usuario, string rol)
        {
            return roles.ObtenerPerfilDeUsuario(idUsuario, usuario, rol);
        }

        public bool AgregarPermisoUsuario(int idUsuario, string permiso)
        {
            return roles.AgregarPermisoUsuario(idUsuario, permiso);
        }

        public bool QuitarPermisoUsuario(int idUsuario, string permiso)
        {
            return roles.QuitarPermisoUsuario(idUsuario, permiso);
        }

        public bool AgregarFamiliaUsuario(int idUsuario, int idFamilia)
        {
            return roles.AgregarFamiliaUsuario(idUsuario, idFamilia);
        }

        public bool QuitarFamiliaUsuario(int idUsuario, int idFamilia)
        {
            return roles.QuitarFamiliaUsuario(idUsuario, idFamilia);
        }

        // Los permisos que un rol base trae por defecto no se le pueden quitar.
        public bool EsPermisoPorDefecto(int idRol, string permiso)
        {
            return roles.EsPermisoPorDefecto(idRol, permiso);
        }

        public bool AgregarRol(string nombre, bool esFamilia)
        {
            return roles.AgregarRol(nombre, esFamilia);
        }

        public bool EliminarRol(int idRol)
        {
            return roles.Eliminar(idRol);
        }

        public bool AgregarPermiso(int idRol, string permiso)
        {
            return roles.AgregarPermiso(idRol, permiso);
        }

        public bool QuitarPermiso(int idRol, string permiso)
        {
            return roles.QuitarPermiso(idRol, permiso);
        }

        public bool AgregarFamilia(int idPadre, int idFamilia)
        {
            return familias.Agregar(idPadre, idFamilia);
        }

        public bool QuitarFamilia(int idPadre, int idFamilia)
        {
            return familias.Quitar(idPadre, idFamilia);
        }

        // ---- Bitácora ----

        public void RegistrarEvento(int? idUsuario, string usuario, string descripcion, string criticidad)
        {
            bitacora.Registrar(idUsuario, usuario, descripcion, criticidad);
        }

        public IList<EventoBitacoraBE> ObtenerBitacora(int pagina, int cantidad, FiltroBitacora filtro, out int total)
        {
            return bitacora.ObtenerPagina(pagina, cantidad, filtro, out total);
        }

        // La tabla guarda los últimos 30 días; los eventos más viejos pasan al historial XML.
        public void ArchivarBitacora(int? idUsuario, string usuario)
        {
            bitacora.ArchivarViejos(idUsuario, usuario);
        }

        // HistorialAuditoria_Seguridad.xml con los eventos filtrados (null si no hay eventos).
        public byte[] ExportarBitacoraXml(FiltroBitacora filtro, int? idUsuario, string usuario)
        {
            return bitacora.ExportarXml(filtro, idUsuario, usuario);
        }

        // ---- Base de datos ----

        public string GenerarBackup(int? idUsuario, string usuario)
        {
            return mantenimiento.GenerarBackup(idUsuario, usuario);
        }

        public bool RestaurarBackup(Stream archivo, int? idUsuario, string usuario)
        {
            return mantenimiento.RestaurarBackup(archivo, idUsuario, usuario);
        }

        public bool RecalcularDigitos(int? idUsuario, string usuario, string clave)
        {
            return mantenimiento.RecalcularDigitos(idUsuario, usuario, clave);
        }

        public void RegistrarFallasIntegridad(IList<ProblemaIntegridadBE> problemas, int? idUsuario, string usuario)
        {
            mantenimiento.RegistrarFallas(problemas, idUsuario, usuario);
        }

        public IList<ProblemaIntegridadBE> VerificarIntegridad()
        {
            return mantenimiento.VerificarIntegridad();
        }
    }
}
