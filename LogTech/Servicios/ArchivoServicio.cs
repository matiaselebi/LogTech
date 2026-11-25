using System;
using System.IO;
using System.Web.Hosting;

namespace LogTech.Servicios
{
    // Archivos que suben los usuarios (documentación, fotos de remitos e incidencias).
    // Se guardan en App_Data/Archivos, que IIS no sirve: solo se descargan con Archivo.ashx,
    // que antes revisa si el usuario puede verlos.
    public static class ArchivoServicio
    {
        public const int TamanioMaximo = 5 * 1024 * 1024;   // 5 MB

        // R9: solo PDF o JPG. Se revisa la extensión y también los primeros bytes del archivo,
        // así un .exe renombrado a .pdf no pasa.
        public static bool EsPdfOJpg(string nombreArchivo, byte[] contenido)
        {
            if (contenido == null || contenido.Length < 4 || contenido.Length > TamanioMaximo) return false;
            var extension = (Path.GetExtension(nombreArchivo) ?? string.Empty).ToLowerInvariant();
            var esPdf = contenido[0] == '%' && contenido[1] == 'P' && contenido[2] == 'D' && contenido[3] == 'F';
            var esJpg = contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF;
            return (extension == ".pdf" && esPdf) || ((extension == ".jpg" || extension == ".jpeg") && esJpg);
        }

        // Guarda el archivo con un nombre nuevo (nunca el que mandó el usuario) y devuelve
        // la ruta relativa que va a la base, por ejemplo "Documentos/3f2a....pdf".
        public static string Guardar(byte[] contenido, string nombreArchivo, string carpeta)
        {
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant() == ".pdf" ? ".pdf" : ".jpg";
            var relativa = carpeta + "/" + Guid.NewGuid().ToString("N") + extension;
            var fisica = RutaFisica(relativa);
            Directory.CreateDirectory(Path.GetDirectoryName(fisica));
            File.WriteAllBytes(fisica, contenido);
            return relativa;
        }

        public static string RutaFisica(string relativa)
        {
            return HostingEnvironment.MapPath("~/App_Data/Archivos/" + relativa);
        }

        public static string TipoContenido(string relativa)
        {
            if (relativa.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return "application/pdf";
            return relativa.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        }

        // ---- Foto de perfil: una por usuario, en Fotos/<IdUsuario>.jpg o .png ----

        public const int TamanioMaximoFoto = 2 * 1024 * 1024;   // 2 MB
        private static readonly string[] ExtensionesFoto = { ".jpg", ".png" };

        // JPG o PNG: se revisa la extensión y también los primeros bytes, como en EsPdfOJpg.
        public static bool EsJpgOPng(string nombreArchivo, byte[] contenido)
        {
            if (contenido == null || contenido.Length < 4 || contenido.Length > TamanioMaximoFoto) return false;
            var extension = (Path.GetExtension(nombreArchivo) ?? string.Empty).ToLowerInvariant();
            var esJpg = contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF;
            var esPng = contenido[0] == 0x89 && contenido[1] == 'P' && contenido[2] == 'N' && contenido[3] == 'G';
            return ((extension == ".jpg" || extension == ".jpeg") && esJpg) || (extension == ".png" && esPng);
        }

        // La foto nueva reemplaza a la anterior (aunque tenga otra extensión).
        public static void GuardarFoto(int idUsuario, byte[] contenido, string nombreArchivo)
        {
            var fisica = RutaFisica("Fotos/" + idUsuario + (Path.GetExtension(nombreArchivo).ToLowerInvariant() == ".png" ? ".png" : ".jpg"));
            Directory.CreateDirectory(Path.GetDirectoryName(fisica));
            foreach (var extension in ExtensionesFoto) File.Delete(RutaFisica("Fotos/" + idUsuario + extension));
            File.WriteAllBytes(fisica, contenido);
        }

        // Ruta relativa de la foto, o null si el usuario no subió ninguna.
        public static string RutaFoto(int idUsuario)
        {
            foreach (var extension in ExtensionesFoto)
            {
                var relativa = "Fotos/" + idUsuario + extension;
                if (File.Exists(RutaFisica(relativa))) return relativa;
            }
            return null;
        }

        // Cambia con cada foto nueva: va en la URL para que el navegador no muestre la anterior guardada en caché.
        public static string VersionFoto(int idUsuario)
        {
            var relativa = RutaFoto(idUsuario);
            return relativa == null ? null : File.GetLastWriteTimeUtc(RutaFisica(relativa)).Ticks.ToString();
        }
    }
}
