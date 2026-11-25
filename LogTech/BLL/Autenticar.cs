using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class Autenticar
    {
        private const int MaximoIntentos = 3;
        private const int MinutosBloqueo = 1;
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly IntegridadDAL integridad = new IntegridadDAL();
        private readonly MantenimientoBLL mantenimiento = new MantenimientoBLL();

        public ResultadoAutenticacionBE Ejecutar(string identificador, string clave)
        {
            // La cuenta de contingencia entra sin consultar la base, para poder restaurarla.
            if (UsuarioContingencia.Coincide(identificador, clave))
                return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.Exitoso, Usuario = UsuarioContingencia.Usuario };

            // Verifica los dígitos verificadores de toda la base antes de tocar la tabla Usuarios.
            var problemas = integridad.Verificar();
            if (problemas.Count > 0) return IngresarConBaseDaniada(identificador, clave, problemas);

            var usuario = usuarios.ObtenerParaLogin((identificador ?? string.Empty).Trim().ToLowerInvariant());
            if (usuario == null)
                return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.CredencialesInvalidas };

            if (usuario.Estado == "Inactivo")
                return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.Inactivo };

            if (usuario.Estado == "Bloqueado")
            {
                if (!usuario.BloqueadoHasta.HasValue)
                    return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.Bloqueado };

                if (usuario.BloqueadoHasta.Value > DateTime.Now)
                {
                    return new ResultadoAutenticacionBE
                    {
                        Estado = EstadoAutenticacion.Bloqueado,
                        SegundosBloqueo = Math.Max(1, (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.Now).TotalSeconds))
                    };
                }

                usuarios.ActualizarIntentos(usuario.IdUsuario, 0, null);
                usuario.IntentosFallidos = 0;
                usuario.Estado = "Activo";
            }

            if (!SeguridadServicio.VerificarClave(clave, usuario.ClaveSalt, usuario.ClaveHash))
            {
                var intentos = usuario.IntentosFallidos + 1;
                var bloqueadoHasta = intentos >= MaximoIntentos ? DateTime.Now.AddMinutes(MinutosBloqueo) : (DateTime?)null;
                usuarios.ActualizarIntentos(usuario.IdUsuario, intentos, bloqueadoHasta);
                bitacora.Registrar(usuario.IdUsuario, usuario.Usuario,
                    bloqueadoHasta.HasValue ? "Cuenta bloqueada por tres intentos fallidos" : "Intento fallido de inicio de sesión",
                    bloqueadoHasta.HasValue ? "Crítica" : "Advertencia");

                return new ResultadoAutenticacionBE
                {
                    Estado = bloqueadoHasta.HasValue ? EstadoAutenticacion.Bloqueado : EstadoAutenticacion.CredencialesInvalidas,
                    IntentosRestantes = Math.Max(0, MaximoIntentos - intentos),
                    SegundosBloqueo = bloqueadoHasta.HasValue ? MinutosBloqueo * 60 : 0
                };
            }

            if (usuario.IntentosFallidos > 0) usuarios.ActualizarIntentos(usuario.IdUsuario, 0, null);
            return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.Exitoso, Usuario = usuario };
        }

        // Con la base dañada solo entra el administrador, y solo si se puede confiar en que es él: Roles y UsuarioRol
        // tienen que estar sanos, y también su fila de Usuarios. Cualquier otro ve "el sistema no está disponible".
        // No se escribe nada en Usuarios (sumar intentos o desbloquear recalcularía los dígitos y taparía el daño); lo
        // que pasó queda en la bitácora, que firma solo su propia fila.
        private ResultadoAutenticacionBE IngresarConBaseDaniada(string identificador, string clave, IList<ProblemaIntegridadBE> problemas)
        {
            UsuarioBE usuario = null;
            try { usuario = usuarios.ObtenerParaLogin((identificador ?? string.Empty).Trim().ToLowerInvariant()); }
            catch (SqlException) { }   // la tabla Usuarios puede no responder: no hay forma de saber quién es
            var esAdministrador = usuario != null && usuario.Rol == "Administrador" && usuario.Estado != "Inactivo"
                && (usuario.Estado != "Bloqueado" || (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value <= DateTime.Now))
                && SeguridadServicio.VerificarClave(clave, usuario.ClaveSalt, usuario.ClaveHash)
                && !problemas.Any(p => p.Tabla == "Roles" || p.Tabla == "UsuarioRol"
                    || (p.Tabla == "Usuarios" && (string.IsNullOrEmpty(p.Fila) || p.Fila == "IdUsuario " + usuario.IdUsuario)));
            if (!esAdministrador)
            {
                Anotar(null, identificador, "Ingreso rechazado: el sistema no está disponible por una falla de integridad", "Advertencia");
                return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.IntegridadComprometida };
            }
            mantenimiento.RegistrarFallas(problemas, usuario.IdUsuario, usuario.Usuario);
            return new ResultadoAutenticacionBE { Estado = EstadoAutenticacion.Exitoso, Usuario = usuario, Problemas = problemas };
        }

        private void Anotar(int? idUsuario, string usuario, string descripcion, string criticidad)
        {
            try { bitacora.Registrar(idUsuario, string.IsNullOrWhiteSpace(usuario) ? "desconocido" : usuario.Trim(), descripcion, criticidad); }
            catch (SqlException) { }
        }
    }
}
