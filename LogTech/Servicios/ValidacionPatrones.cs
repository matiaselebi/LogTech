using System.Diagnostics;
using System.Linq;
using LogTech.BE;

namespace LogTech.Servicios
{
    public static class ValidacionPatrones
    {
        [Conditional("DEBUG")]
        public static void Ejecutar()
        {
            Debug.Assert(ReferenceEquals(GestorSesion.Instancia, GestorSesion.Instancia));
            var rol = new RolBE(1, "Prueba");
            rol.Agregar(new PermisoBE("Consultar"));
            Debug.Assert(rol.ObtenerPermisos().Single() == "Consultar");
            Debug.Assert(TraductorServicio.Traducir(new[] { "Hola" }, "es").Count == 0);

            var salt = SeguridadServicio.GenerarSalt();
            var hash = SeguridadServicio.CalcularHashSha256("Clave123", salt);
            Debug.Assert(SeguridadServicio.VerificarClave("Clave123", salt, hash));
            Debug.Assert(!SeguridadServicio.VerificarClave("ClaveIncorrecta", salt, hash));
            // Cifrado AES: vuelve al dato original y el mismo dato cifra siempre igual (para buscar con "=").
            Debug.Assert(CifradoServicio.Descifrar(CifradoServicio.Cifrar("20123456786")) == "20123456786");
            Debug.Assert(CifradoServicio.CifrarCorreo(" Ana@Mail.com") == CifradoServicio.Cifrar("ana@mail.com"));
            Debug.Assert(CifradoServicio.Descifrar("1144444444") == "1144444444");

            // Política: 8 o más caracteres, una mayúscula y un carácter especial.
            Debug.Assert(SeguridadServicio.ClaveCumplePolitica("Clave#123"));
            Debug.Assert(!SeguridadServicio.ClaveCumplePolitica("Clave123") && !SeguridadServicio.ClaveCumplePolitica("clave#123")
                && !SeguridadServicio.ClaveCumplePolitica("Cl#1"));

            Debug.Assert(Validador.EsCuit("20123456786") && !Validador.EsCuit("20123456783"));
            Debug.Assert(Validador.EsPatente("AB123CD") && Validador.EsPatente("ABC123") && !Validador.EsPatente("AB12CD"));
            Debug.Assert(Validador.EsTelefono("1169565548") && !Validador.EsTelefono("11 6956-5548") && !Validador.EsTelefono("123"));
            Debug.Assert(Validador.EsNombre("María José", 60) && !Validador.EsNombre("Juan2", 60));

            // Parte 1: Buenos Aires - Rosario son unos 280 km en línea recta.
            var km = UbicacionServicio.DistanciaKm(-34.6037m, -58.3816m, -32.9442m, -60.6505m);
            Debug.Assert(km > 270 && km < 290);
            Debug.Assert(!UbicacionServicio.EsValida(null, -58m) && !UbicacionServicio.EsValida(95m, 0m));
            Debug.Assert(ArchivoServicio.EsPdfOJpg("a.pdf", new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', 1 }));
            Debug.Assert(!ArchivoServicio.EsPdfOJpg("a.pdf", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));

            // Entrega final: tarjetas (Luhn y marca) y destino de los retiros.
            Debug.Assert(PasarelaPagoServicio.LuhnValido("4242424242424242") && !PasarelaPagoServicio.LuhnValido("4242424242424241"));
            Debug.Assert(PasarelaPagoServicio.Marca("5555555555554444") == "Mastercard" && PasarelaPagoServicio.Marca("378282246310005") == null);
            Debug.Assert(Validador.EsCbuOAlias("0000003100012345678901") && Validador.EsCbuOAlias("martin.transportes") && !Validador.EsCbuOAlias("123"));
        }
    }
}
