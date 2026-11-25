<%@ Page Title="Quiénes somos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuienesSomos.aspx.cs" Inherits="LogTech.QuienesSomos" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="landing-hero">
            <div class="container">
                <span class="eyebrow eyebrow-light">Quiénes somos</span>
                <h1>Tecnología para ordenar el transporte de cargas.</h1>
                <p>LogTech es una empresa de base tecnológica que desarrolla software B2B en la nube. Conectamos a las industrias que necesitan mover mercadería con los transportistas y las flotas que tienen camiones disponibles, en un solo lugar y con reglas claras.</p>
                <div class="hero-actions"><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a><a class="btn btn-outline-dark btn-lg" href="ComoFunciona.aspx">Cómo funciona</a></div>
            </div>
        </section>

        <%-- Trayectoria: lista ordenada de HTML5 con fechas (<time>); el script de abajo muestra cada etapa al llegar a ella. --%>
        <section class="section-space how-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Nuestra trayectoria</span><h2>De un problema del sector a una plataforma</h2><p>El camino de LogTech, desde la idea hasta la expansión que planeamos.</p></div>
                <ol class="trayectoria">
                    <li><span class="trayectoria-fecha">El origen</span><h3>Un sector que trabaja a mano</h3><p>El transporte de cargas se arreglaba por teléfono, con intermediarios y papeles. Sin datos centralizados no había forma de saber quién estaba habilitado, dónde estaba la carga ni cuándo se cobraba.</p></li>
                    <li><time datetime="2026">2026</time><h3>Lanzamiento en Buenos Aires</h3><p>Desarrollamos la plataforma, el motor de asignación y la billetera con retención escrow, y la lanzamos en Buenos Aires, donde se concentra la mayor parte de la actividad industrial y portuaria del país.</p></li>
                    <li><time datetime="2027">2027 a 2028</time><h3>Crecimiento en todo el país</h3><p>Más viajes por la plataforma, el módulo SaaS para flotas propias y el factoring logístico. Llegar a provincias como Santa Fe y Córdoba no necesita infraestructura física: todo funciona en la nube.</p></li>
                    <li><time datetime="2029">Desde 2029</time><h3>Mirada regional</h3><p>Adaptar la plataforma a las normas fiscales de otros países para llevarla a la región, empezando por los países limítrofes.</p></li>
                </ol>
            </div>
        </section>

        <section class="section-space profiles-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Misión y visión</span><h2>Para qué existimos</h2></div>
                <div class="row g-4">
                    <div class="col-lg-6"><article class="process-card"><h3>Misión</h3><p>Desarrollar y proveer soluciones de software B2B para profesionalizar el ecosistema logístico mediante un motor de asignación centralizado. El propósito de la empresa es entregar la infraestructura tecnológica necesaria para garantizar la transparencia operativa, la seguridad transaccional y la optimización de recursos en toda la cadena de suministro.</p></article></div>
                    <div class="col-lg-6"><article class="process-card"><h3>Visión</h3><p>Convertirnos en la empresa proveedora de software B2B líder y el estándar tecnológico del sector logístico a nivel nacional y regional, consolidando nuestra plataforma y nuestro motor de asignación como la infraestructura principal que las industrias elijan para digitalizar, transparentar y hacer eficiente su cadena de suministro.</p></article></div>
                </div>
            </div>
        </section>

        <section class="section-space how-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Lo que nos distingue</span><h2>Más que una cartelera de fletes</h2></div>
                <div class="row g-4">
                    <div class="col-md-6"><article class="process-card"><h3>Pagos seguros</h3><p>La billetera retiene el pago del dador hasta que se entrega la carga, y el factoring le adelanta al transportista lo que tiene a cobrar.</p></article></div>
                    <div class="col-md-6"><article class="process-card"><h3>Documentación validada</h3><p>Nadie toma un viaje sin la documentación aprobada y vigente: la revisa el administrador y el sistema avisa antes de que venza.</p></article></div>
                    <div class="col-md-6"><article class="process-card"><h3>Remitos que no se pueden alterar</h3><p>Cada remito digital queda sellado con un token SHA-256: si alguien cambia un dato, se nota.</p></article></div>
                    <div class="col-md-6"><article class="process-card"><h3>Hecho para crecer</h3><p>Una arquitectura en capas sobre tecnología Microsoft (ASP.NET y SQL Server) que suma usuarios y servicios sin rehacer el sistema.</p></article></div>
                </div>
            </div>
        </section>

        <section class="section-space profiles-section">
            <div class="container">
                <div class="section-heading"><span class="eyebrow eyebrow-light">Cómo nos organizamos</span><h2>Una dirección general y cuatro gerencias</h2><p>Cada gerencia se ocupa de una parte del negocio.</p></div>
                <ul class="lista-simple">
                    <li>Administración y Finanzas: facturación, compras, tesorería, contabilidad y recursos humanos.</li>
                    <li>Tecnología y Operaciones: desarrollo de la plataforma, infraestructura en la nube, soporte y ciberseguridad.</li>
                    <li>Comercial y de Negocios: nuevos clientes, ventas a empresas y ventas digitales.</li>
                    <li>Marketing: producto y precios, marketing para empresas y fidelización de clientes.</li>
                </ul>
            </div>
        </section>

        <section class="closing-section"><div class="container text-center"><span class="eyebrow eyebrow-light">Sumate</span><h2>Movamos juntos tu próxima carga.</h2><p>Creá tu cuenta como dador de carga, transportista o empresa con flota.</p><a class="btn btn-brand btn-lg" href="Registro.aspx">Crear una cuenta</a></div></section>
    </main>

    <script>
        // Quiénes somos: cada etapa de la trayectoria aparece cuando llega a la pantalla. Sin JavaScript (o con
        // «reducir movimiento» activado en el equipo) se ven todas desde el principio.
        (function () {
            var linea = document.querySelector(".trayectoria");
            if (!linea || !("IntersectionObserver" in window) || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
            linea.classList.add("animada");
            var observador = new IntersectionObserver(function (entradas) {
                entradas.forEach(function (entrada) {
                    if (!entrada.isIntersecting) return;
                    entrada.target.classList.add("visible");
                    observador.unobserve(entrada.target);
                });
            }, { threshold: 0.2 });
            linea.querySelectorAll("li").forEach(function (etapa) { observador.observe(etapa); });
        }());
    </script>
</asp:Content>
