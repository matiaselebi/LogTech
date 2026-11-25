using System.Collections.Generic;
using LogTech.BE;
using LogTech.BLL;

namespace LogTech.Fachada
{
    // Patrón Facade para la parte de negocio (operación logística, suscripciones y viajes).
    // Igual que FachadaSeguridad: las páginas le piden todo a la fachada y no conocen la BLL.
    // Los métodos que cambian algo devuelven null si salió bien, o el texto del error.
    public class FachadaNegocio
    {
        private readonly EstadisticaBLL estadisticas = new EstadisticaBLL();
        private readonly ReporteGerencialBLL reportes = new ReporteGerencialBLL();
        private readonly BilleteraBLL billetera = new BilleteraBLL();
        private readonly DisputaBLL disputas = new DisputaBLL();
        private readonly FactoringBLL factoring = new FactoringBLL();
        private readonly SeguroBLL seguros = new SeguroBLL();
        private readonly OfertaCargaBLL ofertas = new OfertaCargaBLL();
        private readonly ViajeBLL viajes = new ViajeBLL();
        private readonly VehiculoBLL vehiculos = new VehiculoBLL();
        private readonly ConductorBLL conductores = new ConductorBLL();
        private readonly DocumentoBLL documentos = new DocumentoBLL();
        private readonly RemitoBLL remitos = new RemitoBLL();
        private readonly CalificacionBLL calificaciones = new CalificacionBLL();
        private readonly NotificacionBLL notificaciones = new NotificacionBLL();
        private readonly BitacoraBLL bitacora = new BitacoraBLL();
        private readonly ClaveApiBLL clavesApi = new ClaveApiBLL();
        private readonly FacturacionBLL facturacion = new FacturacionBLL();
        private readonly SuscripcionBLL suscripciones = new SuscripcionBLL();

        // ---- Ofertas de carga (CU-001-001 a CU-001-006) ----

        public List<CiudadBE> ListarCiudades()
        {
            return ofertas.ListarCiudades();
        }

        public ViajeBE ObtenerViaje(int idViaje)
        {
            return ofertas.Obtener(idViaje);
        }

        public List<ViajeBE> ListarOfertasDelDador(int idDador, string estado)
        {
            return ofertas.ListarDelDador(idDador, estado);
        }

        public List<ViajeBE> ListarBolsaDeCargas(string origen, string destino, int idUsuario)
        {
            return ofertas.ListarBolsa(origen, destino, idUsuario);
        }

        // Cotizador de fletes (WS_CotizadorFletes y «Calcular tarifa sugerida»).
        public CotizacionBE CotizarFlete(string origen, string destino, decimal pesoToneladas, out string error)
        {
            return ofertas.Cotizar(origen, destino, pesoToneladas, out error);
        }

        // ---- Clave de API del dador (Mi perfil) y seguimiento por servicio web (WS_TrackingViajes) ----

        public ClaveApiBE ObtenerClaveApi(int idUsuario)
        {
            return clavesApi.ObtenerActiva(idUsuario);
        }

        public string GenerarClaveApi(int idUsuario, string usuario)
        {
            return clavesApi.Generar(idUsuario, usuario);
        }

        public bool RevocarClaveApi(int idUsuario, string usuario)
        {
            return clavesApi.Revocar(idUsuario, usuario);
        }

        public SeguimientoBE ConsultarSeguimiento(string claveApi, int idViaje, out string error)
        {
            return clavesApi.ConsultarSeguimiento(claveApi, idViaje, out error);
        }

        public string PublicarOferta(ViajeBE oferta, int idDador, string usuario)
        {
            return ofertas.Publicar(oferta, idDador, usuario);
        }

        public string ModificarOferta(ViajeBE oferta, int idDador, string usuario)
        {
            return ofertas.Modificar(oferta, idDador, usuario);
        }

        public string CancelarOferta(int idViaje, int idDador, string usuario)
        {
            return ofertas.Cancelar(idViaje, idDador, usuario);
        }

        public string AceptarOferta(int idViaje, int? idVehiculo, int idUsuario, string usuario)
        {
            return ofertas.Aceptar(idViaje, idVehiculo, idUsuario, usuario);
        }

