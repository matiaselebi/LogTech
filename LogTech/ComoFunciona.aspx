<%@ Page Title="Cómo funciona" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ComoFunciona.aspx.cs" Inherits="LogTech.ComoFunciona" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="landing-hero">
            <div class="container">
                <span class="eyebrow eyebrow-light">Cómo funciona</span>
                <h1>De la publicación a la entrega, paso a paso.</h1>
                <p>Así recorre un viaje la plataforma: quién hace qué, qué controla el sistema y qué ves en cada momento.</p>
                <div class="hero-actions"><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a><a class="btn btn-outline-dark btn-lg" href="#preguntas">Preguntas frecuentes</a></div>
            </div>
        </section>

        <section class="section-space how-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">El recorrido de un viaje</span><h2>Seis pasos, un solo lugar</h2><p>Cada paso queda registrado en la bitácora y le avisa a quien corresponde.</p></div>
                <div class="pasos-grid">
                    <article class="process-card"><span>01</span><div class="process-icon">⌖</div><h3>Publicás la carga</h3><p>El dador indica origen, destino, mercadería, peso, fecha de carga y la tarifa que ofrece. La oferta aparece al instante en la bolsa de cargas.</p></article>
                    <article class="process-card"><span>02</span><div class="process-icon">⇄</div><h3>El match encuentra el camión</h3><p>El motor cruza las cargas con los camiones libres que soportan el peso y las ordena por cercanía, con la ubicación del navegador o una ciudad de referencia.</p></article>
                    <article class="process-card"><span>03</span><div class="process-icon">✓</div><h3>Un transportista la acepta</h3><p>Solo puede tomarla quien tiene la documentación aprobada y vigente. Si dos la quieren al mismo tiempo, se la queda el primero.</p></article>
                    <article class="process-card"><span>04</span><div class="process-icon">◉</div><h3>Seguís el viaje</h3><p>El transportista avanza los estados en orden y el dador ve el mapa con la última ubicación, la línea de tiempo y las incidencias.</p></article>
                    <article class="process-card"><span>05</span><div class="process-icon">▤</div><h3>Remito digital</h3><p>En destino se carga quién recibió y la foto del remito firmado. El remito queda sellado con un token SHA-256: si alguien lo altera, se nota.</p></article>
                    <article class="process-card"><span>06</span><div class="process-icon">★</div><h3>Calificás el servicio</h3><p>El dador pone de 1 a 5 estrellas y una reseña, una sola vez por viaje. Así se arma la reputación de cada transportista.</p></article>
                </div>
            </div>
        </section>

        <section class="section-space profiles-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Estados del viaje</span><h2>Siempre sabés en qué etapa está</h2><p>Los estados van en orden y no se pueden saltear. Cada cambio queda en el historial del viaje.</p></div>
                <ol class="estados-linea">
                    <li><span class="badge-estado estado-pendiente">Pendiente</span><small>Publicada en la bolsa de cargas</small></li>
                    <li><span class="badge-estado estado-en-curso">Asignado</span><small>Un transportista o una flota la tomó</small></li>
                    <li><span class="badge-estado estado-en-curso">En camino a carga</span><small>El camión va al origen</small></li>
                    <li><span class="badge-estado estado-en-curso">En origen</span><small>Cargando la mercadería</small></li>
                    <li><span class="badge-estado estado-en-curso">En tránsito</span><small>Viajando al destino</small></li>
                    <li><span class="badge-estado estado-en-curso">En destino</span><small>Listo para entregar</small></li>
                    <li><span class="badge-estado estado-entregado">Entregado</span><small>Con remito digital emitido</small></li>
                </ol>
            </div>
        </section>

        <section id="preguntas" class="section-space how-section">
            <div class="container faq">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Preguntas frecuentes</span><h2>Lo que más nos consultan</h2></div>
                <details><summary>¿Cuánto cuesta publicar una carga?</summary><p>Publicar es gratis. LogTech cobra una comisión del 7 % sobre la tarifa de cada viaje que se concreta.</p></details>
                <details><summary>¿Puedo modificar o dar de baja una oferta?</summary><p>Sí, mientras siga pendiente. Cuando un transportista la acepta ya no se puede cambiar, para que nadie se encuentre con un viaje distinto al que tomó.</p></details>
                <details><summary>¿Qué pasa si dos transportistas aceptan la misma carga a la vez?</summary><p>El sistema se la asigna al primero que confirma. Al segundo le avisa que la oferta ya no está disponible.</p></details>
                <details><summary>¿Cómo sé dónde está mi carga?</summary><p>Desde "Mis cargas" elegís "Monitorear": ves el mapa con la última ubicación que reportó el camión y la línea de tiempo del viaje. Si el camión no reporta hace más de dos horas, te avisamos que se perdió la señal.</p></details>
                <details><summary>¿Qué pasa si hay un problema en la ruta?</summary><p>El transportista reporta la incidencia (rotura, demora, accidente, clima o corte de ruta) con una descripción y una foto opcional. Te llega un aviso y queda en la línea de tiempo del viaje.</p></details>
                <details><summary>¿Qué es el remito digital?</summary><p>Es el comprobante de entrega: quién recibió, su DNI, la foto del remito firmado y la fecha. Se emite una sola vez, no se puede editar y se verifica con un token cada vez que se abre.</p></details>
                <details><summary>¿Puedo calificar más de una vez el mismo viaje?</summary><p>No. Cada viaje entregado se califica una sola vez, para que la reputación sea confiable.</p></details>
                <details><summary>¿Qué archivos puedo subir?</summary><p>PDF o JPG de hasta 5 MB. El sistema revisa el contenido real del archivo, no solo su extensión.</p></details>
            </div>
        </section>

        <section class="closing-section"><div class="container text-center"><span class="eyebrow eyebrow-light">Empezá hoy</span><h2>Tu próxima carga, en minutos.</h2><p>Creá tu cuenta como dador de carga o como transportista.</p><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a></div></section>
    </main>
</asp:Content>
