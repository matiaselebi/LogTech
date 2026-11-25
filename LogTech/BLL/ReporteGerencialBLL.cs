using System;
using System.Collections.Generic;
using System.Text;
using LogTech.BE;
using LogTech.DAL;

namespace LogTech.BLL
{
    // CU-008-044 Generar Reporte Gerencial: arma en CSV el reporte del tablero y el de cada flota
    // (R41: se abre con Excel para analizarlo) y deja asentada la exportación en la bitácora (R1).
    public sealed class ReporteGerencialBLL
    {
        private readonly EstadisticaBLL estadisticas = new EstadisticaBLL();
        private readonly BitacoraDAL bitacora = new BitacoraDAL();

        // Devuelve null si con esos filtros no hay datos para exportar.
        public string GenerarCsv(PeriodoEstadistica periodo, string tipoSuscripcion, TipoCarga carga, int idUsuario, string usuario)
        {
            var suscripciones = estadisticas.ObtenerSuscripciones(periodo, tipoSuscripcion);
            var viajes = estadisticas.ObtenerViajes(periodo, carga);
            if (SinDatos(suscripciones) && SinDatos(viajes)) return null;

            var nombreSuscripcion = string.IsNullOrEmpty(tipoSuscripcion) ? "Todas" : tipoSuscripcion;
            var nombreCarga = carga == TipoCarga.Todas ? "Todas" : "Carga " + carga.ToString().ToLower();

            // Separador ";" porque es el que usa Excel en español.
            var csv = new StringBuilder();
            csv.AppendLine("Reporte gerencial LogTech");
            csv.AppendLine("Generado;" + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ";Usuario;" + usuario);
            csv.AppendLine("Agrupado por;" + periodo + ";Suscripciones;" + nombreSuscripcion + ";Viajes;" + nombreCarga);
            csv.AppendLine();

            var resumen = estadisticas.ObtenerResumen();
            csv.AppendLine("Resumen del mes");
            csv.AppendLine("Suscriptores activos;" + resumen.SuscriptoresActivos);
            csv.AppendLine("Ingreso mensual por suscripciones;" + resumen.IngresoMensualSuscriptores.ToString("0.##"));
            csv.AppendLine("Suscripciones nuevas del mes;" + resumen.SuscripcionesDelMes);
            csv.AppendLine("Viajes pedidos del mes;" + resumen.ViajesDelMes);
            csv.AppendLine("Plata movida en viajes del mes;" + resumen.MontoViajesDelMes.ToString("0.##"));
            csv.AppendLine("Comisiones de viajes del mes;" + resumen.ComisionesDelMes.ToString("0.##"));

            AgregarSerie(csv, "Suscripciones nuevas (" + nombreSuscripcion + ")", "Suscripciones nuevas", suscripciones);
            if (tipoSuscripcion == "Flota SaaS")
                AgregarSerie(csv, "Patentes de Flota SaaS", "Patentes registradas", estadisticas.ObtenerPatentes(periodo));
            AgregarSerie(csv, "Viajes pedidos (" + nombreCarga + ")", "Viajes pedidos", viajes);

            bitacora.Registrar(idUsuario, usuario, "Exportación del reporte gerencial (" + periodo + ")", "Informativa");
            return csv.ToString();
        }

        // CU-008-044 para el administrador de flota: el reporte de su flota en CSV. Devuelve null si
        // en el período no hay viajes entregados ni en curso.
        public string GenerarCsvFlota(int idFlota, PeriodoEstadistica periodo, int idVehiculo, string patente, string usuario)
        {
            var reporte = estadisticas.ObtenerReporteFlota(idFlota, periodo, idVehiculo);
            if (reporte.Viajes == 0 && reporte.EnCurso == 0) return null;

            var csv = new StringBuilder();
            csv.AppendLine("Reporte de la flota LogTech");
            csv.AppendLine("Generado;" + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ";Usuario;" + usuario);
            csv.AppendLine("Agrupado por;" + periodo);
            csv.AppendLine("Vehículo;" + (idVehiculo > 0 ? patente : "Toda la flota"));
            csv.AppendLine();
            csv.AppendLine("Resumen del período");
            csv.AppendLine("Viajes entregados;" + reporte.Viajes);
            csv.AppendLine("Toneladas transportadas;" + reporte.Toneladas.ToString("0.##"));
            csv.AppendLine("Kilómetros recorridos;" + reporte.Kilometros.ToString("0"));
            csv.AppendLine("Facturado a los dadores;" + reporte.Facturado.ToString("0.##"));
            csv.AppendLine("Comisión y remitos de LogTech;" + reporte.Comision.ToString("0.##"));
            csv.AppendLine("Cobrado por la flota;" + reporte.Neto.ToString("0.##"));
            csv.AppendLine("Incidencias;" + reporte.Incidencias);
            csv.AppendLine("Viajes en curso ahora;" + reporte.EnCurso);
            csv.AppendLine("Gastos del mes: Flota SaaS;" + reporte.GastoSuscripcion.ToString("0.##"));
            csv.AppendLine("Gastos del mes: seguro;" + reporte.GastoSeguro.ToString("0.##"));

            csv.AppendLine();
            csv.AppendLine("Conductores");
            csv.AppendLine("Conductor;Viajes entregados;Toneladas;Kilómetros;Cobrado;Incidencias;Viajes en curso;Último viaje");
            foreach (var fila in reporte.Conductores)
            {
                csv.AppendLine(fila.Conductor + ";" + fila.Viajes + ";" + fila.Toneladas.ToString("0.##") + ";" + fila.Kilometros.ToString("0")
                    + ";" + fila.Neto.ToString("0.##") + ";" + fila.Incidencias + ";" + fila.EnCurso + ";"
                    + (fila.UltimoViaje.HasValue ? fila.UltimoViaje.Value.ToString("dd/MM/yyyy") : string.Empty));
            }
            AgregarSerie(csv, "Viajes entregados por período", "Viajes entregados", reporte.PorPeriodo, "Cobrado por la flota");

            bitacora.Registrar(idFlota, usuario, "Exportación del reporte de la flota (" + periodo + ")", "Informativa");
            return csv.ToString();
        }

        private static void AgregarSerie(StringBuilder csv, string titulo, string columnaCantidad, IList<PuntoEstadisticaBE> serie,
            string columnaMonto = "Plata")
        {
            csv.AppendLine();
            csv.AppendLine(titulo);
            csv.AppendLine("Período;" + columnaCantidad + ";" + columnaMonto);
            foreach (var punto in serie)
                csv.AppendLine(punto.Periodo + ";" + punto.Cantidad + ";" + punto.Monto.ToString("0.##"));
        }

        private static bool SinDatos(IList<PuntoEstadisticaBE> serie)
        {
            foreach (var punto in serie)
            {
                if (punto.Cantidad > 0) return false;
            }
            return true;
        }
    }
}
