<%@ Page Title="Gestión de flota" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GestionFlota.aspx.cs" Inherits="LogTech.GestionFlota" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="landing-hero">
            <div class="container">
                <span class="eyebrow eyebrow-light">LogTech Flotas</span>
                <h1>La torre de control de tu flota.</h1>
                <p>Para empresas con camiones y choferes propios: tomá viajes de la bolsa de cargas y despachalos con tu gente, con la documentación siempre en regla.</p>
                <div class="hero-actions"><a class="btn btn-brand btn-lg" href="Login.aspx">Ingresar a LogTech Flotas</a><a class="btn btn-outline-dark btn-lg" href="#preguntas">Preguntas frecuentes</a></div>
            </div>
        </section>

        <section class="section-space how-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Qué incluye</span><h2>Todo lo que necesita tu operación</h2></div>
                <div class="pasos-grid">
                    <article class="process-card"><span>01</span><div class="process-icon">▣</div><h3>Vehículos</h3><p>Alta, modificación y baja de tus camiones con patente, capacidad y tipo de acoplado. La patente no se puede repetir en la plataforma.</p></article>
                    <article class="process-card"><span>02</span><div class="process-icon">☺</div><h3>Conductores</h3><p>Cargá a tus choferes con DNI, licencia y teléfono. No necesitan una cuenta propia: los administrás vos.</p></article>
                    <article class="process-card"><span>03</span><div class="process-icon">⇄</div><h3>Asignación interna</h3><p>Aceptás una carga y después elegís qué conductor y qué camión la hacen. Solo te ofrecemos los que están libres y soportan el peso.</p></article>
                    <article class="process-card"><span>04</span><div class="process-icon">◉</div><h3>Seguimiento</h3><p>Actualizás los estados del viaje, reportás incidencias y emitís el remito digital al entregar.</p></article>
                    <article class="process-card"><span>05</span><div class="process-icon">▤</div><h3>Documentación</h3><p>Habilitación CNRT, seguros y VTV de cada unidad, con semáforo y aviso 30 días antes de cada vencimiento.</p></article>
                    <article class="process-card"><span>06</span><div class="process-icon">★</div><h3>Reputación</h3><p>Cada viaje entregado suma calificaciones que ven los dadores antes de elegir con quién trabajar.</p></article>
                </div>
            </div>
        </section>

        <section class="section-space fleet-section">
            <div class="container"><div class="fleet-panel"><div class="row align-items-center g-5">
                <div class="col-lg-6"><span class="eyebrow">Reglas que te cuidan</span><h2>Nada se pisa, nada se pierde</h2><p>El sistema controla lo que en una planilla se escapa.</p><ul class="check-list"><li>Un camión o un conductor en viaje no se puede editar, dar de baja ni asignar a otro viaje</li><li>El viaje no sale hasta tener conductor y camión asignados</li><li>Los estados avanzan en orden, sin saltear ninguno</li><li>Sin documentación obligatoria vigente no se toman viajes</li></ul></div>
                <div class="col-lg-6"><div class="fleet-dashboard"><div class="dash-top"><div><span>Plan Flota SaaS</span><strong>Abono mensual por camión</strong></div></div><div class="dash-stats"><div><span>Se paga</span><strong>1</strong><small>abono por patente activa</small></div><div><span>Conductores</span><strong>∞</strong><small>sin costo extra</small></div><div><span>Comisión</span><strong>7 %</strong><small>por viaje concretado</small></div></div></div></div>
            </div></div></div>
        </section>

        <section id="preguntas" class="section-space how-section">
            <div class="container faq">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Preguntas frecuentes</span><h2>Sobre LogTech Flotas</h2></div>
                <details><summary>¿Los choferes necesitan una cuenta?</summary><p>No. Los carga el administrador de flota y los asigna a cada viaje desde "Mis viajes".</p></details>
                <details><summary>¿Puedo cambiar el camión o el chofer de un viaje?</summary><p>Sí, mientras el viaje esté asignado y todavía no haya salido. Cuando arranca, la asignación queda fija.</p></details>
                <details><summary>¿Qué pasa si quiero dar de baja un camión que está viajando?</summary><p>El sistema no lo permite: primero tiene que terminar el viaje. Lo mismo pasa con los conductores.</p></details>
                <details><summary>¿Puedo registrar una patente que ya usa otra empresa?</summary><p>No. Cada patente activa existe una sola vez en la plataforma. Si la unidad cambió de dueño, la empresa anterior tiene que darla de baja.</p></details>
                <details><summary>¿Quién actualiza el estado de los viajes de la flota?</summary><p>El administrador de flota, desde "Mis viajes". Cada cambio le llega como aviso al dador de la carga.</p></details>
                <details><summary>¿Cómo se cobra el servicio?</summary><p>Con el plan Flota SaaS pagás un abono mensual por cada camión activo. Además, LogTech cobra el 7 % de comisión sobre cada viaje concretado.</p></details>
            </div>
        </section>

        <section class="closing-section"><div class="container text-center"><span class="eyebrow eyebrow-light">LogTech Flotas</span><h2>Tu flota trabajando, vos en control.</h2><p>Ingresá con tu cuenta de administrador de flota.</p><a class="btn btn-brand btn-lg" href="Login.aspx">Ingresar</a></div></section>
    </main>
</asp:Content>
