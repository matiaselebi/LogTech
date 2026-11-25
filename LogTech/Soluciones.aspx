<%@ Page Title="Soluciones" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Soluciones.aspx.cs" Inherits="LogTech.Soluciones" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="landing-hero">
            <div class="container">
                <span class="eyebrow eyebrow-light">Soluciones</span>
                <h1>Una herramienta para cada perfil.</h1>
                <p>El que envía la carga, el que la transporta y el que administra una flota usan la misma plataforma, cada uno con su menú y sus permisos.</p>
                <div class="hero-actions"><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a><a class="btn btn-outline-dark btn-lg" href="#preguntas">Preguntas frecuentes</a></div>
            </div>
        </section>

        <section class="section-space how-section">
            <div class="container">
                <div class="soluciones-grid">
                    <article class="solucion-card">
                        <span class="role-kicker">Dador de carga</span>
                        <h2>Mandá mercadería sin perderla de vista</h2>
                        <ul class="lista-simple">
                            <li>Publicá, modificá o das de baja tus ofertas de carga.</li>
                            <li>Mirá qué camiones libres están cerca del origen y cómo los calificaron.</li>
                            <li>Seguí cada viaje en el mapa, con su historial e incidencias.</li>
                            <li>Consultá los remitos digitales verificados de tus entregas.</li>
                            <li>Calificá al transportista cuando termina el viaje.</li>
                        </ul>
                        <a class="btn btn-brand" href="Registro.aspx">Registrarme como dador</a>
                    </article>
                    <article class="solucion-card">
                        <span class="role-kicker">Transportista</span>
                        <h2>Encontrá viajes que te queden cerca</h2>
                        <ul class="lista-simple">
                            <li>Buscá en la bolsa de cargas por origen y destino.</li>
                            <li>Usá el match para ver primero las cargas más cercanas que entran en tu camión.</li>
                            <li>Aceptá el viaje eligiendo con qué vehículo lo hacés.</li>
                            <li>Actualizá los estados, reportá incidencias y emití el remito al entregar.</li>
                            <li>Tené tu legajo al día con el semáforo de documentación.</li>
                        </ul>
                        <a class="btn btn-brand" href="Registro.aspx">Registrarme como transportista</a>
                    </article>
                    <article class="solucion-card destacada">
                        <span class="role-kicker">Administrador de flota</span>
                        <h2>Toda tu flota en un solo panel</h2>
                        <ul class="lista-simple">
                            <li>Todo lo del transportista, para varios camiones.</li>
                            <li>Cargá a tus conductores y tus vehículos.</li>
                            <li>Tomá viajes y asignalos a un conductor y un camión libres.</li>
                            <li>Controlá la documentación de la empresa y de cada unidad.</li>
                        </ul>
                        <a class="btn btn-brand" href="GestionFlota.aspx">Conocer LogTech Flotas</a>
                    </article>
                </div>
            </div>
        </section>

        <section class="section-space profiles-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Seguridad</span><h2>Tus datos, protegidos</h2><p>Lo que hace la plataforma para que la información sea confiable.</p></div>
                <div class="module-grid">
                    <div class="module-card"><span>01</span><h4>Contraseñas cifradas</h4><p>Se guardan con hash SHA-256 y salt. Después de varios intentos fallidos la cuenta se bloquea.</p></div>
                    <div class="module-card"><span>02</span><h4>Bitácora</h4><p>Cada operación queda registrada con fecha, usuario y criticidad.</p></div>
                    <div class="module-card accent"><span>03</span><h4>Dígitos verificadores</h4><p>Los usuarios, la bitácora, los viajes y los remitos están firmados: si alguien los cambia por fuera del sistema, se detecta.</p></div>
                </div>
            </div>
        </section>

        <section id="preguntas" class="section-space how-section">
            <div class="container faq">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Preguntas frecuentes</span><h2>Antes de empezar</h2></div>
                <details><summary>¿Cómo me registro?</summary><p>Desde "Crear cuenta" elegís si sos dador de carga o transportista y completás tus datos. El CUIT o CUIL se valida con su dígito verificador.</p></details>
                <details><summary>¿Qué documentos tengo que subir?</summary><p>El transportista necesita la licencia de conducir aprobada para tomar viajes; también puede cargar el seguro y la VTV de cada camión y su constancia de CUIT. La flota necesita la habilitación CNRT. El dador puede cargar su constancia de CUIT.</p></details>
                <details><summary>¿Quién revisa la documentación?</summary><p>Un analista de LogTech la aprueba o la rechaza con un motivo. En los dos casos te llega un aviso a tu panel.</p></details>
                <details><summary>¿Qué pasa cuando un documento está por vencer?</summary><p>Te avisamos 30 días antes. Si vence, queda inhabilitado y no podés tomar viajes hasta cargar uno nuevo.</p></details>
                <details><summary>¿En qué idiomas está la plataforma?</summary><p>En español, inglés y portugués. Lo cambiás desde el menú de idioma, arriba a la derecha.</p></details>
                <details><summary>Me olvidé la contraseña, ¿qué hago?</summary><p>En la pantalla de ingreso elegí "Olvidé mi contraseña". Te mandamos un enlace al correo para crear una nueva.</p></details>
            </div>
        </section>

        <section class="closing-section"><div class="container text-center"><span class="eyebrow eyebrow-light">Empezá hoy</span><h2>Elegí tu perfil y empezá a operar.</h2><p>Registrarte lleva un par de minutos.</p><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a></div></section>
    </main>
</asp:Content>