        public List<ViajeBE> ConsultarMatch(int idUsuario, decimal? latitud, decimal? longitud, out string aviso)
        {
            return ofertas.ConsultarMatch(idUsuario, latitud, longitud, out aviso);
        }

        public List<VehiculoBE> ConsultarCamionesCompatibles(int idViaje, int idDador, out string aviso)
        {
            return ofertas.ConsultarCamionesCompatibles(idViaje, idDador, out aviso);
        }

        // ---- Viajes en curso (CU-002-015, CU-004-024, CU-007-035, CU-007-036) ----

        // R17: estados en orden, desde "Asignado" hasta "En destino".
        public string[] EstadosDelRecorrido()
        {
            return ViajeBLL.Recorrido;
        }

        public string SiguienteEstado(string estado)
        {
            return ViajeBLL.Siguiente(estado);
        }

        public string[] TiposDeIncidencia()
        {
            return ViajeBLL.TiposIncidencia;
        }

        public string[] TiposDeAcoplado()
        {
            return VehiculoBLL.TiposAcoplado;
        }

        public List<ViajeBE> ListarMisViajes(int idTransportista, bool entregados)
        {
            return viajes.ListarMisViajes(idTransportista, entregados);
        }

        public string ActualizarEstadoViaje(int idViaje, string estadoNuevo, decimal? latitud, decimal? longitud, int idUsuario, string usuario)
        {
            return viajes.ActualizarEstado(idViaje, estadoNuevo, latitud, longitud, idUsuario, usuario);
        }

        public string AsignarViajeInterno(int idViaje, int idConductor, int idVehiculo, int idFlota, string usuario)
        {
            return viajes.AsignarInterno(idViaje, idConductor, idVehiculo, idFlota, usuario);
        }

        public string ReportarIncidencia(int idViaje, string tipo, string descripcion, byte[] foto, string nombreFoto, int idUsuario, string usuario)
        {
            return viajes.ReportarIncidencia(idViaje, tipo, descripcion, foto, nombreFoto, idUsuario, usuario);
        }

        public MonitoreoBE ConsultarMonitoreo(int idViaje, int idUsuario)
        {
            return viajes.ConsultarMonitoreo(idViaje, idUsuario);
        }

        // ---- Vehículos (CU-002-007 a CU-002-010) ----

        public List<VehiculoBE> ListarVehiculos(int idPropietario, string texto, string estado)
        {
            return vehiculos.Listar(idPropietario, texto, estado);
        }

        public VehiculoBE ObtenerVehiculo(int idVehiculo, int idPropietario)
        {
            return vehiculos.Obtener(idVehiculo, idPropietario);
        }

        // Los vehículos que pueden hacer un viaje de ese peso (aviso: por qué no aparecen los demás).
        public List<VehiculoBE> ListarVehiculosParaViaje(int idPropietario, decimal peso, int? idActual, out string aviso)
        {
            return vehiculos.ListarParaViaje(idPropietario, peso, idActual, out aviso);
        }

        public string AltaVehiculo(VehiculoBE vehiculo, List<DocumentoBE> documentosDelVehiculo, int idUsuario, string usuario)
        {
            return vehiculos.Alta(vehiculo, documentosDelVehiculo, idUsuario, usuario);
        }

        // El texto del pop-up de «Guardar vehículo»: a la flota le dice cuánto se le cobra.
        public string AvisoAltaVehiculo(int idUsuario, string rol)
        {
            return suscripciones.AvisoAltaVehiculo(idUsuario, rol);
        }

        public string DesignarConductor(int idVehiculo, int? idConductor, int idFlota, string usuario)
        {
            return vehiculos.DesignarConductor(idVehiculo, idConductor, idFlota, usuario);
        }

        public string ModificarVehiculo(VehiculoBE vehiculo, int idUsuario, string usuario)
        {
            return vehiculos.Modificar(vehiculo, idUsuario, usuario);
        }

        public string BajaVehiculo(int idVehiculo, int idUsuario, string usuario)
        {
            return vehiculos.Baja(idVehiculo, idUsuario, usuario);
        }

        // ---- Conductores de la flota (CU-002-011 a CU-002-014) ----

        public List<ConductorBE> ListarConductores(int idFlota, string texto, string estado)
        {
            return conductores.Listar(idFlota, texto, estado);
        }

        public ConductorBE ObtenerConductor(int idConductor, int idFlota)
        {
            return conductores.Obtener(idConductor, idFlota);
        }

