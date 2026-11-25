<%@ Page Title="Fletes simples, seguros y conectados" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LogTech._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="hero-section">
            <div class="hero-grid" aria-hidden="true"></div>
            <div class="container hero-content">
                <div class="row align-items-center g-5">
                    <div class="col-lg-6">
                        <span class="eyebrow"><span class="status-dot"></span> Logística inteligente para Argentina</span>
                        <h1>Tu carga llega.<br /><span>Tu negocio avanza.</span></h1>
                        <p class="hero-copy">Conectamos empresas que necesitan transportar mercadería con transportistas verificados. Publicá, asigná y seguí cada viaje desde un solo lugar.</p>
                        <div class="hero-actions">
                            <a class="btn btn-brand btn-lg" href="Login.aspx">Necesito un flete <span aria-hidden="true">→</span></a>
                            <a class="btn btn-ghost btn-lg" href="Login.aspx">Soy transportista</a>
                        </div>
                        <div class="hero-proof" aria-label="Beneficios destacados">
                            <span><strong>24/7</strong> monitoreo</span>
                            <span><strong>100%</strong> trazable</span>
                            <span><strong>Escrow</strong> protegido</span>
                        </div>
                    </div>
                    <div class="col-lg-6">
                        <div class="route-card">
                            <div class="route-card-head"><div><span class="live-pill">En tránsito</span><strong>Viaje #LT-2847</strong></div><span class="route-eta">Llega 16:40</span></div>
                            <div class="map-canvas">
                                <svg viewBox="0 0 620 320" role="img" aria-label="Ruta ilustrativa entre Buenos Aires y Rosario">
                                    <defs><pattern id="grid" width="32" height="32" patternUnits="userSpaceOnUse"><path d="M 32 0 L 0 0 0 32" fill="none" stroke="currentColor" stroke-width="1"/></pattern></defs>
                                    <rect width="620" height="320" fill="url(#grid)" />
                                    <path class="map-road" d="M38 246C113 216 158 270 224 222S309 112 377 143s108 5 198-74" />
                                    <path class="map-route" d="M67 237C128 222 167 250 224 212S312 120 378 151s106 4 164-58" />
                                    <circle class="map-origin" cx="67" cy="237" r="10" /><circle class="map-destination" cx="542" cy="93" r="10" />
                                    <g class="truck-pin" transform="translate(297 151)"><circle r="25"/><path d="M-13-7h17v14h-17zm17 4h8l6 7v3h-14zm-10 14a4 4 0 1 0 0 8 4 4 0 0 0 0-8zm20 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8z"/></g>
                                </svg>
                                <div class="map-label map-label-start"><span></span> Buenos Aires</div><div class="map-label map-label-end"><span></span> Rosario</div>
                            </div>
                            <div class="route-details"><div><span>Transportista</span><strong>Martín R. · 4,9 ★</strong></div><div><span>Unidad</span><strong>AE 482 QJ</strong></div><div><span>Carga</span><strong>12.500 kg</strong></div></div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <section id="como-funciona" class="section-space how-section">
            <div class="container">
                <div class="section-heading text-center"><span class="eyebrow eyebrow-light">Simple de principio a fin</span><h2>Un flete resuelto en tres pasos</h2><p>Menos llamadas y planillas. Más control sobre cada operación.</p></div>
                <div class="row g-4 process-row">
                    <div class="col-md-4"><article class="process-card"><span>01</span><div class="process-icon">⌖</div><h3>Publicá tu carga</h3><p>Indicá origen, destino, fecha, tipo de mercadería y vehículo necesario.</p></article></div>
                    <div class="col-md-4"><article class="process-card featured"><span>02</span><div class="process-icon">⇄</div><h3>Encontrá el transporte</h3><p>Recibí propuestas o asigná un transportista verificado de tu propia flota.</p></article></div>
                    <div class="col-md-4"><article class="process-card"><span>03</span><div class="process-icon">✓</div><h3>Seguí y cobrá seguro</h3><p>Monitoreá el viaje, digitalizá el remito y operá con fondos protegidos.</p></article></div>
                </div>
            </div>
        </section>

        <section id="perfiles" class="section-space profiles-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Una plataforma, tres experiencias</span><h2>Elegí cómo usás LogTech</h2><p>Conocé las herramientas pensadas para cada tipo de usuario.</p></div>
                <ul class="nav role-tabs" id="roleTabs" role="tablist">
                    <li class="nav-item" role="presentation"><button class="nav-link active" id="dador-tab" data-bs-toggle="tab" data-bs-target="#dador" type="button" role="tab" aria-controls="dador" aria-selected="true">Dador de carga</button></li>
                    <li class="nav-item" role="presentation"><button class="nav-link" id="transportista-tab" data-bs-toggle="tab" data-bs-target="#transportista" type="button" role="tab" aria-controls="transportista" aria-selected="false">Transportista</button></li>
                    <li class="nav-item" role="presentation"><button class="nav-link" id="flota-tab" data-bs-toggle="tab" data-bs-target="#flota" type="button" role="tab" aria-controls="flota" aria-selected="false">Administrador de flota</button></li>
                </ul>
                <div class="tab-content role-content" id="roleTabsContent">
                    <div class="tab-pane fade show active" id="dador" role="tabpanel" aria-labelledby="dador-tab" tabindex="0">
                        <div class="role-intro"><div><span class="role-kicker">Para empresas y particulares</span><h3>Mové mercadería con visibilidad total</h3><p>Publicá necesidades de transporte, compará ofertas y controlá la entrega.</p></div><a class="btn btn-brand" href="Login.aspx">Ingresar como dador</a></div>
                        <div class="module-grid"><div class="module-card"><span>01</span><h4>Cuenta</h4><p>Perfil, documentación, billetera, pagos y disputas.</p></div><div class="module-card"><span>02</span><h4>Contratación</h4><p>Suscripción y seguro de mercadería.</p></div><div class="module-card accent"><span>03</span><h4>Viajes</h4><p>Publicar carga, ofertas, seguimiento, entregas e historial.</p></div></div>
                    </div>
                    <div class="tab-pane fade" id="transportista" role="tabpanel" aria-labelledby="transportista-tab" tabindex="0">
                        <div class="role-intro"><div><span class="role-kicker">Para transportistas independientes</span><h3>Encontrá viajes y reducí kilómetros vacíos</h3><p>Tomá cargas compatibles con tu unidad y administrá toda la operación móvil.</p></div><a class="btn btn-brand" href="Login.aspx">Ingresar como transportista</a></div>
                        <div class="module-grid"><div class="module-card"><span>01</span><h4>Legajo digital</h4><p>Licencia, CNRT, VTV y seguro siempre vigentes.</p></div><div class="module-card"><span>02</span><h4>Finanzas</h4><p>Billetera, pagos, disputas y adelantos.</p></div><div class="module-card accent"><span>03</span><h4>Operación</h4><p>Ofertas, GPS, incidencias, remitos y calificaciones.</p></div></div>
                    </div>
                    <div class="tab-pane fade" id="flota" role="tabpanel" aria-labelledby="flota-tab" tabindex="0">
                        <div class="role-intro"><div><span class="role-kicker">SaaS para flotas corporativas</span><h3>Tu operación completa, en una sola vista</h3><p>Gestioná choferes, vehículos, documentación, asignaciones y facturas.</p></div><a class="btn btn-brand" href="Login.aspx">Ingresar a gestión de flota</a></div>
                        <div class="module-grid"><div class="module-card"><span>01</span><h4>Personas</h4><p>Choferes, licencias y habilitaciones CNRT.</p></div><div class="module-card"><span>02</span><h4>Activos</h4><p>Vehículos, seguros y estado operativo.</p></div><div class="module-card accent"><span>03</span><h4>Despacho</h4><p>Asignación interna, seguimiento, factoring y facturas.</p></div></div>
                    </div>
                </div>
            </div>
        </section>

        <section id="saas" class="section-space fleet-section">
            <div class="container"><div class="fleet-panel"><div class="row align-items-center g-5">
                <div class="col-lg-6"><span class="eyebrow">LogTech Flotas</span><h2>La torre de control de tu operación</h2><p>Un módulo SaaS pensado para empresas con flota propia. Centralizá unidades, choferes, vencimientos y viajes sin cambiar la forma en que trabaja tu equipo.</p><ul class="check-list"><li>Tablero de disponibilidad de flota</li><li>Alertas documentales y de mantenimiento</li><li>Asignación directa de viajes internos</li><li>Facturación mensual consolidada</li></ul><a class="btn btn-light btn-lg" href="Login.aspx">Ingresar a LogTech Flotas</a></div>
                <div class="col-lg-6"><div class="fleet-dashboard"><div class="dash-top"><div><span>Flota activa</span><strong>Operación de hoy</strong></div><span class="dash-date">26 AGO</span></div><div class="dash-stats"><div><span>Unidades</span><strong>24</strong><small>22 operativas</small></div><div><span>En viaje</span><strong>08</strong><small class="positive">+2 hoy</small></div><div><span>Alertas</span><strong>03</strong><small>Documentación</small></div></div><div class="dash-list"><div><span class="vehicle-status moving"></span><strong>AE 482 QJ</strong><span>Buenos Aires → Rosario</span><em>En tránsito</em></div><div><span class="vehicle-status ready"></span><strong>AF 119 PL</strong><span>Base Avellaneda</span><em>Disponible</em></div><div><span class="vehicle-status warning"></span><strong>AD 773 BK</strong><span>VTV vence en 8 días</span><em>Revisar</em></div></div></div></div>
            </div></div></div>
        </section>

        <%-- Lista de precios v5 (6.2.5), con IVA incluido. --%>
        <section id="precios" class="section-space how-section">
            <div class="container">
                <div class="section-heading text-center"><span class="eyebrow eyebrow-light">Precios claros</span><h2>Pagás solo por lo que usás</h2><p>Todos los precios incluyen IVA. Cada cobro tiene su factura electrónica.</p></div>
                <div class="row g-4 process-row">
                    <div class="col-md-4"><article class="process-card"><span>DADOR</span><h3>Publicar cargas</h3><strong class="precio-plan">7 % por viaje</strong><p>Incluido en la tarifa que publicás: no hay cargos ocultos.</p><ul class="lista-plan"><li>Tus 3 primeros viajes sin comisión</li><li>Remito digital: $ 1.500 por viaje</li><li>Seguro de la carga desde $ 15.000 por mes</li></ul></article></div>
                    <div class="col-md-4"><article class="process-card featured"><span>TRANSPORTISTA</span><h3>Premium</h3><strong class="precio-plan">$ 22.500 por mes</strong><p>Para el que vive de la ruta.</p><ul class="lista-plan"><li>No pagás la comisión del 7 %</li><li>Alertas de cargas de alta rentabilidad</li><li>Adelanto de cobros (factoring) al 4 %, también en la cuenta gratuita</li></ul></article></div>
                    <div class="col-md-4"><article class="process-card"><span>FLOTA</span><h3>Flota SaaS</h3><strong class="precio-plan">$ 52.500 por patente</strong><p>Por mes, según el cupo de vehículos que elijas.</p><ul class="lista-plan"><li>Vehículos, conductores y asignación de viajes</li><li>Reporte de la flota con gastos del mes</li><li>Cambiás el cupo o la das de baja cuando quieras</li></ul></article></div>
                </div>
            </div>
        </section>

        <section id="proximos-pasos" class="closing-section"><div class="container text-center"><span class="eyebrow eyebrow-light">El próximo viaje empieza acá</span><h2>Más carga. Menos incertidumbre.</h2><p>Una experiencia clara para quien publica, transporta o administra una flota.</p><a class="btn btn-brand btn-lg" href="Login.aspx">Iniciar sesión</a></div></section>
    </main>
</asp:Content>
