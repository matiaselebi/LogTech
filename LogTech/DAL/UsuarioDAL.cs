using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using LogTech.BE;
using LogTech.Servicios;

namespace LogTech.DAL
{
    // El correo, el teléfono, el CUIT/CUIL y la licencia se guardan cifrados con AES (CifradoServicio).
    public sealed class UsuarioDAL
    {
        public UsuarioBE ObtenerPorId(int idUsuario)
        {
            const string sql = @"SELECT u.IdUsuario, u.NombreUsuario, u.CorreoElectronico, u.Nombre, u.Apellido,
                u.Telefono, u.CuitCuil, u.RazonSocial, u.NumeroLicencia, r.Nombre, u.Estado,
                u.IntentosFallidos, u.ClaveHash, u.ClaveSalt, u.CondicionIva
                FROM Usuarios u
                INNER JOIN UsuarioRol ur ON ur.IdUsuario = u.IdUsuario
                INNER JOIN Roles r ON r.IdRol = ur.IdRol
                WHERE u.IdUsuario = @Id;";
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = idUsuario;
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;
                    return new UsuarioBE
                    {
                        IdUsuario = lector.GetInt32(0),
                        Usuario = lector.GetString(1),
                        Correo = CifradoServicio.Descifrar(lector.GetString(2)),
                        Nombre = lector.GetString(3),
                        Apellido = lector.GetString(4),
                        Telefono = CifradoServicio.Descifrar(lector.GetString(5)),
                        CuitCuil = CifradoServicio.Descifrar(lector.GetString(6)),
                        RazonSocial = lector.IsDBNull(7) ? null : lector.GetString(7),
                        NumeroLicencia = lector.IsDBNull(8) ? null : CifradoServicio.Descifrar(lector.GetString(8)),
                        Rol = lector.GetString(9),
                        Estado = lector.GetString(10),
                        IntentosFallidos = lector.GetInt32(11),
                        ClaveHash = lector.GetString(12),
                        ClaveSalt = lector.GetString(13),
                        CondicionIva = lector.GetString(14)
                    };
                }
            }
        }

        public string ObtenerEstado(int idUsuario)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("SELECT Estado FROM Usuarios WHERE IdUsuario = @Id;", conexion))
            {
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = idUsuario;
                conexion.Open();
                return Convert.ToString(comando.ExecuteScalar());
            }
        }

        public IList<UsuarioBE> ObtenerTodos(FiltroUsuario filtro)
        {
            var where = new StringBuilder(" WHERE 1 = 1");
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
                where.Append(" AND (u.NombreUsuario LIKE @Texto OR u.CorreoElectronico = @Correo OR u.Nombre LIKE @Texto OR u.Apellido LIKE @Texto"
                    + " OR CONVERT(NVARCHAR(12), u.IdUsuario) = @Id)");   // también por el número de ID
            if (!string.IsNullOrWhiteSpace(filtro.Estado))
                where.Append(" AND u.Estado = @Estado");

            var sql = @"SELECT u.IdUsuario, u.NombreUsuario, u.CorreoElectronico, u.Nombre, u.Apellido,
                u.Telefono, u.RazonSocial, u.NumeroLicencia, r.Nombre, u.Estado, u.IntentosFallidos
                FROM Usuarios u
                INNER JOIN UsuarioRol ur ON ur.IdUsuario = u.IdUsuario
                INNER JOIN Roles r ON r.IdRol = ur.IdRol" + where + " ORDER BY u.NombreUsuario;";

            var lista = new List<UsuarioBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                if (!string.IsNullOrWhiteSpace(filtro.Texto))
                {
                    comando.Parameters.Add("@Texto", SqlDbType.NVarChar, 120).Value = "%" + filtro.Texto.Trim() + "%";
                    comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(filtro.Texto);   // correo completo
                    comando.Parameters.Add("@Id", SqlDbType.NVarChar, 12).Value = filtro.Texto.Trim();
                }
                if (!string.IsNullOrWhiteSpace(filtro.Estado))
                    comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = filtro.Estado;
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        lista.Add(new UsuarioBE
                        {
                            IdUsuario = lector.GetInt32(0),
                            Usuario = lector.GetString(1),
                            Correo = CifradoServicio.Descifrar(lector.GetString(2)),
                            Nombre = lector.GetString(3),
                            Apellido = lector.GetString(4),
                            Telefono = CifradoServicio.Descifrar(lector.GetString(5)),
                            RazonSocial = lector.IsDBNull(6) ? null : lector.GetString(6),
                            NumeroLicencia = lector.IsDBNull(7) ? null : CifradoServicio.Descifrar(lector.GetString(7)),
                            Rol = lector.GetString(8),
                            Estado = lector.GetString(9),
                            IntentosFallidos = lector.GetInt32(10)
                        });
                    }
                }
            }
            return lista;
        }

        public bool CorreoEnUso(string correo, int exceptoId)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(
                "SELECT COUNT(*) FROM Usuarios WHERE CorreoElectronico = @Correo AND IdUsuario <> @Id;", conexion))
            {
                comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(correo);
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = exceptoId;
                conexion.Open();
                return Convert.ToInt32(comando.ExecuteScalar()) > 0;
            }
        }

        public void Modificar(UsuarioBE usuario)
        {
            const string sql = @"UPDATE Usuarios SET Nombre = @Nombre, Apellido = @Apellido,
                CorreoElectronico = @Correo, Telefono = @Telefono,
                RazonSocial = @RazonSocial, NumeroLicencia = @NumeroLicencia, CondicionIva = @CondicionIva
                WHERE IdUsuario = @Id;";
            EjecutorDAL.EjecutarCambio(sql, "Usuarios",
                new SqlParameter("@Nombre", SqlDbType.NVarChar, 60) { Value = usuario.Nombre },
                new SqlParameter("@Apellido", SqlDbType.NVarChar, 60) { Value = usuario.Apellido },
                new SqlParameter("@Correo", SqlDbType.NVarChar, 200) { Value = CifradoServicio.CifrarCorreo(usuario.Correo) },
                new SqlParameter("@Telefono", SqlDbType.NVarChar, 100) { Value = CifradoServicio.Cifrar(usuario.Telefono) },
                new SqlParameter("@RazonSocial", SqlDbType.NVarChar, 120) { Value = (object)usuario.RazonSocial ?? DBNull.Value },
                new SqlParameter("@NumeroLicencia", SqlDbType.NVarChar, 100) { Value = (object)CifradoServicio.Cifrar(usuario.NumeroLicencia) ?? DBNull.Value },
                new SqlParameter("@CondicionIva", SqlDbType.NVarChar, 30) { Value = usuario.CondicionIva },
                new SqlParameter("@Id", SqlDbType.Int) { Value = usuario.IdUsuario });
        }

        public void Inactivar(int idUsuario)
        {
            EjecutorDAL.EjecutarCambio("UPDATE Usuarios SET Estado = 'Inactivo' WHERE IdUsuario = @Id;", "Usuarios",
                new SqlParameter("@Id", SqlDbType.Int) { Value = idUsuario });
        }

        public void Bloquear(int idUsuario)
        {
            EjecutorDAL.EjecutarCambio(
                "UPDATE Usuarios SET Estado = 'Bloqueado', BloqueadoHasta = NULL WHERE IdUsuario = @Id;", "Usuarios",
                new SqlParameter("@Id", SqlDbType.Int) { Value = idUsuario });
        }

        public void Desbloquear(int idUsuario)
        {
            EjecutorDAL.EjecutarCambio(
                "UPDATE Usuarios SET Estado = 'Activo', IntentosFallidos = 0, BloqueadoHasta = NULL WHERE IdUsuario = @Id;", "Usuarios",
                new SqlParameter("@Id", SqlDbType.Int) { Value = idUsuario });
        }

        public UsuarioBE ObtenerParaLogin(string identificador)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = EjecutorDAL.Procedimiento("usp_Usuario_ObtenerParaLogin", conexion))
            {
                comando.Parameters.Add("@Identificador", SqlDbType.NVarChar, 100).Value = identificador;
                comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(identificador);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;
                    return new UsuarioBE
                    {
                        IdUsuario = lector.GetInt32(0),
                        Usuario = lector.GetString(1),
                        Correo = CifradoServicio.Descifrar(lector.GetString(2)),
                        ClaveHash = lector.GetString(3),
                        ClaveSalt = lector.GetString(4),
                        Idioma = lector.GetString(5),
                        IntentosFallidos = lector.GetInt32(6),
                        BloqueadoHasta = lector.IsDBNull(7) ? (DateTime?)null : lector.GetDateTime(7),
                        Estado = lector.GetString(8),
                        Rol = lector.GetString(9),
                        Nombre = lector.GetString(10),
                        Apellido = lector.GetString(11)
                    };
                }
            }
        }

        public UsuarioBE ObtenerPorCorreo(string correo)
        {
            const string sql = "SELECT TOP 1 IdUsuario, NombreUsuario, CorreoElectronico FROM Usuarios WHERE CorreoElectronico = @Correo;";
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(correo);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;
                    return new UsuarioBE
                    {
                        IdUsuario = lector.GetInt32(0),
                        Usuario = lector.GetString(1),
                        Correo = CifradoServicio.Descifrar(lector.GetString(2))
                    };
                }
            }
        }

        public string ObtenerNombreUsuario(int idUsuario)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("SELECT NombreUsuario FROM Usuarios WHERE IdUsuario = @Id;", conexion))
            {
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = idUsuario;
                conexion.Open();
                return Convert.ToString(comando.ExecuteScalar());
            }
        }

        public bool Existe(string usuario, string correo)
        {
            const string sql = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @Usuario OR CorreoElectronico = @Correo;";
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario;
                comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(correo);
                conexion.Open();
                return Convert.ToInt32(comando.ExecuteScalar()) > 0;
            }
        }

        public int Registrar(UsuarioBE usuario)
        {
            const string insertarUsuario = @"INSERT INTO Usuarios
                (NombreUsuario, CorreoElectronico, ClaveHash, ClaveSalt, Nombre, Apellido,
                 Telefono, CuitCuil, RazonSocial, NumeroLicencia, CondicionIva, Idioma, Estado)
                VALUES (@Usuario, @Correo, @Hash, @Salt, @Nombre, @Apellido, @Telefono,
                 @CuitCuil, @RazonSocial, @NumeroLicencia, @CondicionIva, 'es-ES', 'Activo');
                SELECT CONVERT(INT, SCOPE_IDENTITY());";
            const string buscarRol = "SELECT IdRol FROM Roles WHERE Nombre = @Rol;";
            const string asignarRol = "INSERT INTO UsuarioRol (IdUsuario, IdRol) VALUES (@IdUsuario, @IdRol);";

            using (var conexion = new SqlConnection(Conexion.cadena))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        EjecutorDAL.ComprobarIntegridad(conexion, transaccion, new[] { "Usuarios", "UsuarioRol", "Roles" });
                        int idUsuario;
                        using (var comando = new SqlCommand(insertarUsuario, conexion, transaccion))
                        {
                            comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario.Usuario;
                            comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 200).Value = CifradoServicio.CifrarCorreo(usuario.Correo);
                            comando.Parameters.Add("@Hash", SqlDbType.Char, 64).Value = usuario.ClaveHash;
                            comando.Parameters.Add("@Salt", SqlDbType.NVarChar, 44).Value = usuario.ClaveSalt;
                            comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 60).Value = usuario.Nombre;
                            comando.Parameters.Add("@Apellido", SqlDbType.NVarChar, 60).Value = usuario.Apellido;
                            comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 100).Value = CifradoServicio.Cifrar(usuario.Telefono);
                            comando.Parameters.Add("@CuitCuil", SqlDbType.NVarChar, 100).Value = CifradoServicio.Cifrar(usuario.CuitCuil);
                            comando.Parameters.Add("@RazonSocial", SqlDbType.NVarChar, 120).Value = (object)usuario.RazonSocial ?? DBNull.Value;
                            comando.Parameters.Add("@NumeroLicencia", SqlDbType.NVarChar, 100).Value = (object)CifradoServicio.Cifrar(usuario.NumeroLicencia) ?? DBNull.Value;
                            comando.Parameters.Add("@CondicionIva", SqlDbType.NVarChar, 30).Value = usuario.CondicionIva;
                            idUsuario = Convert.ToInt32(comando.ExecuteScalar());
                        }

                        int idRol;
                        using (var comando = new SqlCommand(buscarRol, conexion, transaccion))
                        {
                            comando.Parameters.Add("@Rol", SqlDbType.NVarChar, 50).Value = usuario.Rol;
                            var valor = comando.ExecuteScalar();
                            if (valor == null) throw new InvalidOperationException("El rol seleccionado no existe.");
                            idRol = Convert.ToInt32(valor);
                        }

                        using (var comando = new SqlCommand(asignarRol, conexion, transaccion))
                        {
                            comando.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                            comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = idRol;
                            comando.ExecuteNonQuery();
                        }

                        EjecutorDAL.RecalcularDigitos(conexion, transaccion, "Usuarios");
                        EjecutorDAL.RecalcularDigitos(conexion, transaccion, "UsuarioRol");
                        transaccion.Commit();
                        return idUsuario;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        public void ActualizarIntentos(int idUsuario, int intentos, DateTime? bloqueadoHasta)
        {
            EjecutorDAL.EjecutarCambio("usp_Usuario_ActualizarIntentos", "Usuarios",
                new SqlParameter("@Intentos", SqlDbType.Int) { Value = intentos },
                new SqlParameter("@BloqueadoHasta", SqlDbType.DateTime2) { Value = (object)bloqueadoHasta ?? DBNull.Value },
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }

        public void RestablecerClave(int idUsuario, string hash, string salt)
        {
            EjecutorDAL.EjecutarCambio("usp_Usuario_RestablecerClave", "Usuarios",
                new SqlParameter("@Hash", SqlDbType.Char, 64) { Value = hash },
                new SqlParameter("@Salt", SqlDbType.NVarChar, 44) { Value = salt },
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }
    }
}
