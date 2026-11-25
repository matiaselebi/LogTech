using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;

namespace LogTech.BLL
{
    public sealed class RolPermisoBLL
    {
        private readonly RolPermisoDAL rolesPermisos = new RolPermisoDAL();

        // Los permisos con los que viene cada rol base (los del script de la base). No se le pueden quitar, por si
        // alguien deja a un rol sin lo que necesita para operar: se le pueden agregar otros y quitar los agregados.
        private static readonly Dictionary<string, string[]> PermisosPorDefecto = new Dictionary<string, string[]>
        {
            { "Administrador", new[] { "Administrar usuarios", "Administrar roles", "Administrar permisos", "Administrar familias",
                "Consultar bitácora", "Realizar backup", "Restaurar base de datos", "Recuperar dígitos verificadores",
                "Validar documentación", "Generar facturas corporativas", "Generar reportes gerenciales",
                "Resolver disputas comerciales", "Supervisar viajes" } },
            { "Administrador de Flota", new[] { "Consultar match de cargas", "Aceptar ofertas de carga", "Gestionar vehículos",
                "Gestionar transportistas", "Asignar viajes internos", "Gestionar documentación", "Gestionar remitos digitales",
                "Reportar incidencias de viaje", "Consultar estado de pago", "Gestionar pólizas de seguro",
                "Actualizar estado de viaje", "Consultar monitoreo de viaje", "Consultar reputación", "Gestionar suscripción SaaS",
                "Generar reportes gerenciales", "Consultar facturas" } },
            { "Transportista", new[] { "Consultar match de cargas", "Aceptar ofertas de carga", "Gestionar vehículos",
                "Gestionar documentación", "Gestionar remitos digitales", "Reportar incidencias de viaje", "Consultar estado de pago",
                "Gestionar factoring", "Gestionar pólizas de seguro", "Actualizar estado de viaje", "Consultar monitoreo de viaje",
                "Calificar servicio de transporte", "Consultar reputación", "Consultar facturas", "Gestionar suscripción premium" } },
            { "Dador de carga", new[] { "Publicar ofertas de carga", "Gestionar ofertas de carga", "Consultar match de cargas",
                "Gestionar documentación", "Gestionar remitos digitales", "Gestionar fondos escrow", "Consultar estado de pago",
                "Gestionar disputas comerciales", "Gestionar pólizas de seguro", "Consultar monitoreo de viaje",
                "Calificar servicio de transporte", "Consultar reputación", "Gestionar suscripción SaaS", "Consultar facturas" } }
        };

        // true si el permiso es uno de los que el rol base trae por defecto.
        public bool EsPermisoPorDefecto(int idRol, string permiso)
        {
            foreach (var rol in rolesPermisos.ObtenerTodos())
            {
                string[] porDefecto;
                if (rol.IdRol == idRol && PermisosPorDefecto.TryGetValue(rol.Nombre, out porDefecto))
                    return Array.IndexOf(porDefecto, permiso) >= 0;
            }
            return false;
        }

        public IList<RolBE> ObtenerTodos()
        {
            return rolesPermisos.ObtenerTodos();
        }

        public IList<PermisoBE> ObtenerCatalogoPermisos()
        {
            return rolesPermisos.ObtenerCatalogo();
        }

        // Permisos del rol con los de sus familias (Composite). Rol desconocido = ninguno.
        public List<string> ObtenerPermisosDeRol(string rol)
        {
            foreach (var actual in rolesPermisos.ObtenerTodos())
            {
                if (actual.Nombre == rol) return actual.ObtenerPermisos();
            }
            return new List<string>();
        }

        // Patrón Composite: el usuario es un compuesto más. Adentro tiene su rol (con sus familias), las familias que se
        // le dieron a él (compuestos) y sus permisos particulares (hojas), así que sus permisos se calculan igual que los de un rol: ObtenerPermisos() recorre
        // el árbol y junta todo sin repetir.
        public RolBE ObtenerPerfilDeUsuario(int idUsuario, string usuario, string rol)
        {
            var perfil = new RolBE(0, usuario ?? string.Empty);
            var todos = rolesPermisos.ObtenerTodos();
            foreach (var actual in todos)
                if (actual.Nombre == rol) perfil.Agregar(actual);
            foreach (var idFamilia in rolesPermisos.ObtenerFamiliasDelUsuario(idUsuario))
                foreach (var familia in todos)
                    if (familia.IdRol == idFamilia && familia.EsFamilia) perfil.Agregar(familia);
            foreach (var particular in rolesPermisos.ObtenerDelUsuario(idUsuario))
                perfil.Agregar(new PermisoBE(particular));
            return perfil;
        }

        // Lo que puede hacer un usuario (arma el menú del panel): los permisos de todo su perfil.
        public List<string> ObtenerPermisosDeUsuario(int idUsuario, string rol)
        {
            return ObtenerPerfilDeUsuario(idUsuario, null, rol).ObtenerPermisos();
        }

        public bool AgregarPermisoUsuario(int idUsuario, string nombre)
        {
            nombre = (nombre ?? string.Empty).Trim();
            return idUsuario > 0 && nombre.Length > 0 && nombre.Length <= 60 && rolesPermisos.AgregarAUsuario(idUsuario, nombre);
        }

        public bool QuitarPermisoUsuario(int idUsuario, string nombre)
        {
            return idUsuario > 0 && rolesPermisos.QuitarDeUsuario(idUsuario, nombre);
        }

        public bool AgregarRol(string nombre, bool esFamilia)
        {
            nombre = (nombre ?? string.Empty).Trim();
            return nombre.Length > 0 && nombre.Length <= 50 && rolesPermisos.AgregarRol(nombre, esFamilia);
        }

        public bool AgregarPermiso(int idRol, string nombre)
        {
            nombre = (nombre ?? string.Empty).Trim();
            return nombre.Length > 0 && nombre.Length <= 60 && rolesPermisos.AgregarPermiso(idRol, nombre);
        }

        public bool QuitarPermiso(int idRol, string nombre)
        {
            return !EsPermisoPorDefecto(idRol, nombre) && rolesPermisos.QuitarPermiso(idRol, nombre);
        }

        public bool AgregarFamiliaUsuario(int idUsuario, int idFamilia)
        {
            return idUsuario > 0 && idFamilia > 0 && rolesPermisos.AgregarFamiliaAUsuario(idUsuario, idFamilia);
        }

        public bool QuitarFamiliaUsuario(int idUsuario, int idFamilia)
        {
            return idUsuario > 0 && rolesPermisos.QuitarFamiliaDeUsuario(idUsuario, idFamilia);
        }

        public bool Eliminar(int idRol)
        {
            return idRol > 0 && rolesPermisos.Eliminar(idRol);
        }
    }
}
