using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class GestionUsuariosBLL
    {
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        public IList<UsuarioBE> Listar(FiltroUsuario filtro)
        {
            return usuarios.ObtenerTodos(filtro ?? new FiltroUsuario());
        }

        public UsuarioBE Obtener(int idUsuario)
        {
            return usuarios.ObtenerPorId(idUsuario);
        }

        // Mi perfil: foto que se ve en la barra del panel. Devuelve null si salió bien, o el error.
        public string CambiarFoto(int idUsuario, string usuario, byte[] foto, string nombreArchivo)
        {
            if (!ArchivoServicio.EsJpgOPng(nombreArchivo, foto)) return "Elegí una foto JPG o PNG de hasta 2 MB.";
            ArchivoServicio.GuardarFoto(idUsuario, foto, nombreArchivo);
            bitacora.Registrar(idUsuario, usuario, "Cambio de la foto de perfil", "Informativa");
            return null;
        }

        public string RutaFoto(int idUsuario)
        {
            return ArchivoServicio.RutaFoto(idUsuario);
        }

        public string VersionFoto(int idUsuario)
        {
            return ArchivoServicio.VersionFoto(idUsuario);
        }

        // Mi perfil (CU-009-053): el propio usuario cambia sus datos de contacto. Solo se toman los campos que puede
        // editar; la razón social es del dador y la licencia, del transportista.
        public string ModificarPerfil(UsuarioBE nuevos, int idUsuario, string usuario)
        {
            var actual = usuarios.ObtenerPorId(idUsuario);
            if (actual == null) return "La cuenta ya no existe.";
            actual.Nombre = nuevos.Nombre;
            actual.Apellido = nuevos.Apellido;
            actual.Correo = nuevos.Correo;
            actual.Telefono = nuevos.Telefono;
            actual.CondicionIva = nuevos.CondicionIva;
            if (actual.Rol == "Dador de carga") actual.RazonSocial = string.IsNullOrWhiteSpace(nuevos.RazonSocial) ? null : nuevos.RazonSocial.Trim();
            if (actual.Rol == "Transportista") actual.NumeroLicencia = string.IsNullOrWhiteSpace(nuevos.NumeroLicencia) ? null : nuevos.NumeroLicencia.Trim();
            return Modificar(actual, idUsuario, usuario);
        }

        // Actualiza los datos de contacto. Devuelve null si salió bien, o el texto del error.
        public string Modificar(UsuarioBE datos, int idEjecutor, string ejecutor)
        {
            datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
            datos.Apellido = (datos.Apellido ?? string.Empty).Trim();
            datos.Correo = (datos.Correo ?? string.Empty).Trim().ToLowerInvariant();
            datos.Telefono = (datos.Telefono ?? string.Empty).Trim();

            if (datos.Nombre.Length == 0 || datos.Apellido.Length == 0
                || datos.Correo.Length == 0 || datos.Telefono.Length == 0)
                return "Completá nombre, apellido, correo y teléfono.";
            if (!Validador.EsNombre(datos.Nombre, 60) || !Validador.EsNombre(datos.Apellido, 60))
                return "El nombre y el apellido solo pueden tener letras y espacios.";
            if (!Validador.EsCorreo(datos.Correo)) return "El correo electrónico no es válido.";
            if (!Validador.EsTelefono(datos.Telefono)) return "El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15).";
            if (!Validador.EsCondicionIva(datos.CondicionIva)) return "Elegí la condición frente al IVA.";
            if (datos.RazonSocial != null && datos.RazonSocial.Length > 120) return "La razón social admite hasta 120 caracteres.";
            if (datos.NumeroLicencia != null && !Validador.EsLicencia(datos.NumeroLicencia))
                return "El número de licencia solo admite letras, números y guiones.";
            if (usuarios.CorreoEnUso(datos.Correo, datos.IdUsuario))
                return "Ese correo ya está en uso por otra cuenta.";

            usuarios.Modificar(datos);
            bitacora.Registrar(idEjecutor, ejecutor, "Modificación de datos del usuario " + datos.Usuario, "Informativa");
            return null;
        }

        public string DarDeBaja(int idUsuario, int idEjecutor, string ejecutor)
        {
            if (idUsuario == idEjecutor) return "No podés dar de baja tu propia cuenta.";
            var u = usuarios.ObtenerPorId(idUsuario);
            if (u == null) return "El usuario ya no existe.";
            if (u.Estado == "Inactivo") return "El usuario ya está inactivo.";
            // R23 (ERR025): no se da de baja a quien tiene ofertas, viajes, pagos sin liquidar o reclamos abiertos.
            if (viajes.TieneOperacionesActivas(idUsuario))
                return "El usuario tiene ofertas, viajes, pagos sin liquidar o reclamos abiertos: no se puede dar de baja hasta que terminen.";
            usuarios.Inactivar(idUsuario);
            bitacora.Registrar(idEjecutor, ejecutor, "Baja del usuario " + u.Usuario, "Crítica");
            return null;
        }

        public string Bloquear(int idUsuario, int idEjecutor, string ejecutor)
        {
            if (idUsuario == idEjecutor) return "No podés bloquear tu propia cuenta.";
            var u = usuarios.ObtenerPorId(idUsuario);
            if (u == null) return "El usuario ya no existe.";
            if (u.Estado == "Bloqueado") return "El usuario ya está bloqueado.";
            usuarios.Bloquear(idUsuario);
            bitacora.Registrar(idEjecutor, ejecutor, "Bloqueo del usuario " + u.Usuario, "Crítica");
            return null;
        }

        public string Desbloquear(int idUsuario, int idEjecutor, string ejecutor)
        {
            var u = usuarios.ObtenerPorId(idUsuario);
            if (u == null) return "El usuario ya no existe.";
            if (u.Estado == "Activo") return "El usuario ya está activo.";
            usuarios.Desbloquear(idUsuario);
            bitacora.Registrar(idEjecutor, ejecutor, "Desbloqueo del usuario " + u.Usuario, "Informativa");
            return null;
        }
    }
}