        // Los conductores que pueden hacer un viaje (aviso: por qué no aparecen los demás).
        public List<ConductorBE> ListarConductoresParaViaje(int idFlota, int? idActual, out string aviso)
        {
            return conductores.ListarParaViaje(idFlota, idActual, out aviso);
        }

        public string AltaConductor(ConductorBE conductor, List<DocumentoBE> documentosDelConductor, int idFlota, string usuario)
        {
            return conductores.Alta(conductor, documentosDelConductor, idFlota, usuario);
        }

        public string ModificarConductor(ConductorBE conductor, int idFlota, string usuario)
        {
            return conductores.Modificar(conductor, idFlota, usuario);
        }

        public string BajaConductor(int idConductor, int idFlota, string usuario)
        {
            return conductores.Baja(idConductor, idFlota, usuario);
        }

        // ---- Documentación (CU-003-016 a CU-003-021) ----

        public string[] TiposDeDocumento(string rol)
        {
            return DocumentoBLL.TiposDelRol(rol);
        }

        // Los que tiene que tener aprobados cada vehículo y cada conductor.
        public string[] DocumentosDelVehiculo()
        {
            return DocumentoBLL.DelVehiculo;
        }

        public string[] DocumentosDelConductor()
        {
            return DocumentoBLL.DelConductor;
        }

        public List<DocumentoBE> ListarMisDocumentos(int idUsuario)
        {
            return documentos.ListarDelUsuario(idUsuario);
        }

        public List<DocumentoBE> ListarDocumentosParaValidar(string estado)
        {
            return documentos.ListarTodos(estado);
        }

        public string ObtenerSemaforoDocumentacion(int idUsuario, string rol, out string detalle)
        {
            return documentos.ObtenerSemaforo(idUsuario, rol, out detalle);
        }

        public string AltaDocumento(DocumentoBE documento, int idUsuario, string usuario)
        {
            return documentos.Alta(documento, idUsuario, usuario);
        }

        public string ModificarDocumento(DocumentoBE documento, int idUsuario, string usuario)
        {
            return documentos.Modificar(documento, idUsuario, usuario);
        }

        public string BajaDocumento(int idDocumento, int idUsuario, string usuario)
        {
            return documentos.Baja(idDocumento, idUsuario, usuario);
        }

        public string ValidarDocumento(int idDocumento, bool aprobar, string motivo, int idAnalista, string usuario)
        {
            return documentos.Validar(idDocumento, aprobar, motivo, idAnalista, usuario);
        }

        // CU-003-021: la llama la rutina diaria de Global.asax.
        public void ProcesarVencimientos()
        {
            documentos.ProcesarVencimientos();
            seguros.ProcesarVencimientos();
            suscripciones.ProcesarVencimientos();
            facturacion.ReintentarPendientes();
        }

        // ---- Remitos (CU-004-022 y CU-004-023) ----

        public byte[] ExportarRemitoXml(int idViaje, int idUsuario, string usuario)
        {
            return remitos.ExportarXml(idViaje, idUsuario, usuario);
        }

        public string ImprimirRemito(int idViaje, int idUsuario, string usuario)
        {
            return remitos.Imprimir(idViaje, idUsuario, usuario);
        }

        public string AltaRemito(int idViaje, string receptor, string dni, byte[] foto, string nombreFoto, int idUsuario, string usuario)
        {
            return remitos.Alta(idViaje, receptor, dni, foto, nombreFoto, idUsuario, usuario);
        }

        public RemitoBE ConsultarRemito(int idViaje, int idUsuario)
        {
            return remitos.ConsultarDelViaje(idViaje, idUsuario);
        }

        public List<RemitoBE> ListarRemitos(int idUsuario, string texto)
        {
            return remitos.Listar(idUsuario, texto);
        }

        // ---- Calificaciones (CU-007-037) ----

        public string CalificarViaje(int idViaje, int puntaje, string resena, int idDador, string usuario)
        {
            return calificaciones.Calificar(idViaje, puntaje, resena, idDador, usuario);
        }

        // ---- Avisos del panel, bitácora de consultas y archivos ----

        public List<NotificacionBE> ListarNotificaciones(int idUsuario)
        {
            return notificaciones.ListarUltimas(idUsuario);
        }

