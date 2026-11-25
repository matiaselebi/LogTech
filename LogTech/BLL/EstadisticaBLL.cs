using System;
using System.Collections.Generic;
using System.Globalization;
using LogTech.BE;
using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    // Estadísticas del tablero del administrador (el resumen del mes y las series de suscripciones,
    // patentes y viajes por día, mes o año) y el reporte de cada flota.
    public sealed class EstadisticaBLL
    {
        // Tipos de carga según el peso del viaje, en toneladas:
        // ligera hasta 3,5 t; media de 3,5 a 12 t; pesada desde 12 t.
        private const decimal TopeCargaLigera = 3.5m;
        private const decimal TopeCargaMedia = 12m;
        private const decimal PesoMaximo = 1000m;

        private readonly EstadisticaDAL estadisticas = new EstadisticaDAL();
        private readonly ViajeDAL viajes = new ViajeDAL();
        private readonly ConductorDAL conductores = new ConductorDAL();
        private readonly CiudadDAL ciudades = new CiudadDAL();
        private readonly SuscripcionDAL suscripciones = new SuscripcionDAL();
        private readonly SeguroDAL seguros = new SeguroDAL();

        public ResumenTableroBE ObtenerResumen()
        {
            var hoy = DateTime.Today;
            return estadisticas.ObtenerResumen(new DateTime(hoy.Year, hoy.Month, 1));
        }

        // tipo vacío = todas las suscripciones.
        public IList<PuntoEstadisticaBE> ObtenerSuscripciones(PeriodoEstadistica periodo, string tipo)
        {
            return Completar(estadisticas.SuscripcionesPorPeriodo(periodo, Desde(periodo), tipo ?? string.Empty), periodo);
        }

        public IList<PuntoEstadisticaBE> ObtenerViajes(PeriodoEstadistica periodo, TipoCarga carga)
        {
            decimal pesoDesde, pesoHasta;
            RangoDePeso(carga, out pesoDesde, out pesoHasta);
            return Completar(estadisticas.ViajesPorPeriodo(periodo, Desde(periodo), pesoDesde, pesoHasta), periodo);
        }

        // De dónde sale la plata que entra este mes: lo que cobra LogTech de cada tipo de suscripción (de los seguros, el
        // 5 % de la prima), las comisiones de los viajes, los remitos digitales y las comisiones del factoring. Las tarifas de los
        // viajes no se cuentan: son del transportista, LogTech solo cobra la comisión y el remito.
        public IList<TotalEstadisticaBE> ObtenerIngresosDelMes()
        {
            var ingresos = estadisticas.SuscriptoresPorTipo();
            var hoy = DateTime.Today;
            var viajesDelMes = estadisticas.TotalViajes(new DateTime(hoy.Year, hoy.Month, 1), 0, PesoMaximo);
            ingresos.Add(new TotalEstadisticaBE { Nombre = "Comisiones de viajes", Cantidad = viajesDelMes.Cantidad, Monto = viajesDelMes.Comision });
            var remitos = estadisticas.RemitosDelMes(new DateTime(hoy.Year, hoy.Month, 1));
            if (remitos.Cantidad > 0) ingresos.Add(remitos);
            var factoring = estadisticas.FactoringDelMes(new DateTime(hoy.Year, hoy.Month, 1));
            if (factoring.Cantidad > 0) ingresos.Add(factoring);
            return ingresos;
        }

        public IList<TotalEstadisticaBE> ObtenerSuscriptoresPorTipo()
        {
            return estadisticas.SuscriptoresPorTipo();
        }

        // tipo vacío = todas las suscripciones.
        public TotalEstadisticaBE ObtenerTotalSuscriptores(string tipo)
        {
            return estadisticas.TotalSuscriptores(tipo ?? string.Empty);
        }

        // Cantidad, plata movida y comisión de los viajes del período elegido.
        public TotalEstadisticaBE ObtenerTotalViajes(PeriodoEstadistica periodo, TipoCarga carga)
        {
            decimal pesoDesde, pesoHasta;
            RangoDePeso(carga, out pesoDesde, out pesoHasta);
            return estadisticas.TotalViajes(Desde(periodo), pesoDesde, pesoHasta);
        }

        // Plata movida en el período por cada tipo de carga (para el gráfico de torta).
        public IList<TotalEstadisticaBE> ObtenerViajesPorCarga(PeriodoEstadistica periodo)
        {
            var porCarga = new List<TotalEstadisticaBE>();
            var nombres = new[] { "Carga ligera", "Carga media", "Carga pesada" };
            var cargas = new[] { TipoCarga.Ligera, TipoCarga.Media, TipoCarga.Pesada };
            for (var i = 0; i < cargas.Length; i++)
            {
                var total = ObtenerTotalViajes(periodo, cargas[i]);
                total.Nombre = nombres[i];
                porCarga.Add(total);
            }
            return porCarga;
        }

        // Rango de peso de cada tipo de carga. "Todas" va de 0 al máximo.
        private static void RangoDePeso(TipoCarga carga, out decimal pesoDesde, out decimal pesoHasta)
        {
            pesoDesde = 0;
            pesoHasta = PesoMaximo;
            if (carga == TipoCarga.Ligera)
            {
                pesoHasta = TopeCargaLigera;
            }
            else if (carga == TipoCarga.Media)
            {
                pesoDesde = TopeCargaLigera;
                pesoHasta = TopeCargaMedia;
            }
            else if (carga == TipoCarga.Pesada)
            {
                pesoDesde = TopeCargaMedia;
            }
        }

        // Cuántas patentes de Flota SaaS había al final de cada período y cuánto pagaban por mes.
        public IList<PuntoEstadisticaBE> ObtenerPatentes(PeriodoEstadistica periodo)
        {
            var patentes = new List<PuntoEstadisticaBE>();
            for (var fecha = Desde(periodo); fecha <= DateTime.Today; fecha = Siguiente(fecha, periodo))
            {
                // El período en curso se mide hasta ahora; los anteriores, hasta su último día.
                var corte = Siguiente(fecha, periodo);
                if (corte > DateTime.Now) corte = DateTime.Now;

                var punto = estadisticas.PatentesActivas(corte);
                punto.Periodo = Etiqueta(fecha, periodo);
                patentes.Add(punto);
            }
            return patentes;
        }

        // CU-008-044 para el administrador de flota: los viajes que la flota entregó en el período
        // (por fecha de carga), lo que cobró y el rendimiento de cada conductor. Los kilómetros son
        // los del cotizador: línea recta entre las ciudades por el factor de ruta. idVehiculo: 0 = toda la flota, o
        // solo los viajes de ese vehículo (también sirve para el camión del transportista).
        public ReporteFlotaBE ObtenerReporteFlota(int idFlota, PeriodoEstadistica periodo, int idVehiculo)
        {
            var desde = Desde(periodo);
            var coordenadas = new Dictionary<string, CiudadBE>();
            foreach (var ciudad in ciudades.Listar()) coordenadas[ciudad.Nombre] = ciudad;
            var incidencias = estadisticas.IncidenciasPorViaje(idFlota);

            // Una fila por conductor activo, aunque no haya viajado en el período.
            var filas = new Dictionary<int, RendimientoConductorBE>();
            foreach (var conductor in conductores.Listar(idFlota, string.Empty, "Activo"))
                filas[conductor.IdConductor] = new RendimientoConductorBE { Conductor = conductor.Apellido + ", " + conductor.Nombre };

            var reporte = new ReporteFlotaBE();
            var serie = new List<PuntoEstadisticaBE>();
            foreach (var viaje in viajes.ListarDelTransportista(idFlota, true))
            {
                if (viaje.FechaCarga < desde || (idVehiculo > 0 && viaje.IdVehiculo != idVehiculo)) continue;
                var fila = FilaDe(filas, viaje);
                var km = Kilometros(coordenadas, viaje);
                var neto = viaje.NetoTransportista;
                var conIncidencias = incidencias.ContainsKey(viaje.IdViaje) ? incidencias[viaje.IdViaje] : 0;
                fila.Viajes++;
                fila.Toneladas += viaje.PesoToneladas;
                fila.Kilometros += km;
                fila.Neto += neto;
                fila.Incidencias += conIncidencias;
                if (!fila.UltimoViaje.HasValue || viaje.FechaCarga > fila.UltimoViaje.Value) fila.UltimoViaje = viaje.FechaCarga;

                reporte.Viajes++;
                reporte.Toneladas += viaje.PesoToneladas;
                reporte.Kilometros += km;
                reporte.Facturado += viaje.PrecioDador;
                reporte.Comision += viaje.ParaLogTech;
                reporte.Neto += neto;
                reporte.Incidencias += conIncidencias;

                var clave = Clave(viaje.FechaCarga, periodo);
                var punto = Buscar(serie, clave);
                if (punto == null) serie.Add(punto = new PuntoEstadisticaBE { Clave = clave });
                punto.Cantidad++;
                punto.Monto += neto;
            }
            foreach (var viaje in viajes.ListarDelTransportista(idFlota, false))
            {
                if (idVehiculo > 0 && viaje.IdVehiculo != idVehiculo) continue;
                FilaDe(filas, viaje).EnCurso++;
                reporte.EnCurso++;
                reporte.ViajeEnCurso = "N° " + viaje.IdViaje + " · " + viaje.Origen + " → " + viaje.Destino + ", " + viaje.Estado;
            }

            // Gastos del mes en LogTech: lo que paga por la Flota SaaS y por el seguro.
            var suscripcion = suscripciones.ObtenerVigente(idFlota, "Flota SaaS");
            var seguro = seguros.ObtenerActivo(idFlota);
            reporte.GastoSuscripcion = suscripcion == null ? 0 : suscripcion.Monto;
            reporte.GastoSeguro = seguro == null ? 0 : seguro.Monto;

            reporte.PorPeriodo = Completar(serie, periodo);
            var lista = new List<RendimientoConductorBE>(filas.Values);
            lista.Sort((a, b) => b.Neto.CompareTo(a.Neto));   // primero el que más cobró
            reporte.Conductores = lista;
            return reporte;
        }

        // La fila del conductor del viaje. Los viajes que la flota todavía no asignó a nadie van juntos,
        // y un conductor dado de baja aparece solo si tiene viajes en el período.
        private static RendimientoConductorBE FilaDe(Dictionary<int, RendimientoConductorBE> filas, ViajeBE viaje)
        {
            var id = viaje.IdConductor ?? 0;
            if (!filas.ContainsKey(id))
                filas[id] = new RendimientoConductorBE { Conductor = viaje.IdConductor.HasValue ? viaje.Conductor : "Sin conductor asignado" };
            return filas[id];
        }

        private static decimal Kilometros(Dictionary<string, CiudadBE> coordenadas, ViajeBE viaje)
        {
            if (!coordenadas.ContainsKey(viaje.Origen) || !coordenadas.ContainsKey(viaje.Destino)) return 0;
            var desde = coordenadas[viaje.Origen];
            var hasta = coordenadas[viaje.Destino];
            return Math.Round((decimal)UbicacionServicio.DistanciaKm(desde.Latitud, desde.Longitud, hasta.Latitud, hasta.Longitud)
                * OfertaCargaBLL.FactorRuta);
        }

        // Primer día que muestra cada gráfico: últimos 30 días, 12 meses o 5 años.
        private static DateTime Desde(PeriodoEstadistica periodo)
        {
            var hoy = DateTime.Today;
            if (periodo == PeriodoEstadistica.Diario)
                return hoy.AddDays(-29);
            if (periodo == PeriodoEstadistica.Mensual)
                return new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-11);
            return new DateTime(hoy.Year - 4, 1, 1);
        }

        // La base solo trae los períodos que tuvieron movimiento. Acá se arma la lista
        // completa, con los días o meses sin movimiento en cero, para que el gráfico no
        // los saltee.
        private static IList<PuntoEstadisticaBE> Completar(IList<PuntoEstadisticaBE> datos, PeriodoEstadistica periodo)
        {
            var completa = new List<PuntoEstadisticaBE>();
            for (var fecha = Desde(periodo); fecha <= DateTime.Today; fecha = Siguiente(fecha, periodo))
            {
                var punto = Buscar(datos, Clave(fecha, periodo)) ?? new PuntoEstadisticaBE();
                punto.Periodo = Etiqueta(fecha, periodo);
                completa.Add(punto);
            }
            return completa;
        }

        private static PuntoEstadisticaBE Buscar(IList<PuntoEstadisticaBE> datos, string clave)
        {
            foreach (var punto in datos)
            {
                if (punto.Clave == clave) return punto;
            }
            return null;
        }

        // Mismo formato que arma la base en EstadisticaDAL.Agrupar.
        private static string Clave(DateTime fecha, PeriodoEstadistica periodo)
        {
            if (periodo == PeriodoEstadistica.Diario) return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (periodo == PeriodoEstadistica.Mensual) return fecha.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            return fecha.ToString("yyyy", CultureInfo.InvariantCulture);
        }

        private static string Etiqueta(DateTime fecha, PeriodoEstadistica periodo)
        {
            if (periodo == PeriodoEstadistica.Diario) return fecha.ToString("dd/MM", CultureInfo.InvariantCulture);
            if (periodo == PeriodoEstadistica.Mensual) return fecha.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            return fecha.ToString("yyyy", CultureInfo.InvariantCulture);
        }

        private static DateTime Siguiente(DateTime fecha, PeriodoEstadistica periodo)
        {
            if (periodo == PeriodoEstadistica.Diario) return fecha.AddDays(1);
            if (periodo == PeriodoEstadistica.Mensual) return fecha.AddMonths(1);
            return fecha.AddYears(1);
        }
    }
}
