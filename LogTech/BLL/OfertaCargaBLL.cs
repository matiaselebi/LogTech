using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Ofertas de carga (CU-001-001 a CU-001-006): publicar, modificar, dar de baja, consultar,
    // aceptar y el motor de asignación (match).
    public sealed class OfertaCargaBLL
    {
        public const decimal PorcentajeComision = 0.07m;   // la parte de cada viaje que cobra LogTech
        public const decimal CargoRemito = 1500;           // el remito digital, que paga el dador dentro del precio
        private const int ViajesSinComision = 3;           // los primeros viajes de un dador nuevo
        private const decimal PesoMaximo = 60;
        private const decimal TarifaMinimaOferta = 10000;  // para que cubra la comisión y el remito
        private const decimal TarifaMaxima = 100000000;
        private const int ResultadosMatch = 20;
        // Cotizador (WS_CotizadorFletes): distancia por ruta × ($900 + $40 por tonelada), con un mínimo por viaje.
        // Sale del costo de un semirremolque lleno: gasoil $2.075 (documento v5), ~0,35 l/km, ~40 % del costo.
        private const decimal CostoPorKm = 900;
        private const decimal CostoPorKmYTonelada = 40;
        private const decimal TarifaMinima = 250000;
        public const decimal FactorRuta = 1.2m;   // la ruta es más larga que la línea recta entre las ciudades

        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly VehiculoBLL vehiculos = new VehiculoBLL();
        private readonly ConductorBLL conductores = new ConductorBLL();
        private readonly DocumentoDAL documentos = new DocumentoDAL();
        private readonly UsuarioDAL usuarios = new UsuarioDAL();
        private readonly CiudadDAL ciudades = new CiudadDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly ViajeBLL viajeBLL = new ViajeBLL();
        private readonly BilleteraBLL billetera = new BilleteraBLL();
        private readonly SuscripcionBLL suscripciones = new SuscripcionBLL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        public List<CiudadBE> ListarCiudades()
        {
            return ciudades.Listar();
        }

        public ViajeBE Obtener(int idViaje)
        {
            return viajes.Obtener(idViaje);
        }

        // CU-001-004 (dador): sus ofertas; estado vacío = todas.
        public List<ViajeBE> ListarDelDador(int idDador, string estado)
        {
            return viajes.ListarDelDador(idDador, estado);
        }

        // CU-001-004 (transportista / flota): la bolsa de cargas pendientes.
        public List<ViajeBE> ListarBolsa(string origen, string destino, int idUsuario)
        {
            return SinComisionSiEsPremium(viajes.ListarPendientes(origen, destino, PesoMaximo), idUsuario);
        }

        // El transportista Premium no paga el 7 %: en la bolsa ve lo que cobra él.
        private List<ViajeBE> SinComisionSiEsPremium(List<ViajeBE> ofertas, int idUsuario)
        {
            if (!suscripciones.EsPremium(idUsuario)) return ofertas;
            foreach (var oferta in ofertas) oferta.Comision = 0;
            return ofertas;
        }

        // CU-001-001. Devuelve null si salió bien, o el texto del error.
        public string Publicar(ViajeBE oferta, int idDador, string usuario)
        {
            oferta.IdDador = idDador;
            var error = Validar(oferta) ?? ValidarPago(oferta, idDador);
            if (error != null) return error;
            CalcularMontos(oferta, viajes.ContarOfertasDelDador(idDador) < ViajesSinComision);
            error = ControlarSaldo(oferta, idDador);
            if (error != null) return error;
            viajes.Publicar(oferta);
            bitacora.Registrar(idDador, usuario, "Alta de oferta de carga " + oferta.Origen + " → " + oferta.Destino, "Informativa");
            AvisarPremium(oferta);
            return null;
        }

        // Premium transportista: si la tarifa supera en un 20 % o más la sugerida por el cotizador, la carga es de alta
        // rentabilidad y se les avisa a los transportistas con la membresía.
        private void AvisarPremium(ViajeBE oferta)
        {
            string error;
            var sugerida = Cotizar(oferta.Origen, oferta.Destino, oferta.PesoToneladas, out error);
            if (sugerida == null || oferta.TarifaAcordada < sugerida.Tarifa * 1.2m) return;
            var porcentaje = (int)Math.Round((oferta.TarifaAcordada / sugerida.Tarifa - 1) * 100);
            foreach (var idTransportista in suscripciones.ListarPremium())
                notificaciones.Avisar(idTransportista, "Carga de alta rentabilidad: " + oferta.Origen + " → " + oferta.Destino + ", "
                    + oferta.PesoToneladas.ToString("0.##") + " t por " + oferta.TarifaAcordada.ToString("C0") + " (" + porcentaje
                    + " % arriba de la tarifa sugerida). Buscala en la Bolsa de cargas.");
        }

        // CU-001-002.
        public string Modificar(ViajeBE oferta, int idDador, string usuario)
        {
            var actual = viajes.Obtener(oferta.IdViaje);
            if (actual == null || actual.IdDador != idDador) return "La oferta no existe.";
            oferta.IdDador = idDador;
            var error = Validar(oferta) ?? ValidarPago(oferta, idDador);
            if (error != null) return error;
            CalcularMontos(oferta, actual.DescuentoDador > 0);   // la promoción se mantiene si la tenía
            error = ControlarSaldo(oferta, idDador);
            if (error != null) return error;
            // R2 (ERR003): solo mientras nadie la tomó.
            if (actual.Estado != "Pendiente" || !viajes.Modificar(oferta))
                return "La oferta ya no está disponible: fue aceptada o dada de baja.";
            bitacora.Registrar(idDador, usuario, "Modificación de la oferta de carga N° " + oferta.IdViaje, "Informativa");
            return null;
        }

        // CU-001-003. Baja lógica: la oferta queda "Cancelado".
        public string Cancelar(int idViaje, int idDador, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdDador != idDador) return "La oferta no existe.";
            // R2 (ERR003).
            if (viaje.Estado != "Pendiente" || !viajes.CambiarEstado(idViaje, "Pendiente", "Cancelado"))
                return "La oferta ya no se puede dar de baja: fue aceptada por un transportista.";
            viaje.Estado = "Cancelado";
            viajeBLL.Notificar(viaje, "Oferta dada de baja por el dador.", null, null, usuario);
            bitacora.Registrar(idDador, usuario, "Baja de la oferta de carga N° " + idViaje, "Informativa");
            return null;
        }

        // CU-001-005. El transportista elige con qué vehículo; la flota lo asigna después (CU-002-015).
        public string Aceptar(int idViaje, int? idVehiculo, int idUsuario, string usuario)
        {
            var cuenta = usuarios.ObtenerPorId(idUsuario);
            if (cuenta == null) return "La cuenta no existe.";
            var esFlota = cuenta.Rol == "Administrador de Flota";
            var viaje = viajes.Obtener(idViaje);
            // R2 (ERR003).
            if (viaje == null || viaje.Estado != "Pendiente") return "La oferta ya no está disponible: otro transportista la aceptó o fue dada de baja.";

            // R3 (ERR005): todos los documentos obligatorios de la cuenta aprobados y ninguno vencido.
            if (documentos.TieneVencidos(idUsuario))
                return "No podés tomar viajes: tenés documentación vencida. Actualizala en Documentación.";
            foreach (var obligatorio in DocumentoBLL.ObligatoriosDeLaCuenta(cuenta.Rol))
                if (!documentos.TieneAprobado(idUsuario, obligatorio))
                    return "No podés tomar viajes: te falta la " + obligatorio + " aprobada. Cargala o esperá la revisión en Documentación.";

            if (esFlota)
            {
                // La flota asigna el camión y el conductor después (CU-002-015), pero solo toma lo que puede hacer: un
                // camión y un conductor habilitados y libres que lleven el peso.
                idVehiculo = null;
                string aviso;
                if (vehiculos.ListarParaViaje(idUsuario, viaje.PesoToneladas, null, out aviso).Count == 0)
                    return "No tenés un vehículo habilitado y libre que cargue " + viaje.PesoToneladas.ToString("0.##") + " t. " + (aviso ?? "Cargá uno en Mis vehículos.");
                if (conductores.ListarParaViaje(idUsuario, null, out aviso).Count == 0)
                    return "No tenés un conductor habilitado y libre para este viaje. " + (aviso ?? "Cargá uno en Conductores.");
            }
            else
            {
                var vehiculo = idVehiculo.HasValue ? vehiculos.Obtener(idVehiculo.Value, idUsuario) : null;
                if (vehiculo == null || vehiculo.Estado != "Activo")
                    return "Elegí uno de tus vehículos activos para hacer el viaje.";
                // Solo con la documentación del camión aprobada (semáforo en verde).
                if (vehiculo.Semaforo != "Verde") return "Tu camión " + vehiculo.Patente + " todavía no puede hacer viajes: " + vehiculo.MotivoSemaforo;
                // R6 (ERR008).
                if (vehiculo.EnViaje) return "El vehículo " + vehiculo.Patente + " ya tiene un viaje asignado o en curso.";
                if (vehiculo.CapacidadToneladas < viaje.PesoToneladas)
                    return "El vehículo " + vehiculo.Patente + " carga hasta " + vehiculo.CapacidadToneladas.ToString("0.##") + " t y la oferta pide " + viaje.PesoToneladas.ToString("0.##") + " t.";
            }

            // Con saldo: al dador le tiene que alcanzar cuando se acepta. Si no, se le avisa para que cargue.
            if (viaje.MedioPago == "Saldo" && billetera.Saldo(viaje.IdDador.Value) < viaje.PrecioDador)
            {
                notificaciones.Avisar(viaje.IdDador.Value, "Un transportista quiso tomar tu carga N° " + idViaje + ", pero no te alcanza el saldo"
                    + " de la billetera para pagarla. Cargá saldo o cambiá el medio de pago.");
                return "El dador todavía no tiene saldo para pagar este viaje. Probá con otra carga.";
            }
            // CU-005-025: si se paga con tarjeta o con saldo, se le cobra al dador y queda retenido hasta el remito.
            TarjetaBE tarjeta;
            var retencion = billetera.CobrarRetencion(viaje, out tarjeta);
            // Premium transportista: no paga la comisión del 7 % (el dador paga lo mismo; el transportista cobra más).
            var sinComision = !esFlota && suscripciones.EsPremium(idUsuario);
            if (sinComision) viaje.Comision = 0;
            // R2 otra vez, en la base: si otro la tomó un instante antes, no se pisa. Si no se guardó (otro la tomó o
            // falló la base), se devuelve lo cobrado a la tarjeta (con saldo, el movimiento no llegó a guardarse).
            bool aceptado;
            try
            {
                aceptado = viajes.Aceptar(idViaje, idUsuario, idVehiculo, retencion, sinComision);
            }
            catch (Exception)
            {
                BilleteraBLL.Devolver(tarjeta, viaje.PrecioDador);
                throw;
            }
            if (!aceptado)
            {
                BilleteraBLL.Devolver(tarjeta, viaje.PrecioDador);
                return "La oferta ya no está disponible: otro transportista la aceptó recién.";
            }
            viaje.Estado = "Asignado";
            viaje.IdTransportista = idUsuario;
            var quien = cuenta.RazonSocial ?? (cuenta.Nombre + " " + cuenta.Apellido);
            viajeBLL.Notificar(viaje, "Oferta aceptada por " + quien + ".", null, null, usuario);
            bitacora.Registrar(idUsuario, usuario, "Aceptación de la oferta de carga N° " + idViaje, "Informativa");
            return null;
        }

        // CU-001-006 (transportista / flota): ofertas que entran en sus camiones libres, de la
        // más cercana a la más lejana. aviso: ERR006 (sin ubicación) o ERR004 (sin resultados).
        public List<ViajeBE> ConsultarMatch(int idUsuario, decimal? latitud, decimal? longitud, out string aviso)
        {
            var resultado = new List<ViajeBE>();
            // R4: la ubicación del usuario es el punto de partida.
            if (!UbicacionServicio.EsValida(latitud, longitud))
            {
                aviso = "No pudimos obtener tu ubicación. Permití el acceso a la ubicación en el navegador o elegí una ciudad de referencia.";
                return resultado;
            }
            var capacidad = vehiculos.CapacidadLibreMaxima(idUsuario);
            if (capacidad == 0)
            {
                aviso = "No tenés vehículos habilitados y libres: están en viaje, su documentación no está aprobada o no cargaste ninguno (el transportista lo carga en Mi perfil; la flota, en Mis vehículos).";
                return resultado;
            }

            var coordenadas = CoordenadasPorCiudad();
            foreach (var oferta in viajes.ListarPendientes(string.Empty, string.Empty, capacidad))
            {
                CiudadBE origen;
                if (coordenadas.TryGetValue(oferta.Origen, out origen))
                    oferta.DistanciaKm = UbicacionServicio.DistanciaKm(latitud.Value, longitud.Value, origen.Latitud, origen.Longitud);
                resultado.Add(oferta);
            }
            resultado.Sort(CompararDistancia);
            if (resultado.Count > ResultadosMatch) resultado.RemoveRange(ResultadosMatch, resultado.Count - ResultadosMatch);
            aviso = resultado.Count == 0 ? "No hay cargas compatibles con tus vehículos en este momento." : null;
            return SinComisionSiEsPremium(resultado, idUsuario);
        }

        // CU-001-006 (dador): camiones libres que soportan el peso de su oferta, primero los que
        // están más cerca del origen (por su última ubicación) y después los mejor calificados.
        public List<VehiculoBE> ConsultarCamionesCompatibles(int idViaje, int idDador, out string aviso)
        {
            var resultado = new List<VehiculoBE>();
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdDador != idDador || viaje.Estado != "Pendiente")
            {
                aviso = "Elegí una de tus ofertas pendientes.";
                return resultado;
            }
            CiudadBE origen;
            if (!CoordenadasPorCiudad().TryGetValue(viaje.Origen, out origen))
            {
                aviso = "No se encontró la ubicación del origen de la carga.";
                return resultado;
            }

            foreach (var vehiculo in vehiculos.ListarCompatibles(viaje.PesoToneladas))
            {
                if (vehiculo.Latitud.HasValue)
                    vehiculo.DistanciaKm = UbicacionServicio.DistanciaKm(vehiculo.Latitud.Value, vehiculo.Longitud.Value, origen.Latitud, origen.Longitud);
                resultado.Add(vehiculo);
            }
            resultado.Sort((a, b) =>
            {
                if (a.DistanciaKm.HasValue != b.DistanciaKm.HasValue) return a.DistanciaKm.HasValue ? -1 : 1;
                if (a.DistanciaKm.HasValue) return a.DistanciaKm.Value.CompareTo(b.DistanciaKm.Value);
                return (b.Reputacion ?? 0).CompareTo(a.Reputacion ?? 0);
            });
            if (resultado.Count > ResultadosMatch) resultado.RemoveRange(ResultadosMatch, resultado.Count - ResultadosMatch);
            aviso = resultado.Count == 0 ? "No hay camiones libres que soporten " + viaje.PesoToneladas.ToString("0.##") + " t en este momento." : null;
            return resultado;
        }

        // Tarifa sugerida para un viaje (WS_CotizadorFletes y «Calcular tarifa sugerida»). Devuelve null y el error
        // si los datos no sirven. El 7 % de LogTech está incluido en la tarifa.
        public CotizacionBE Cotizar(string origen, string destino, decimal pesoToneladas, out string error)
        {
            error = null;
            var coordenadas = CoordenadasPorCiudad();
            if (origen == null || destino == null || !coordenadas.ContainsKey(origen) || !coordenadas.ContainsKey(destino)
                || origen == destino)
            {
                error = "Elegí de la lista un origen y un destino distintos.";
                return null;
            }
            if (pesoToneladas <= 0 || pesoToneladas > PesoMaximo)
            {
                error = "El peso tiene que ser mayor a 0 y de hasta " + PesoMaximo + " toneladas.";
                return null;
            }

            var desde = coordenadas[origen];
            var hasta = coordenadas[destino];
            var km = Math.Round((decimal)UbicacionServicio.DistanciaKm(desde.Latitud, desde.Longitud, hasta.Latitud, hasta.Longitud) * FactorRuta);
            var tarifa = Math.Round(km * (CostoPorKm + CostoPorKmYTonelada * pesoToneladas) / 1000) * 1000;   // redondeada a miles
            if (tarifa < TarifaMinima) tarifa = TarifaMinima;
            var comision = Math.Round(tarifa * PorcentajeComision, 2);
            return new CotizacionBE
            {
                Origen = origen,
                Destino = destino,
                PesoToneladas = pesoToneladas,
                DistanciaKm = km,
                Tarifa = tarifa,
                Comision = comision,
                CargoRemito = CargoRemito,
                NetoTransportista = tarifa - comision - CargoRemito
            };
        }

        // ERR001. Devuelve null si la oferta está bien.
        private string Validar(ViajeBE oferta)
        {
            oferta.Mercaderia = (oferta.Mercaderia ?? string.Empty).Trim();
            var coordenadas = CoordenadasPorCiudad();
            if (oferta.Origen == null || oferta.Destino == null || !coordenadas.ContainsKey(oferta.Origen)
                || !coordenadas.ContainsKey(oferta.Destino) || oferta.Origen == oferta.Destino)
                return "Elegí de la lista un origen y un destino distintos.";
            if (oferta.Mercaderia.Length < 3 || oferta.Mercaderia.Length > 120)
                return "Describí la mercadería (entre 3 y 120 caracteres).";
            if (oferta.PesoToneladas <= 0 || oferta.PesoToneladas > PesoMaximo)
                return "El peso tiene que ser mayor a 0 y de hasta " + PesoMaximo + " toneladas.";
            if (oferta.FechaCarga.Date < DateTime.Today || oferta.FechaCarga.Date > DateTime.Today.AddMonths(6))
                return "La fecha de carga tiene que ser desde hoy y dentro de los próximos 6 meses.";
            if (oferta.TarifaAcordada < TarifaMinimaOferta || oferta.TarifaAcordada > TarifaMaxima)
                return "El precio tiene que ser de al menos " + TarifaMinimaOferta.ToString("C0") + ".";
            oferta.PesoToneladas = Math.Round(oferta.PesoToneladas, 2);
            oferta.TarifaAcordada = Math.Round(oferta.TarifaAcordada, 2);
            return null;
        }

        // CU-005-025: efectivo, el saldo de la billetera o una tarjeta del dador (R21: ya la validó la pasarela al agregarla).
        private string ValidarPago(ViajeBE oferta, int idDador)
        {
            if (oferta.MedioPago == "Efectivo" || oferta.MedioPago == "Saldo")
            {
                oferta.IdTarjeta = null;
                return null;
            }
            if (oferta.MedioPago != "Tarjeta") return "Elegí cómo vas a pagar: en efectivo, con tarjeta o con el saldo de la billetera.";
            if (!oferta.IdTarjeta.HasValue || billetera.ObtenerTarjeta(oferta.IdTarjeta.Value, idDador) == null)
                return "Elegí una de tus tarjetas, o agregá una en Billetera.";
            return null;
        }

        // Con saldo, al publicar o modificar ya tiene que alcanzar para el precio (se descuenta cuando la aceptan).
        private string ControlarSaldo(ViajeBE oferta, int idDador)
        {
            if (oferta.MedioPago != "Saldo") return null;
            var saldo = billetera.Saldo(idDador);
            return saldo < oferta.PrecioDador ? BilleteraBLL.FaltaSaldo(saldo) : null;
        }

        // El precio incluye todo. LogTech cobra el 7 % y el remito; el transportista, el resto. En los 3 primeros
        // viajes de un dador nuevo el 7 % se le descuenta al dador: paga menos y el transportista cobra igual.
        private static void CalcularMontos(ViajeBE oferta, bool sinComision)
        {
            var comision = Math.Round(oferta.TarifaAcordada * PorcentajeComision, 2);
            oferta.Comision = sinComision ? 0 : comision;
            oferta.DescuentoDador = sinComision ? comision : 0;
            oferta.CargoRemito = CargoRemito;
        }

        private Dictionary<string, CiudadBE> CoordenadasPorCiudad()
        {
            var diccionario = new Dictionary<string, CiudadBE>();
            foreach (var ciudad in ciudades.Listar()) diccionario[ciudad.Nombre] = ciudad;
            return diccionario;
        }

        // Las ofertas sin distancia (origen desconocido) van al final.
        private static int CompararDistancia(ViajeBE a, ViajeBE b)
        {
            if (a.DistanciaKm.HasValue != b.DistanciaKm.HasValue) return a.DistanciaKm.HasValue ? -1 : 1;
            return a.DistanciaKm.HasValue ? a.DistanciaKm.Value.CompareTo(b.DistanciaKm.Value) : 0;
        }
    }
}