        public void MarcarNotificacionesLeidas(int idUsuario)
        {
            notificaciones.MarcarLeidas(idUsuario);
        }

        // R1 de las consultas: la pantalla avisa qué consultó el usuario.
        public void RegistrarConsulta(int idUsuario, string usuario, string consulta)
        {
            bitacora.Registrar(idUsuario, usuario, consulta, "Informativa");
        }

        // Archivo.ashx: ruta del archivo pedido si el usuario puede verlo; null si no.
        // tipo: "documento" (id = IdDocumento), "remito" (id = IdViaje) o "incidencia" (id = IdIncidencia).
        public string ObtenerArchivo(string tipo, int id, int idUsuario, bool esAnalista)
        {
            if (tipo == "documento") return documentos.RutaArchivo(id, idUsuario, esAnalista);
            if (tipo == "remito")
            {
                var remito = remitos.ConsultarDelViaje(id, idUsuario);
                return remito == null ? null : remito.Foto;
            }
            if (tipo == "incidencia") return viajes.RutaFotoIncidencia(id, idUsuario);
            return null;
        }

        // ---- Tablero del administrador ----

        public ResumenTableroBE ObtenerResumenTablero()
        {
            return estadisticas.ObtenerResumen();
        }

        public IList<TotalEstadisticaBE> ObtenerIngresosDelMes()
        {
            return estadisticas.ObtenerIngresosDelMes();
        }

        public IList<TotalEstadisticaBE> ObtenerSuscriptoresPorTipo()
        {
            return estadisticas.ObtenerSuscriptoresPorTipo();
        }

        public TotalEstadisticaBE ObtenerTotalSuscriptores(string tipo)
        {
            return estadisticas.ObtenerTotalSuscriptores(tipo);
        }

        public TotalEstadisticaBE ObtenerTotalViajes(PeriodoEstadistica periodo, TipoCarga carga)
        {
            return estadisticas.ObtenerTotalViajes(periodo, carga);
        }

        public IList<TotalEstadisticaBE> ObtenerViajesPorCarga(PeriodoEstadistica periodo)
        {
            return estadisticas.ObtenerViajesPorCarga(periodo);
        }

        // tipo vacío = todas las suscripciones.
        public IList<PuntoEstadisticaBE> ObtenerEstadisticaSuscripciones(PeriodoEstadistica periodo, string tipo)
        {
            return estadisticas.ObtenerSuscripciones(periodo, tipo);
        }

        public IList<PuntoEstadisticaBE> ObtenerEstadisticaPatentes(PeriodoEstadistica periodo)
        {
            return estadisticas.ObtenerPatentes(periodo);
        }

        public IList<PuntoEstadisticaBE> ObtenerEstadisticaViajes(PeriodoEstadistica periodo, TipoCarga carga)
        {
            return estadisticas.ObtenerViajes(periodo, carga);
        }

        // CU-008-044: devuelve el reporte en CSV, o null si no hay datos con esos filtros.
        public string ExportarReporteGerencial(PeriodoEstadistica periodo, string tipoSuscripcion, TipoCarga carga, int idUsuario, string usuario)
        {
            return reportes.GenerarCsv(periodo, tipoSuscripcion, carga, idUsuario, usuario);
        }

        // CU-008-044 para el administrador de flota: el reporte de su flota o de uno de sus vehículos (idVehiculo 0 = toda
        // la flota). También lo usa «Ver detalles» del vehículo.
        public ReporteFlotaBE ObtenerReporteFlota(int idFlota, PeriodoEstadistica periodo, int idVehiculo)
        {
            return estadisticas.ObtenerReporteFlota(idFlota, periodo, idVehiculo);
        }

        // Devuelve el reporte de la flota en CSV, o null si en el período no hay viajes.
        public string ExportarReporteFlota(int idFlota, PeriodoEstadistica periodo, int idVehiculo, string patente, string usuario)
        {
            return reportes.GenerarCsvFlota(idFlota, periodo, idVehiculo, patente, usuario);
        }

        // ---- Billetera y escrow (CU-005-025 y CU-005-026) ----

        public BilleteraBE ObtenerBilletera(int idUsuario)
        {
            return billetera.ObtenerResumen(idUsuario);
        }

