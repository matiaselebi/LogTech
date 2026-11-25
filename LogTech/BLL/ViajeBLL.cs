using System;
using System.Collections.Generic;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Viajes en curso: estados, asignación interna de la flota, incidencias y monitoreo.
    // Es el sujeto del patrón Observer: cada cambio de estado se avisa a los observadores.
    public sealed class ViajeBLL
    {
        // R17: orden obligatorio de los estados. "Entregado" llega solo con el remito (CU-004-022).
        public static readonly string[] Recorrido = { "Asignado", "En camino a carga", "En origen", "En tránsito", "En destino" };
        public static readonly string[] TiposIncidencia = { "Rotura", "Demora", "Accidente", "Clima", "Corte de ruta", "Otro" };
        // R18: si el camión no reporta hace más de estas horas, se avisa la pérdida de señal.
        private const int HorasSinSenal = 2;
        // Entrega estimada: distancia por ruta a esta velocidad promedio de un camión, con paradas.
        // ponytail: estimación simple; con tránsito real se usaría un servicio de rutas.
        private const decimal VelocidadPromedio = 60;

        private readonly List<IObservadorViaje> observadores = new List<IObservadorViaje>();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly ConductorBLL conductores = new ConductorBLL();
        private readonly VehiculoBLL vehiculos = new VehiculoBLL();
        private readonly CiudadDAL ciudades = new CiudadDAL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();

        public ViajeBLL()
        {
            Suscribir(new HistorialViajeObservador());
            Suscribir(new AvisoDadorObservador());
            Suscribir(new LiberarFondosObservador());
            Suscribir(new FacturarViajeObservador());
        }

        public void Suscribir(IObservadorViaje observador)
        {
            observadores.Add(observador);
        }

        public void Notificar(ViajeBE viaje, string novedad, decimal? latitud, decimal? longitud, string usuario)
        {
            foreach (var observador in observadores) observador.Actualizar(viaje, novedad, latitud, longitud, usuario);
        }

        public List<ViajeBE> ListarMisViajes(int idTransportista, bool entregados)
        {
            return viajes.ListarDelTransportista(idTransportista, entregados);
        }

        // Estado que sigue en el recorrido (null si ya está en destino o terminó).
        public static string Siguiente(string estado)
        {
            var posicion = Array.IndexOf(Recorrido, estado);
            return posicion < 0 || posicion == Recorrido.Length - 1 ? null : Recorrido[posicion + 1];
        }

        // CU-007-035. Devuelve null si salió bien, o el texto del error.
        public string ActualizarEstado(int idViaje, string estadoNuevo, decimal? latitud, decimal? longitud, int idUsuario, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdTransportista != idUsuario) return "El viaje no existe o no está a tu cargo.";
            if (Array.IndexOf(Recorrido, viaje.Estado) < 0) return "El viaje ya no está en curso (estado: " + viaje.Estado + ").";
            if (!viaje.IdVehiculo.HasValue) return "Asigná un conductor y un vehículo antes de iniciar el viaje.";

            // R17 (ERR019): solo se puede avanzar al estado siguiente, sin saltear ninguno.
            var siguiente = Siguiente(viaje.Estado);
            if (siguiente == null) return "El viaje ya está en destino: para cerrarlo generá el remito digital.";
            if (estadoNuevo != siguiente)
                return "No se puede pasar de \"" + viaje.Estado + "\" a \"" + estadoNuevo + "\": el siguiente estado es \"" + siguiente + "\".";

            if (!viajes.CambiarEstado(idViaje, viaje.Estado, estadoNuevo))
                return "Otro usuario actualizó el viaje recién. Volvé a cargar la pantalla.";
            viaje.Estado = estadoNuevo;
            if (!UbicacionServicio.EsValida(latitud, longitud)) latitud = longitud = null;
            Notificar(viaje, "Estado actualizado a " + estadoNuevo + ".", latitud, longitud, usuario);
            bitacora.Registrar(idUsuario, usuario, "Viaje N° " + idViaje + ": estado actualizado a " + estadoNuevo, "Informativa");
            return null;
        }

        // CU-002-015. La flota vincula un conductor y un vehículo propios a un viaje que aceptó.
        public string AsignarInterno(int idViaje, int idConductor, int idVehiculo, int idFlota, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdTransportista != idFlota) return "El viaje no existe o no es de tu flota.";
            if (viaje.Estado != "Asignado") return "El viaje ya salió (estado: " + viaje.Estado + "): no se puede cambiar la asignación.";

            var conductor = conductores.Obtener(idConductor, idFlota);
            if (conductor == null || conductor.Estado != "Activo") return "Elegí un conductor activo de tu flota.";
            // Solo con la documentación aprobada (semáforo en verde).
            if (conductor.Semaforo != "Verde") return conductor.NombreCompleto + " todavía no puede hacer viajes: " + conductor.MotivoSemaforo;
            // R8 (ERR010): el conductor no puede estar en otro viaje.
            if (conductor.EnViaje && viaje.IdConductor != idConductor)
                return "El conductor " + conductor.NombreCompleto + " ya tiene un viaje asignado o en curso. Elegí otro conductor.";

            var vehiculo = vehiculos.Obtener(idVehiculo, idFlota);
            if (vehiculo == null || vehiculo.Estado != "Activo") return "Elegí un vehículo activo de tu flota.";
            if (vehiculo.Semaforo != "Verde") return "El vehículo " + vehiculo.Patente + " todavía no puede hacer viajes: " + vehiculo.MotivoSemaforo;
            // El camión con conductor designado sale solo con ese conductor.
            if (vehiculo.IdConductorDesignado.HasValue && vehiculo.IdConductorDesignado != idConductor)
                return "El " + vehiculo.Patente + " tiene conductor designado: " + vehiculo.ConductorDesignado + ".";
            // R6 (ERR008): el vehículo no puede estar en otro viaje.
            if (vehiculo.EnViaje && viaje.IdVehiculo != idVehiculo)
                return "El vehículo " + vehiculo.Patente + " está en otro viaje. Elegí otra unidad.";
            if (vehiculo.CapacidadToneladas < viaje.PesoToneladas)
                return "El vehículo " + vehiculo.Patente + " carga hasta " + vehiculo.CapacidadToneladas.ToString("0.##") + " t y el viaje lleva " + viaje.PesoToneladas.ToString("0.##") + " t.";

            if (!viajes.AsignarInterno(idViaje, idFlota, idConductor, idVehiculo))
                return "Otro usuario actualizó el viaje recién. Volvé a cargar la pantalla.";
            Notificar(viaje, "Conductor " + conductor.NombreCompleto + " y vehículo " + vehiculo.Patente + " asignados.", null, null, usuario);
            bitacora.Registrar(idFlota, usuario, "Viaje N° " + idViaje + " asignado a " + conductor.NombreCompleto + " (" + vehiculo.Patente + ")", "Informativa");
            return null;
        }

        // CU-004-024. foto puede venir vacía (es opcional).
        public string ReportarIncidencia(int idViaje, string tipo, string descripcion, byte[] foto, string nombreFoto, int idUsuario, string usuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || viaje.IdTransportista != idUsuario) return "El viaje no existe o no está a tu cargo.";
            // R11 (ERR014): solo mientras el viaje está en marcha.
            if (Array.IndexOf(Recorrido, viaje.Estado) < 1)
                return "Solo se pueden reportar incidencias de un viaje en marcha (estado actual: " + viaje.Estado + ").";

            descripcion = (descripcion ?? string.Empty).Trim();
            if (Array.IndexOf(TiposIncidencia, tipo) < 0 || descripcion.Length < 10 || descripcion.Length > 500)
                return "Elegí el tipo de problema y describilo (entre 10 y 500 caracteres).";
            var tieneFoto = foto != null && foto.Length > 0;
            if (tieneFoto && !ArchivoServicio.EsPdfOJpg(nombreFoto, foto))
                return "La foto tiene que ser JPG (o un PDF) de hasta 5 MB.";

            var ruta = tieneFoto ? ArchivoServicio.Guardar(foto, nombreFoto, "Incidencias") : null;
            viajes.RegistrarIncidencia(idViaje, tipo, descripcion, ruta, usuario);
            if (viaje.IdDador.HasValue)
                notificaciones.Avisar(viaje.IdDador.Value, "Viaje N° " + idViaje + " (" + viaje.Origen + " → " + viaje.Destino + "): incidencia \"" + tipo + "\". " + descripcion);
            bitacora.Registrar(idUsuario, usuario, "Incidencia (" + tipo + ") en el viaje N° " + idViaje, "Advertencia");
            return null;
        }

        // Para Archivo.ashx: la foto de una incidencia, solo para el dador y el transportista del viaje.
        public string RutaFotoIncidencia(int idIncidencia, int idUsuario)
        {
            int idViaje;
            var foto = viajes.ObtenerFotoIncidencia(idIncidencia, out idViaje);
            var viaje = foto == null ? null : viajes.Obtener(idViaje);
            return viaje != null && (viaje.IdDador == idUsuario || viaje.IdTransportista == idUsuario) ? foto : null;
        }

        // CU-007-036. Devuelve null si el usuario no participa del viaje (no es el dador ni el transportista).
        public MonitoreoBE ConsultarMonitoreo(int idViaje, int idUsuario)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null || (viaje.IdDador != idUsuario && viaje.IdTransportista != idUsuario)) return null;
            return Monitorear(viaje);
        }

        // Administrador (sección Viajes): cualquier viaje, con la entrega estimada.
        public MonitoreoBE ConsultarViaje(int idViaje)
        {
            var viaje = viajes.Obtener(idViaje);
            if (viaje == null) return null;
            var monitoreo = Monitorear(viaje);
            EstimarEntrega(monitoreo);
            return monitoreo;
        }

        // Administrador: viajes que esperan transportista y viajes en curso de toda la plataforma.
        public List<ViajeBE> ListarSolicitados()
        {
            return viajes.ListarPendientes(string.Empty, string.Empty, 60);
        }

        public List<ViajeBE> ListarEnCurso()
        {
            return viajes.ListarEnCurso();
        }

        // Distancia por ruta hasta el destino a 60 km/h. En tránsito, desde la última ubicación informada; antes de
        // salir, desde el origen y el día de carga a las 8 (o ahora, si ya pasó).
        private void EstimarEntrega(MonitoreoBE monitoreo)
        {
            var viaje = monitoreo.Viaje;
            if (Array.IndexOf(Recorrido, viaje.Estado) < 0) return;
            var destino = ciudades.Listar().Find(c => c.Nombre == viaje.Destino);
            if (destino == null) return;
            var salida = viaje.FechaCarga.Date.AddHours(8);
            if (viaje.Estado == "En tránsito" || salida < DateTime.Now) salida = DateTime.Now;
            monitoreo.KmRestantes = Math.Round((decimal)UbicacionServicio.DistanciaKm(monitoreo.Latitud, monitoreo.Longitud,
                destino.Latitud, destino.Longitud) * OfertaCargaBLL.FactorRuta);
            monitoreo.EntregaEstimada = salida.AddHours((double)(monitoreo.KmRestantes / VelocidadPromedio));
        }

        // Línea de tiempo y punto del mapa (R18): la última coordenada que informó el camión, o el origen.
        private MonitoreoBE Monitorear(ViajeBE viaje)
        {
            var monitoreo = new MonitoreoBE { Viaje = viaje, Eventos = viajes.ObtenerLineaDeTiempo(viaje.IdViaje) };
            // R18: la última coordenada que reportó el camión.
            EventoViajeBE ultimo = null;
            foreach (var evento in monitoreo.Eventos)
            {
                if (evento.Latitud.HasValue) ultimo = evento;
            }

            if (ultimo == null)
            {
                foreach (var ciudad in ciudades.Listar())
                {
                    if (ciudad.Nombre != viaje.Origen) continue;
                    monitoreo.Latitud = ciudad.Latitud;
                    monitoreo.Longitud = ciudad.Longitud;
                }
                if (viaje.Estado != "Pendiente" && viaje.Estado != "Asignado")
                    monitoreo.Aviso = "El camión todavía no reportó su ubicación: el mapa muestra el origen del viaje.";
                return monitoreo;
            }

            monitoreo.Latitud = ultimo.Latitud.Value;
            monitoreo.Longitud = ultimo.Longitud.Value;
            var enMarcha = Array.IndexOf(Recorrido, viaje.Estado) > 0;
            if (enMarcha && ultimo.Fecha < DateTime.Now.AddHours(-HorasSinSenal))
                monitoreo.Aviso = "Sin señal del camión desde el " + ultimo.Fecha.ToString("dd/MM HH:mm") + ": el mapa muestra la última ubicación guardada.";
            return monitoreo;
        }
    }
}
