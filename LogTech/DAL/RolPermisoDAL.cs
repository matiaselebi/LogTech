using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class RolPermisoDAL
    {
        public IList<RolBE> ObtenerTodos()
        {
            var roles = new List<RolBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = EjecutorDAL.Procedimiento("usp_RolPermiso_ObtenerTodos", conexion))
            {
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        var idRol = lector.GetInt32(0);
                        var rol = roles.Find(x => x.IdRol == idRol);
                        if (rol == null)
                        {
                            rol = new RolBE(idRol, lector.GetString(1)) { EsFamilia = lector.GetBoolean(3) };
                            roles.Add(rol);
                        }
                        if (!lector.IsDBNull(2)) rol.Agregar(new PermisoBE(lector.GetString(2)));
                    }
                    // El procedimiento devuelve un segundo resultado: las familias de cada rol (patrón Composite).
                    lector.NextResult();
                    while (lector.Read())
                        roles.Find(x => x.IdRol == lector.GetInt32(0))
                            .Agregar(roles.Find(x => x.IdRol == lector.GetInt32(1)));
                }
            }
            return roles;
        }

        public bool AgregarRol(string nombre, bool esFamilia)
        {
            const string sql = @"IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = @Nombre)
                BEGIN INSERT INTO Roles (Nombre, EsFamilia) VALUES (@Nombre, @EsFamilia); SELECT 1; END ELSE SELECT 0;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@EsFamilia", SqlDbType.Bit) { Value = esFamilia },
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 50) { Value = nombre });
        }

        public IList<PermisoBE> ObtenerCatalogo()
        {
            var permisos = new List<PermisoBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("SELECT Nombre FROM Permisos ORDER BY Nombre;", conexion))
            {
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                    while (lector.Read()) permisos.Add(new PermisoBE(lector.GetString(0)));
            }
            return permisos;
        }

        public bool AgregarPermiso(int idRol, string nombre)
        {
            // Solo vincula permisos que ya existen en el catálogo. No crea permisos nuevos.
            const string sql = @"DECLARE @IdPermiso INT;
                SELECT @IdPermiso = IdPermiso FROM Permisos WHERE Nombre = @Nombre;
                IF @IdPermiso IS NULL BEGIN SELECT 0; RETURN; END;
                IF NOT EXISTS (SELECT 1 FROM RolPermiso WHERE IdRol = @IdRol AND IdPermiso = @IdPermiso)
                BEGIN INSERT INTO RolPermiso (IdRol, IdPermiso) VALUES (@IdRol, @IdPermiso); SELECT 1; END ELSE SELECT 0;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@IdRol", SqlDbType.Int) { Value = idRol },
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 60) { Value = nombre });
        }

        public bool QuitarPermiso(int idRol, string nombre)
        {
            return EjecutorDAL.EjecutarBooleano(@"DELETE rp FROM RolPermiso rp
                JOIN Permisos p ON p.IdPermiso = rp.IdPermiso
                WHERE rp.IdRol = @Rol AND p.Nombre = @Nombre;
                SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;",
                new SqlParameter("@Rol", SqlDbType.Int) { Value = idRol },
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 60) { Value = nombre });
        }

        // Permisos particulares: los que tiene un usuario además de los de su rol (tabla UsuarioPermiso).
        public List<string> ObtenerDelUsuario(int idUsuario)
        {
            return EjecutorDAL.Leer(@"SELECT p.Nombre FROM UsuarioPermiso up
                JOIN Permisos p ON p.IdPermiso = up.IdPermiso
                WHERE up.IdUsuario = @Usuario ORDER BY p.Nombre;", lector => lector.GetString(0),
                new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario });
        }

        // Solo vincula permisos que ya existen en el catálogo. Devuelve false si el usuario ya lo tenía.
        public bool AgregarAUsuario(int idUsuario, string nombre)
        {
            const string sql = @"DECLARE @IdPermiso INT;
                SELECT @IdPermiso = IdPermiso FROM Permisos WHERE Nombre = @Nombre;
                IF @IdPermiso IS NULL OR NOT EXISTS (SELECT 1 FROM Usuarios WHERE IdUsuario = @Usuario) BEGIN SELECT 0; RETURN; END;
                IF NOT EXISTS (SELECT 1 FROM UsuarioPermiso WHERE IdUsuario = @Usuario AND IdPermiso = @IdPermiso)
                BEGIN INSERT INTO UsuarioPermiso (IdUsuario, IdPermiso) VALUES (@Usuario, @IdPermiso); SELECT 1; END ELSE SELECT 0;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 60) { Value = nombre });
        }

        public bool QuitarDeUsuario(int idUsuario, string nombre)
        {
            return EjecutorDAL.EjecutarBooleano(@"DELETE up FROM UsuarioPermiso up
                JOIN Permisos p ON p.IdPermiso = up.IdPermiso
                WHERE up.IdUsuario = @Usuario AND p.Nombre = @Nombre;
                SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;",
                new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 60) { Value = nombre });
        }

        // Familias que tiene un usuario además de su rol (tabla UsuarioFamilia).
        public List<int> ObtenerFamiliasDelUsuario(int idUsuario)
        {
            return EjecutorDAL.Leer("SELECT IdFamilia FROM UsuarioFamilia WHERE IdUsuario = @Usuario ORDER BY IdFamilia;",
                lector => lector.GetInt32(0), new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario });
        }

        // Solo familias (no roles). Devuelve false si el usuario ya la tenía.
        public bool AgregarFamiliaAUsuario(int idUsuario, int idFamilia)
        {
            const string sql = @"IF NOT EXISTS (SELECT 1 FROM Roles WHERE IdRol = @Familia AND EsFamilia = 1)
                    OR NOT EXISTS (SELECT 1 FROM Usuarios WHERE IdUsuario = @Usuario) BEGIN SELECT 0; RETURN; END;
                IF NOT EXISTS (SELECT 1 FROM UsuarioFamilia WHERE IdUsuario = @Usuario AND IdFamilia = @Familia)
                BEGIN INSERT INTO UsuarioFamilia (IdUsuario, IdFamilia) VALUES (@Usuario, @Familia); SELECT 1; END ELSE SELECT 0;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Familia", SqlDbType.Int) { Value = idFamilia });
        }

        public bool QuitarFamiliaDeUsuario(int idUsuario, int idFamilia)
        {
            return EjecutorDAL.EjecutarBooleano(@"DELETE FROM UsuarioFamilia WHERE IdUsuario = @Usuario AND IdFamilia = @Familia;
                SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;",
                new SqlParameter("@Usuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Familia", SqlDbType.Int) { Value = idFamilia });
        }

        public bool Eliminar(int idRol)
        {
            // EjecutorDAL mantiene las eliminaciones y el DVV en una transacción.
            const string sql = @"IF NOT EXISTS (SELECT 1 FROM Roles WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdRol = @Rol AND Nombre NOT IN ('Administrador', 'Administrador de Flota', 'Transportista', 'Dador de carga'))
                    OR EXISTS (SELECT 1 FROM UsuarioRol WITH (UPDLOCK, HOLDLOCK) WHERE IdRol = @Rol)
                BEGIN SELECT 0; RETURN; END;
                DELETE FROM RolFamilia WHERE IdPadre = @Rol OR IdFamilia = @Rol;
                DELETE FROM UsuarioFamilia WHERE IdFamilia = @Rol;
                DELETE FROM RolPermiso WHERE IdRol = @Rol;
                DELETE FROM Roles WHERE IdRol = @Rol;
                SELECT 1;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@Rol", SqlDbType.Int) { Value = idRol });
        }
    }
}