        public List<TransaccionBE> ListarMovimientos(int idUsuario)
        {
            return billetera.ListarMovimientos(idUsuario);
        }

        public List<TarjetaBE> ListarTarjetas(int idUsuario)
        {
            return billetera.ListarTarjetas(idUsuario);
        }

        public string AgregarTarjeta(int idUsuario, string usuario, string titular, string numero, string vencimiento, string codigo)
        {
            return billetera.AgregarTarjeta(idUsuario, usuario, titular, numero, vencimiento, codigo);
        }

        public string QuitarTarjeta(int idTarjeta, int idUsuario, string usuario)
        {
            return billetera.QuitarTarjeta(idTarjeta, idUsuario, usuario);
        }

        public string CargarSaldo(int idUsuario, string usuario, int idTarjeta, decimal monto)
        {
            return billetera.CargarSaldo(idUsuario, usuario, idTarjeta, monto);
        }

        public string RetirarSaldo(int idUsuario, string usuario, decimal monto, string cbuOAlias)
        {
            return billetera.Retirar(idUsuario, usuario, monto, cbuOAlias);
        }

        public string ObtenerCbuDeposito(int idUsuario)
        {
            return billetera.ObtenerCbuDeposito(idUsuario);
        }

        public string GuardarCbuDeposito(int idUsuario, string usuario, string cbuOAlias)
        {
            return billetera.GuardarCbuDeposito(idUsuario, usuario, cbuOAlias);
        }

        public string EliminarCbuDeposito(int idUsuario, string usuario)
        {
            return billetera.EliminarCbuDeposito(idUsuario, usuario);
        }

        // ---- Efectivo en punto de pago y factoring (CU-006-029 y CU-006-030) ----

        public List<ViajeBE> ListarViajesAPagar(int idDador)
        {
            return billetera.ListarAPagar(idDador);
        }

        public string PagarEnPuntoDePago(int idViaje, int idDador, string usuario)
        {
            return billetera.PagarEnPuntoDePago(idViaje, idDador, usuario, false);
        }

        public string PagarViajeConSaldo(int idViaje, int idDador, string usuario)
        {
            return billetera.PagarEnPuntoDePago(idViaje, idDador, usuario, true);
        }

        public CotizacionFactoringBE CotizarFactoring(decimal monto)
        {
            return factoring.Cotizar(monto);
        }

        public PuntajeFactoringBE ObtenerPuntajeFactoring(int idTransportista)
        {
            return factoring.CalcularPuntaje(idTransportista);
        }

        public List<ViajeBE> ListarViajesAdelantables(int idTransportista)
        {
            return factoring.ListarAdelantables(idTransportista);
        }

        public List<SolicitudFactoringBE> ListarSolicitudesFactoring(int idTransportista)
        {
            return factoring.ListarSolicitudes(idTransportista);
        }

        public string SolicitarFactoring(int idViaje, int idTransportista, string usuario)
        {
            return factoring.Solicitar(idViaje, idTransportista, usuario);
        }

        // ---- Seguros (CU-006-031 a CU-006-034) ----

        public List<string> ListarAseguradoras()
        {
            return seguros.ListarAseguradoras();
        }

        // CU-006-032: cambiar el plan del seguro activo.
        public string CambiarPlanSeguro(int idUsuario, string usuario, int idPlan)
        {
            return seguros.CambiarPlan(idUsuario, usuario, idPlan);
        }

        public decimal CalcularAjusteCambioSeguro(int idUsuario, int idPlan)
        {
            return seguros.CalcularAjusteCambio(idUsuario, idPlan);
        }

        public List<PlanSeguroBE> ListarPlanesSeguro(string aseguradora)
        {
            return seguros.ListarPlanes(aseguradora);
        }

        public PlanSeguroBE ObtenerPlanSeguro(int idPlan)
        {
            return seguros.ObtenerPlan(idPlan);
        }

        public SeguroBE ObtenerSeguroActivo(int idUsuario)
        {
            return seguros.ObtenerActivo(idUsuario);
        }

        public List<SeguroBE> ListarSeguros(int idUsuario)
        {
            return seguros.ListarHistorial(idUsuario);
        }

        public string ContratarSeguro(int idUsuario, string usuario, int idPlan, int idTarjeta)
        {
            return seguros.Contratar(idUsuario, usuario, idPlan, idTarjeta);
        }

        public string CancelarSeguro(int idUsuario, string usuario)
        {
            return seguros.Cancelar(idUsuario, usuario);
        }

        // ---- Supervisión del administrador (sección Viajes) ----

        public TotalEstadisticaBE ObtenerEscrowTotal()
        {
            return billetera.ObtenerEscrowTotal();
        }

        public List<ViajeBE> ListarViajesSolicitados()
        {
            return viajes.ListarSolicitados();
        }

        public List<ViajeBE> ListarViajesEnCurso()
        {
            return viajes.ListarEnCurso();
        }

        public MonitoreoBE ConsultarViajeAdministrador(int idViaje)
        {
            return viajes.ConsultarViaje(idViaje);
        }

        // ---- Disputas comerciales (CU-005-027) ----

        // idDador null = todas (el administrador).
        public List<DisputaBE> ListarDisputas(int? idDador)
        {
            return disputas.Listar(idDador);
        }

        public List<ViajeBE> ListarViajesReclamables(int idDador)
        {
            return disputas.ListarReclamables(idDador);
        }

        public string AbrirDisputa(int idViaje, int idDador, string usuario, string motivo)
        {
            return disputas.Abrir(idViaje, idDador, usuario, motivo);
        }

        public string ResolverDisputa(int idDisputa, bool devolverAlDador, string comentario, int idAdmin, string admin)
        {
            return disputas.Resolver(idDisputa, devolverAlDador, comentario, idAdmin, admin);
        }

        // ---- Suscripciones Flota SaaS y Premium (CU-008-040 a 043) ----

        public SuscripcionBE ObtenerSuscripcion(int idUsuario, string rol)
        {
            return suscripciones.ObtenerVigente(idUsuario, rol);
        }

        // Lo que se paga por mes: la Flota SaaS por vehículo, la Premium un precio fijo.
        public decimal CalcularMontoSuscripcion(string rol, int vehiculos)
        {
            return SuscripcionBLL.CalcularMonto(SuscripcionBLL.TipoDelRol(rol), vehiculos);
        }

        public int ContarVehiculosActivos(int idUsuario)
        {
            return suscripciones.VehiculosActivos(idUsuario);
        }

        // Cuántos vehículos se cobrarían al contratar la Flota SaaS ahora: los activos, como mínimo uno.
        public int VehiculosACobrar(int idUsuario)
        {
            return suscripciones.VehiculosACobrar(idUsuario);
        }

        public string ContratarSuscripcion(int idUsuario, string usuario, string rol, int idTarjeta)
        {
            return suscripciones.Contratar(idUsuario, usuario, rol, idTarjeta);
        }

        // Cambia con qué se paga (una tarjeta o el saldo, idTarjeta 0).
        public string ModificarSuscripcion(int idUsuario, string usuario, string rol, int idTarjeta)
        {
            return suscripciones.Modificar(idUsuario, usuario, rol, idTarjeta);
        }

        public string CancelarSuscripcion(int idUsuario, string usuario, string rol, string motivo)
        {
            return suscripciones.Cancelar(idUsuario, usuario, rol, motivo);
        }

        // ---- Facturación (CU-008-039) ----

        public List<FacturaBE> ListarMisFacturas(int idUsuario)
        {
            return facturacion.ListarDelUsuario(idUsuario);
        }

        // Para el administrador: las facturas del mes.
        public List<FacturaBE> ListarFacturasDelMes(System.DateTime mes)
        {
            return facturacion.ListarDelMes(mes);
        }

        // El PDF de la factura (null si no existe, no está emitida o no es del usuario).
        public byte[] ObtenerFacturaPdf(int idFactura, int idUsuario, bool esAdministrador, string usuario)
        {
            return facturacion.ObtenerPdf(idFactura, idUsuario, esAdministrador, usuario);
        }

        // ReporteFacturacion.xml del mes (null si no hay facturas).
        public byte[] ExportarReporteFacturacion(System.DateTime mes, int idUsuario, string usuario)
        {
            return facturacion.ExportarXml(mes, idUsuario, usuario);
        }

        // ERR022: vuelve a pedir el CAE de las facturas pendientes (también lo hace la rutina diaria).
        public void ReintentarFacturasPendientes()
        {
            facturacion.ReintentarPendientes();
        }
    }
}
