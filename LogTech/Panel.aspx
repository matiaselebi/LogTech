<%@ Page Title="Panel" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Panel.aspx.cs" Inherits="LogTech.Panel" %>
<%-- Gráficos del tablero: control Chart que ya trae .NET Framework (no hay que instalar nada). --%>
<%@ Register Assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>
<%@ Register Src="~/Controles/MisCargas.ascx" TagPrefix="uc" TagName="MisCargas" %>
<%@ Register Src="~/Controles/BolsaCargas.ascx" TagPrefix="uc" TagName="BolsaCargas" %>
<%@ Register Src="~/Controles/MisViajes.ascx" TagPrefix="uc" TagName="MisViajes" %>
<%@ Register Src="~/Controles/Vehiculos.ascx" TagPrefix="uc" TagName="Vehiculos" %>
<%@ Register Src="~/Controles/Conductores.ascx" TagPrefix="uc" TagName="Conductores" %>
<%@ Register Src="~/Controles/Documentacion.ascx" TagPrefix="uc" TagName="Documentacion" %>
<%@ Register Src="~/Controles/ValidarDocumentacion.ascx" TagPrefix="uc" TagName="ValidarDocumentacion" %>
<%@ Register Src="~/Controles/Remitos.ascx" TagPrefix="uc" TagName="Remitos" %>
<%@ Register Src="~/Controles/ReporteFlota.ascx" TagPrefix="uc" TagName="ReporteFlota" %>
<%@ Register Src="~/Controles/Billetera.ascx" TagPrefix="uc" TagName="Billetera" %>
<%@ Register Src="~/Controles/Disputas.ascx" TagPrefix="uc" TagName="Disputas" %>
<%@ Register Src="~/Controles/ViajesAdmin.ascx" TagPrefix="uc" TagName="ViajesAdmin" %>
<%@ Register Src="~/Controles/Seguros.ascx" TagPrefix="uc" TagName="Seguros" %>
<%@ Register Src="~/Controles/Facturas.ascx" TagPrefix="uc" TagName="Facturas" %>
<%@ Register Src="~/Controles/Suscripcion.ascx" TagPrefix="uc" TagName="Suscripcion" %>
<%@ Register Src="~/Controles/SemaforoDocumentacion.ascx" TagPrefix="uc" TagName="SemaforoDocumentacion" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="panel-page panel-page-app">
        <div class="panel-app-shell">
            <button id="panelMenuToggle" class="panel-menu-toggle" type="button" aria-label="Abrir menú" aria-controls="panelSidebar" aria-expanded="false"><span></span><span></span><span></span></button>
            <aside id="panelSidebar" class="panel-sidebar">
                <%-- La foto de perfil, si el usuario subió una en "Mi perfil"; si no, el logo. --%>
                <div class="panel-sidebar-head"><asp:Image ID="imgFotoMenu" runat="server" CssClass="brand-mark foto-perfil" AlternateText="Foto de perfil" Visible="false" /><span id="spnLogoMenu" runat="server" class="brand-mark brand-logo-mark" aria-hidden="true"></span><div><strong>LogTech</strong><small><asp:Literal ID="litRolMenu" runat="server" /></small></div></div>
                <nav class="panel-nav" aria-label="Menú del panel">
                    <asp:LinkButton ID="btnMenuInicio" runat="server" CausesValidation="false" OnClick="btnMenuInicio_Click" Text="Inicio" />
                    <%-- Operación logística: cada opción aparece solo si el rol tiene el permiso. --%>
                    <asp:Panel ID="pnlMenuOperacion" runat="server" Visible="false">
                        <asp:LinkButton ID="btnMenuCargas" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Cargas" Text="Mis cargas" />
                        <asp:LinkButton ID="btnMenuBolsa" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Bolsa" Text="Bolsa de cargas" />
                        <asp:LinkButton ID="btnMenuViajes" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Viajes" Text="Mis viajes" />
                        <asp:LinkButton ID="btnMenuVehiculos" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Vehiculos" Text="Mis vehículos" />
                        <asp:LinkButton ID="btnMenuConductores" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Conductores" Text="Conductores" />
                        <asp:LinkButton ID="btnMenuDocumentacion" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Documentacion" Text="Mi documentación" />
                        <asp:LinkButton ID="btnMenuValidar" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Validar" Text="Validar documentación" />
                        <asp:LinkButton ID="btnMenuRemitos" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Remitos" Text="Remitos digitales" />
                        <asp:LinkButton ID="btnMenuReporteFlota" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="ReporteFlota" Text="Reporte de la flota" />
                        <asp:LinkButton ID="btnMenuBilletera" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Billetera" Text="Billetera" />
                        <asp:LinkButton ID="btnMenuSeguros" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Seguros" Text="Seguros" />
                        <asp:LinkButton ID="btnMenuFacturas" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Facturas" Text="Facturas" />
                        <asp:LinkButton ID="btnMenuSuscripcion" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Suscripcion" Text="Suscripción" />
                        <asp:LinkButton ID="btnMenuDisputas" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="Disputas" Text="Disputas" />
                        <asp:LinkButton ID="btnMenuViajesAdmin" runat="server" CausesValidation="false" OnClick="btnMenuModulo_Click" CommandArgument="ViajesAdmin" Text="Viajes" />
                    </asp:Panel>
                    <asp:Panel ID="pnlMenuAdmin" runat="server" Visible="false">
                        <asp:LinkButton ID="btnMenuUsuarios" runat="server" CausesValidation="false" OnClick="btnMenuUsuarios_Click" Text="Usuarios" />
                        <asp:LinkButton ID="btnMenuRoles" runat="server" CausesValidation="false" OnClick="btnMenuRoles_Click" Text="Roles y permisos" />
                        <asp:LinkButton ID="btnMenuBitacora" runat="server" CausesValidation="false" OnClick="btnMenuBitacora_Click" Text="Bitácora" />
                    </asp:Panel>
                    <asp:Panel ID="pnlMenuMantenimiento" runat="server" Visible="false">
                        <asp:LinkButton ID="btnMenuBaseDatos" runat="server" CausesValidation="false" OnClick="btnMenuBaseDatos_Click" Text="Base de datos" />
                    </asp:Panel>
                    <asp:Panel ID="pnlMenuVacio" runat="server" CssClass="panel-nav-empty"><small>Las opciones de este perfil se agregarán en próximas entregas.</small></asp:Panel>
                </nav>
                <%-- CU-011-068 (ERR039): pide confirmación; si el usuario cancela, la sesión sigue abierta. --%>
                <asp:Button ID="btnCerrarSesion" runat="server" CssClass="btn btn-outline-light panel-logout" OnClick="btnCerrarSesion_Click"
                    CausesValidation="false" OnClientClick="return confirm('¿Querés cerrar la sesión?');" />
            </aside>
            <button id="panelMenuOverlay" class="panel-menu-overlay" type="button" aria-label="Cerrar menú"></button>

            <section class="panel-main">
                <asp:Label ID="lblSinPermiso" runat="server" CssClass="panel-message error" role="alert" Visible="false"
                    Text="No tenés permiso para entrar a esa sección. El intento quedó registrado en la bitácora." />
                <%-- ERR049: la base fue alterada por afuera. Lo ve el administrador apenas entra (y la contingencia). --%>
                <asp:Panel ID="pnlFallaIntegridad" runat="server" CssClass="admin-section falla-integridad" Visible="false">
                    <div class="section-title"><div><h2>Error de integridad</h2><p><asp:Literal ID="litFallaIntegridad" runat="server" /></p></div></div>
                    <div class="table-responsive"><asp:GridView ID="gvFallas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None"><Columns><asp:BoundField DataField="Tabla" HeaderText="Tabla" /><asp:BoundField DataField="Fila" HeaderText="Registro" NullDisplayText="-" /><asp:BoundField DataField="Detalle" HeaderText="Detalle" /></Columns></asp:GridView></div>
                    <div class="botonera">
                        <asp:Button ID="btnRecalcularFalla" runat="server" CssClass="btn btn-brand" Text="Recalcular dígitos verificadores" CausesValidation="false" OnClick="btnRecalcular_Click" />
                        <asp:Button ID="btnIrRestaurar" runat="server" CssClass="btn btn-outline-danger" Text="Restaurar un backup" CausesValidation="false" OnClick="btnIrRestaurar_Click" />
                    </div>
                </asp:Panel>
                <%-- CU-010-066 (ERR046): antes de recalcular los dígitos se pide la contraseña de quien lo hace. --%>
                <asp:Panel ID="pnlConfirmarRecalculo" runat="server" CssClass="admin-section" Visible="false" DefaultButton="btnConfirmarRecalculo">
                    <div class="section-title"><div><h2>Recalcular dígitos verificadores</h2>
                        <p>Se aceptan los datos actuales como correctos y se vuelven a firmar todas las tablas. Para confirmar, ingresá tu contraseña.</p></div></div>
                    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtClaveRecalculo" Text="Contraseña" />
                        <asp:TextBox ID="txtClaveRecalculo" runat="server" CssClass="form-control" TextMode="Password" MaxLength="64" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtClaveRecalculo" ValidationGroup="Recalculo" Display="Dynamic" CssClass="text-danger" ErrorMessage="Ingresá tu contraseña." /></div>
                    <asp:Label ID="lblRecalculo" runat="server" CssClass="panel-message error" role="alert" />
                    <div class="botonera">
                        <asp:Button ID="btnConfirmarRecalculo" runat="server" CssClass="btn btn-brand" Text="Recalcular" ValidationGroup="Recalculo" OnClick="btnConfirmarRecalculo_Click" />
                        <asp:Button ID="btnCancelarRecalculo" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarRecalculo_Click" />
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlInicio" runat="server">
                    <header class="session-banner">
                        <div class="session-identity"><span class="session-dot"></span><div><small><asp:Literal ID="litEstado" runat="server" /></small><strong><asp:Literal ID="litSesion" runat="server" /></strong></div></div>
                    </header>

                    <%-- Avisos del usuario (Observer y rutina de vencimientos) y semáforo de su documentación. --%>
                    <asp:Panel ID="pnlAvisos" runat="server" Visible="false" CssClass="admin-section avisos-panel">
                        <div class="section-title"><div><h2>Avisos</h2></div><asp:LinkButton ID="btnMarcarLeidas" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Marcar como leídos" CausesValidation="false" OnClick="btnMarcarLeidas_Click" /></div>
                        <uc:SemaforoDocumentacion ID="ucSemaforoInicio" runat="server" />
                        <asp:Repeater ID="rptAvisos" runat="server">
                            <HeaderTemplate><ul class="avisos-lista"></HeaderTemplate>
                            <ItemTemplate><li class='<%# (bool)Eval("Leida") ? "" : "is-nuevo" %>'><time><%#: Eval("Fecha", "{0:dd/MM HH:mm}") %></time><span><%#: Eval("Texto") %></span></li></ItemTemplate>
                            <FooterTemplate></ul></FooterTemplate>
                        </asp:Repeater>
                        <asp:Label ID="lblSinAvisos" runat="server" CssClass="avisos-vacio" Text="No tenés avisos." />
                        <asp:Label ID="lblAvisos" runat="server" CssClass="panel-message" role="status" />
                    </asp:Panel>

                    <asp:Panel ID="pnlTablero" runat="server" Visible="false">
                        <div class="panel-heading">
                            <div><span class="eyebrow eyebrow-light">TABLERO</span><h1>Resumen del negocio</h1><p>Lo que mueve plata en LogTech: suscripciones y viajes.</p></div>
                            <div class="tablero-acciones">
                                <div class="form-field tablero-filtro"><asp:Label runat="server" AssociatedControlID="ddlPeriodo" Text="Ver por" /><asp:DropDownList ID="ddlPeriodo" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlTablero_SelectedIndexChanged"><asp:ListItem Value="Diario" Text="Día (últimos 30 días)" /><asp:ListItem Value="Mensual" Text="Mes (últimos 12 meses)" Selected="True" /><asp:ListItem Value="Anual" Text="Año (últimos 5 años)" /></asp:DropDownList></div>
                                <asp:Button ID="btnExportarReporte" runat="server" CssClass="btn btn-brand" Text="Exportar reporte (CSV)" CausesValidation="false" OnClick="btnExportarReporte_Click" />
                            </div>
                        </div>

                        <div class="tablero-resumen">
                            <div class="tablero-kpis">
                                <article class="kpi-card"><small>Suscriptores activos</small><strong><asp:Literal ID="litSuscriptoresActivos" runat="server" /></strong><span>Clientes con una suscripción vigente</span></article>
                                <article class="kpi-card"><small>Ingreso mensual por suscripciones</small><strong><asp:Literal ID="litIngresoSuscriptores" runat="server" /></strong><span>Lo que cobra LogTech por mes de las suscripciones activas (de los seguros, el 5 %)</span></article>
                                <article class="kpi-card"><small>Suscripciones nuevas del mes</small><strong><asp:Literal ID="litSuscripcionesMes" runat="server" /></strong><span>Altas desde el 1 de este mes</span></article>
                                <article class="kpi-card"><small>Viajes pedidos del mes</small><strong><asp:Literal ID="litViajesMes" runat="server" /></strong><span>Pedidos desde el 1 de este mes</span></article>
                                <article class="kpi-card"><small>Plata movida en viajes del mes</small><strong><asp:Literal ID="litMontoViajesMes" runat="server" /></strong><span>Suma de las tarifas de los viajes</span></article>
                                <article class="kpi-card"><small>Comisiones de viajes del mes</small><strong><asp:Literal ID="litComisionesMes" runat="server" /></strong><span>El 7 % de cada viaje que cobra LogTech</span></article>
                                <article class="kpi-card"><small>Seguros activos</small><strong><asp:Literal ID="litSegurosActivos" runat="server" /></strong><span>Contratados a las aseguradoras desde LogTech</span></article>
                                <article class="kpi-card"><small>Comisión de seguros por mes</small><strong><asp:Literal ID="litComisionSeguros" runat="server" /></strong><span>El 5 % de la prima de cada seguro activo</span></article>
                            </div>
                            <article class="ingresos-card">
                                <small>Plata ingresada este mes</small>
                                <strong><asp:Literal ID="litIngresosMes" runat="server" /></strong>
                                <span>Abonos de las suscripciones, el 5 % de los seguros y las comisiones de viajes, remitos y factoring del mes</span>
                                <asp:Chart ID="chrIngresos" runat="server" Width="380px" Height="230px" AntiAliasing="All" TextAntiAliasingQuality="Normal" Palette="None" PaletteCustomColors="#123D32; #79B51C; #4A9A9A; #FF6B2C">
                                    <Series><asp:Series Name="Serie" ChartType="Doughnut" XValueMember="Nombre" YValueMembers="Monto" LegendText="#VALX  #PERCENT{P0}" BorderColor="White" BorderWidth="2" CustomProperties="DoughnutRadius=45, PieLabelStyle=Disabled" /></Series>
                                    <Legends><asp:Legend Name="Default" Docking="Bottom" Alignment="Center" LegendStyle="Column" MaximumAutoSize="70" TextWrapThreshold="40" IsTextAutoFit="false" ForeColor="#4C625B" Font="Segoe UI, 9pt" /></Legends>
                                    <ChartAreas><asp:ChartArea Name="Area" /></ChartAreas>
                                </asp:Chart>
                            </article>
                        </div>
                        <asp:Label ID="lblTablero" runat="server" CssClass="panel-message" role="status" />

                        <section class="admin-section">
                            <div class="section-title"><div><span>01</span><h2>Suscripciones</h2></div>
                                <div class="form-field tablero-filtro"><asp:Label runat="server" AssociatedControlID="ddlTipoSuscripcion" Text="Tipo de suscripción" /><asp:DropDownList ID="ddlTipoSuscripcion" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlTablero_SelectedIndexChanged"><asp:ListItem Value="" Text="Todas" /><asp:ListItem Value="Flota SaaS" Text="Flota SaaS" /><asp:ListItem Value="Premium transportista" Text="Premium transportista" /><asp:ListItem Value="Protección de carga" Text="Protección de carga" /></asp:DropDownList></div>
                            </div>
                            <div class="tablero-totales">
                                <div class="total-bloque">
                                    <small>Total por mes</small>
                                    <strong><asp:Literal ID="litTotalSuscripciones" runat="server" /></strong>
                                    <span><asp:Literal ID="litDetalleSuscripciones" runat="server" /></span>
                                </div>
                                <%-- La torta reparte el total entre los tipos: solo se muestra con "Todas". --%>
                                <asp:Panel ID="pnlTortaSuscripciones" runat="server" CssClass="total-torta">
                                    <asp:Chart ID="chrSuscripcionesTipos" runat="server" BackColor="Transparent" Width="380px" Height="200px" AntiAliasing="All" TextAntiAliasingQuality="Normal" Palette="None" PaletteCustomColors="#123D32; #79B51C; #4A9A9A">
                                        <Series><asp:Series Name="Serie" ChartType="Doughnut" XValueMember="Nombre" YValueMembers="Monto" LegendText="#VALX  #PERCENT{P0}" BorderColor="White" BorderWidth="2" CustomProperties="DoughnutRadius=45, PieLabelStyle=Disabled" /></Series>
                                        <Legends><asp:Legend Name="Default" Docking="Right" BackColor="Transparent" Alignment="Center" LegendStyle="Column" MaximumAutoSize="70" TextWrapThreshold="40" IsTextAutoFit="false" ForeColor="#4C625B" Font="Segoe UI, 9pt" /></Legends>
                                        <ChartAreas><asp:ChartArea Name="Area" BackColor="Transparent" /></ChartAreas>
                                    </asp:Chart>
                                </asp:Panel>
                            </div>
                            <div class="admin-grid">
                                <div class="tablero-grafico"><h3>Suscripciones nuevas</h3>
                                    <asp:Chart ID="chrSuscripcionesCantidad" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                        <Series><asp:Series Name="Serie" ChartType="Column" XValueMember="Periodo" YValueMembers="Cantidad" Color="#79B51C" /></Series>
                                        <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" /></AxisY></asp:ChartArea></ChartAreas>
                                    </asp:Chart>
                                </div>
                                <div class="tablero-grafico"><h3>Plata por mes que representan</h3>
                                    <asp:Chart ID="chrSuscripcionesMonto" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                        <Series><asp:Series Name="Serie" ChartType="Line" BorderWidth="3" MarkerStyle="Circle" XValueMember="Periodo" YValueMembers="Monto" Color="#123D32" /></Series>
                                        <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" Format="C0" /></AxisY></asp:ChartArea></ChartAreas>
                                    </asp:Chart>
                                </div>
                            </div>
                            <details class="tablero-reporte"><summary>Ver reporte</summary>
                                <div class="table-responsive"><asp:GridView ID="gvSuscripciones" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None"><Columns><asp:BoundField DataField="Periodo" HeaderText="Período" /><asp:BoundField DataField="Cantidad" HeaderText="Suscripciones nuevas" /><asp:BoundField DataField="Monto" HeaderText="Plata por mes" DataFormatString="{0:C0}" /></Columns></asp:GridView></div>
                            </details>

                            <%-- Solo con "Flota SaaS": el abono se cobra por camión, así que se muestran las patentes. --%>
                            <asp:Panel ID="pnlPatentes" runat="server" Visible="false" CssClass="tablero-patentes">
                                <div class="admin-grid">
                                    <div class="tablero-grafico"><h3>Patentes registradas en Flota SaaS</h3>
                                        <asp:Chart ID="chrPatentesCantidad" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                            <Series><asp:Series Name="Serie" ChartType="Column" XValueMember="Periodo" YValueMembers="Cantidad" Color="#79B51C" /></Series>
                                            <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" /></AxisY></asp:ChartArea></ChartAreas>
                                        </asp:Chart>
                                    </div>
                                    <div class="tablero-grafico"><h3>Plata por mes que generan las patentes</h3>
                                        <asp:Chart ID="chrPatentesMonto" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                            <Series><asp:Series Name="Serie" ChartType="Line" BorderWidth="3" MarkerStyle="Circle" XValueMember="Periodo" YValueMembers="Monto" Color="#123D32" /></Series>
                                            <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" Format="C0" /></AxisY></asp:ChartArea></ChartAreas>
                                        </asp:Chart>
                                    </div>
                                </div>
                                <details class="tablero-reporte"><summary>Ver reporte de patentes</summary>
                                    <div class="table-responsive"><asp:GridView ID="gvPatentes" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None"><Columns><asp:BoundField DataField="Periodo" HeaderText="Período" /><asp:BoundField DataField="Cantidad" HeaderText="Patentes registradas" /><asp:BoundField DataField="Monto" HeaderText="Plata por mes" DataFormatString="{0:C0}" /></Columns></asp:GridView></div>
                                </details>
                            </asp:Panel>
                        </section>

                        <section class="admin-section">
                            <div class="section-title"><div><span>02</span><h2>Viajes</h2></div>
                                <div class="form-field tablero-filtro"><asp:Label runat="server" AssociatedControlID="ddlTipoCarga" Text="Tipo de carga" /><asp:DropDownList ID="ddlTipoCarga" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlTablero_SelectedIndexChanged"><asp:ListItem Value="Todas" Text="Todas" /><asp:ListItem Value="Ligera" Text="Carga ligera (hasta 3,5 t)" /><asp:ListItem Value="Media" Text="Carga media (3,5 a 12 t)" /><asp:ListItem Value="Pesada" Text="Carga pesada (12 t o más)" /></asp:DropDownList></div>
                            </div>
                            <div class="tablero-totales">
                                <div class="total-bloque">
                                    <small>Plata movida</small>
                                    <strong><asp:Literal ID="litTotalViajes" runat="server" /></strong>
                                    <span><asp:Literal ID="litDetalleViajes" runat="server" /></span>
                                </div>
                                <div class="total-bloque total-ganancia">
                                    <small>Ganancia de LogTech (7 %)</small>
                                    <strong><asp:Literal ID="litComisionViajes" runat="server" /></strong>
                                    <span>Comisiones de esos viajes</span>
                                </div>
                                <%-- La torta reparte la plata movida entre los tipos de carga: solo con "Todas". --%>
                                <asp:Panel ID="pnlTortaViajes" runat="server" CssClass="total-torta">
                                    <asp:Chart ID="chrViajesCargas" runat="server" BackColor="Transparent" Width="380px" Height="200px" AntiAliasing="All" TextAntiAliasingQuality="Normal" Palette="None" PaletteCustomColors="#C9EE8F; #79B51C; #123D32">
                                        <Series><asp:Series Name="Serie" ChartType="Doughnut" XValueMember="Nombre" YValueMembers="Monto" LegendText="#VALX  #PERCENT{P0}" BorderColor="White" BorderWidth="2" CustomProperties="DoughnutRadius=45, PieLabelStyle=Disabled" /></Series>
                                        <Legends><asp:Legend Name="Default" Docking="Right" BackColor="Transparent" Alignment="Center" LegendStyle="Column" MaximumAutoSize="70" TextWrapThreshold="40" IsTextAutoFit="false" ForeColor="#4C625B" Font="Segoe UI, 9pt" /></Legends>
                                        <ChartAreas><asp:ChartArea Name="Area" BackColor="Transparent" /></ChartAreas>
                                    </asp:Chart>
                                </asp:Panel>
                            </div>
                            <div class="admin-grid">
                                <div class="tablero-grafico"><h3>Viajes pedidos</h3>
                                    <asp:Chart ID="chrViajesCantidad" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                        <Series><asp:Series Name="Serie" ChartType="Column" XValueMember="Periodo" YValueMembers="Cantidad" Color="#79B51C" /></Series>
                                        <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" /></AxisY></asp:ChartArea></ChartAreas>
                                    </asp:Chart>
                                </div>
                                <div class="tablero-grafico"><h3>Plata movida en viajes</h3>
                                    <asp:Chart ID="chrViajesMonto" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                                        <Series><asp:Series Name="Serie" ChartType="Line" BorderWidth="3" MarkerStyle="Circle" XValueMember="Periodo" YValueMembers="Monto" Color="#123D32" /></Series>
                                        <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" Format="C0" /></AxisY></asp:ChartArea></ChartAreas>
                                    </asp:Chart>
                                </div>
                            </div>
                            <details class="tablero-reporte"><summary>Ver reporte</summary>
                                <div class="table-responsive"><asp:GridView ID="gvViajes" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None"><Columns><asp:BoundField DataField="Periodo" HeaderText="Período" /><asp:BoundField DataField="Cantidad" HeaderText="Viajes pedidos" /><asp:BoundField DataField="Monto" HeaderText="Plata movida" DataFormatString="{0:C0}" /></Columns></asp:GridView></div>
                            </details>
                        </section>
                    </asp:Panel>
                </asp:Panel>

                <asp:Panel ID="pnlCuenta" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light">MI CUENTA</span><h1>Cambiar contraseña</h1><p>Renová tu contraseña de acceso.</p></div></div>
                    <div class="admin-grid">
                        <section class="admin-card">
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtClaveActual" Text="Contraseña actual" /><asp:TextBox ID="txtClaveActual" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="current-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtClaveActual" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Clave" /></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtClaveNueva" Text="Nueva contraseña" /><asp:TextBox ID="txtClaveNueva" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtClaveNueva" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Clave" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtClaveNueva" ValidationExpression="^(?=.*[A-ZÀ-ÖØ-Þ])(?=.*[^A-Za-zÀ-ÖØ-öø-ÿ0-9\s]).{8,}$" ErrorMessage="Mínimo 8 caracteres, una mayúscula y un carácter especial." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Clave" /><small>Mínimo 8 caracteres, una mayúscula y un carácter especial.</small></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtClaveConfirmar" Text="Confirmar contraseña" /><asp:TextBox ID="txtClaveConfirmar" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtClaveConfirmar" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Clave" /><asp:CompareValidator runat="server" ControlToValidate="txtClaveConfirmar" ControlToCompare="txtClaveNueva" ErrorMessage="Las contraseñas no coinciden." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Clave" /></div>
                            <div class="botonera">
                                <asp:Button ID="btnCambiarClave" runat="server" CssClass="btn btn-brand" Text="Guardar contraseña" OnClick="btnCambiarClave_Click" CausesValidation="true" ValidationGroup="Clave" />
                                <asp:Button ID="btnVolverPerfil" runat="server" CssClass="btn btn-outline-dark" Text="Volver a Mi perfil" CausesValidation="false" OnClick="btnVolverPerfil_Click" />
                            </div>
                        </section>
                    </div>
                    <asp:Label ID="lblCuenta" runat="server" CssClass="panel-message" role="status" />
                </asp:Panel>

                <asp:Panel ID="pnlPerfil" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light">MI CUENTA</span><h1>Mi perfil</h1><p>Tus datos de contacto.</p></div></div>
                    <section class="admin-section">
                        <p>Usuario: <strong><asp:Literal ID="litPerfilUsuario" runat="server" /></strong> · Rol: <strong><asp:Literal ID="litPerfilRol" runat="server" /></strong></p>
                        <%-- Foto de perfil: se ve arriba a la izquierda, en la barra del panel. Solo JPG o PNG de hasta 2 MB. --%>
                        <div class="foto-perfil-form">
                            <asp:Image ID="imgFotoPerfil" runat="server" CssClass="foto-perfil foto-perfil-grande" AlternateText="Foto de perfil" Visible="false" />
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuFotoPerfil" Text="Foto de perfil (JPG o PNG, hasta 2 MB)" /><asp:FileUpload ID="fuFotoPerfil" runat="server" CssClass="form-control" accept=".jpg,.jpeg,.png" /></div>
                            <asp:Button ID="btnCambiarFoto" runat="server" CssClass="btn btn-outline-dark" Text="Cambiar foto" CausesValidation="false" OnClick="btnCambiarFoto_Click" />
                        </div>
                        <div class="admin-grid">
                            <section class="admin-card">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilNombre" Text="Nombre" /><asp:TextBox ID="txtPerfilNombre" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilNombre" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPerfilNombre" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilApellido" Text="Apellido" /><asp:TextBox ID="txtPerfilApellido" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilApellido" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPerfilApellido" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilCorreo" Text="Correo" /><asp:TextBox ID="txtPerfilCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100" data-tipo="correo" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilCorreo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPerfilCorreo" ValidationExpression="^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$" ErrorMessage="Ingresá un correo válido (ej.: nombre@empresa.com)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div>
                            </section>
                            <section class="admin-card">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilTelefono" Text="Teléfono" /><asp:TextBox ID="txtPerfilTelefono" runat="server" CssClass="form-control" MaxLength="10" data-tipo="digitos" inputmode="numeric" placeholder="Solo números" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilTelefono" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPerfilTelefono" ValidationExpression="^\d{10}$" ErrorMessage="El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlPerfilCondicionIva" Text="Condición frente al IVA" /><asp:DropDownList ID="ddlPerfilCondicionIva" runat="server" CssClass="form-select"><asp:ListItem Value="Responsable inscripto" Text="Responsable inscripto" /><asp:ListItem Value="Monotributista" Text="Monotributista" /><asp:ListItem Value="Exento" Text="Exento" /><asp:ListItem Value="Consumidor final" Text="Consumidor final" /></asp:DropDownList></div>
                                <asp:Panel ID="pnlPerfilRazonSocial" runat="server" Visible="false"><div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilRazonSocial" Text="Razón social" /><asp:TextBox ID="txtPerfilRazonSocial" runat="server" CssClass="form-control" MaxLength="120" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilRazonSocial" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div></asp:Panel>
                                <asp:Panel ID="pnlPerfilLicencia" runat="server" Visible="false"><div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPerfilLicencia" Text="Número de licencia" /><asp:TextBox ID="txtPerfilLicencia" runat="server" CssClass="form-control" MaxLength="40" data-tipo="licencia" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPerfilLicencia" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPerfilLicencia" ValidationExpression="^[A-Za-z0-9\-]{4,40}$" ErrorMessage="De 4 a 40 letras, números o guiones." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Perfil" /></div></asp:Panel>
                            </section>
                        </div>
                        <%-- «Cambiar contraseña» está acá y no en la barra del panel. --%>
                        <div class="botonera mt-4">
                            <asp:Button ID="btnGuardarPerfil" runat="server" CssClass="btn btn-brand" Text="Guardar cambios" OnClick="btnGuardarPerfil_Click" CausesValidation="true" ValidationGroup="Perfil" />
                            <asp:Button ID="btnIrCambiarClave" runat="server" CssClass="btn btn-outline-dark" Text="Cambiar contraseña" CausesValidation="false" OnClick="btnIrCambiarClave_Click" />
                        </div>
                        <asp:Label ID="lblPerfil" runat="server" CssClass="panel-message" role="status" />
                    </section>
                </asp:Panel>

                <asp:Panel ID="pnlUsuarios" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light">ADMINISTRACIÓN DEL SISTEMA</span><h1>Usuarios</h1><p>Consultá, modificá, bloqueá o dá de baja las cuentas.</p></div></div>
                    <section class="admin-section">
                        <div class="admin-filters">
                            <%-- Filtran solos: la lista al elegir un estado y el texto al salir del campo o al apretar Enter. --%>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtUsuarioFiltro" Text="Buscar" /><asp:TextBox ID="txtUsuarioFiltro" runat="server" CssClass="form-control" MaxLength="120" AutoPostBack="true" OnTextChanged="FiltrarUsuarios" placeholder="Nombre, usuario, correo o ID" /></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstadoFiltro" Text="Estado" /><asp:DropDownList ID="ddlEstadoFiltro" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="FiltrarUsuarios"><asp:ListItem Value="" Text="Todos" /><asp:ListItem Value="Activo" Text="Activo" /><asp:ListItem Value="Inactivo" Text="Inactivo" /><asp:ListItem Value="Bloqueado" Text="Bloqueado" /></asp:DropDownList></div>
                        </div>
                        <div class="table-responsive"><asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None" DataKeyNames="IdUsuario" EmptyDataText="Sin usuarios" OnRowCommand="gvUsuarios_RowCommand">
                            <Columns>
                                <asp:BoundField DataField="IdUsuario" HeaderText="ID" />
                                <asp:BoundField DataField="Usuario" HeaderText="Usuario" />
                                <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                                <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
                                <asp:BoundField DataField="Correo" HeaderText="Correo" />
                                <asp:BoundField DataField="Rol" HeaderText="Rol" />
                                <asp:BoundField DataField="Estado" HeaderText="Estado" />
                                <asp:BoundField DataField="IntentosFallidos" HeaderText="Intentos" />
                                <asp:TemplateField HeaderText="Acciones"><ItemTemplate>
                                    <asp:LinkButton runat="server" Text="Ver perfil" CommandName="Perfil" CommandArgument='<%# Eval("IdUsuario") %>' CausesValidation="false" />
                                    <asp:LinkButton runat="server" Text="Editar" CommandName="Editar" CommandArgument='<%# Eval("IdUsuario") %>' CausesValidation="false" />
                                    <asp:LinkButton runat="server" Text="Baja" CommandName="Baja" CommandArgument='<%# Eval("IdUsuario") %>' CausesValidation="false" Visible='<%# (string)Eval("Estado") != "Inactivo" && !EsMiCuenta(Eval("IdUsuario")) %>' OnClientClick="return confirm('¿Dar de baja (inactivar) esta cuenta?');" />
                                    <asp:LinkButton runat="server" Text="Bloquear" CommandName="Bloquear" CommandArgument='<%# Eval("IdUsuario") %>' CausesValidation="false" Visible='<%# (string)Eval("Estado") != "Bloqueado" && !EsMiCuenta(Eval("IdUsuario")) %>' OnClientClick="return confirm('¿Bloquear esta cuenta?');" />
                                    <asp:LinkButton runat="server" Text="Desbloquear" CommandName="Desbloquear" CommandArgument='<%# Eval("IdUsuario") %>' CausesValidation="false" Visible='<%# (string)Eval("Estado") != "Activo" %>' />
                                </ItemTemplate></asp:TemplateField>
                            </Columns>
                        </asp:GridView></div>
                        <asp:Label ID="lblUsuarios" runat="server" CssClass="panel-message" role="status" />
                    </section>
                    <%-- El perfil del usuario (rol o familia), sus familias y permisos, y si tiene permisos particulares. --%>
                    <asp:Panel ID="pnlPerfilUsuario" runat="server" Visible="false" CssClass="admin-section">
                        <h2>Perfil de <asp:Literal ID="litPerfilDe" runat="server" /></h2>
                        <p>Perfil: <strong><asp:Literal ID="litPerfilNombre" runat="server" /></strong> · <asp:Literal ID="litPerfilResumen" runat="server" /></p>
                        <asp:TreeView ID="tvPerfilUsuario" runat="server" ShowLines="true" />
                        <p class="mt-3"><strong>Permisos particulares:</strong> <asp:Literal ID="litPermisosParticulares" runat="server" /></p>
                        <%-- Solo dadores: la clave de WS_TrackingViajes (R46). La genera el administrador; el dador no la ve en su cuenta. --%>
                        <asp:Panel ID="pnlClaveApiUsuario" runat="server" Visible="false" CssClass="mt-4">
                            <h3>Clave de API (WS_TrackingViajes)</h3>
                            <p>Con esta clave, el sistema de la empresa del dador consulta el estado y la ubicación de sus viajes. La generás acá y se la pasás a la empresa. <asp:Literal ID="litClaveApiEstado" runat="server" /></p>
                            <asp:Panel ID="pnlClaveApiNueva" runat="server" Visible="false" CssClass="form-field"><asp:Label runat="server" AssociatedControlID="txtClaveApiNueva" Text="Clave nueva (se muestra una sola vez)" /><asp:TextBox ID="txtClaveApiNueva" runat="server" CssClass="form-control" ReadOnly="true" /></asp:Panel>
                            <div class="botonera">
                                <asp:Button ID="btnGenerarClaveApi" runat="server" CssClass="btn btn-brand" Text="Generar clave nueva" CausesValidation="false" OnClick="btnGenerarClaveApi_Click" OnClientClick="return confirm('¿Generar una clave nueva? La anterior deja de funcionar.');" />
                                <asp:Button ID="btnRevocarClaveApi" runat="server" CssClass="btn btn-outline-danger" Text="Revocar" CausesValidation="false" OnClick="btnRevocarClaveApi_Click" OnClientClick="return confirm('¿Revocar la clave? El sistema del dador deja de poder consultar sus viajes.');" />
                            </div>
                        </asp:Panel>
                        <div class="botonera"><asp:Button ID="btnCerrarPerfilUsuario" runat="server" CssClass="btn btn-outline-dark" Text="Cerrar" CausesValidation="false" OnClick="btnCerrarPerfilUsuario_Click" /></div>
                    </asp:Panel>
                    <asp:Panel ID="pnlEditarUsuario" runat="server" Visible="false" CssClass="admin-section">
                        <h2>Editar datos de <asp:Literal ID="litUsuarioEditado" runat="server" /></h2>
                        <div class="admin-grid">
                            <section class="admin-card">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditNombre" Text="Nombre" /><asp:TextBox ID="txtEditNombre" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditNombre" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtEditNombre" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditApellido" Text="Apellido" /><asp:TextBox ID="txtEditApellido" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditApellido" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtEditApellido" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditCorreo" Text="Correo" /><asp:TextBox ID="txtEditCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100" data-tipo="correo" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditCorreo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtEditCorreo" ValidationExpression="^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$" ErrorMessage="Ingresá un correo válido (ej.: nombre@empresa.com)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /></div>
                            </section>
                            <section class="admin-card">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditTelefono" Text="Teléfono" /><asp:TextBox ID="txtEditTelefono" runat="server" CssClass="form-control" MaxLength="10" data-tipo="digitos" inputmode="numeric" placeholder="Solo números" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditTelefono" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtEditTelefono" ValidationExpression="^\d{10}$" ErrorMessage="El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditRazonSocial" Text="Razón social" /><asp:TextBox ID="txtEditRazonSocial" runat="server" CssClass="form-control" MaxLength="120" /></div>
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtEditLicencia" Text="Número de licencia" /><asp:TextBox ID="txtEditLicencia" runat="server" CssClass="form-control" MaxLength="40" data-tipo="licencia" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtEditLicencia" ValidationExpression="^[A-Za-z0-9\-]{4,40}$" ErrorMessage="De 4 a 40 letras, números o guiones." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Usuario" /></div>
                            </section>
                        </div>
                        <div class="inline-form"><asp:Button ID="btnGuardarUsuario" runat="server" CssClass="btn btn-brand" Text="Guardar cambios" OnClick="btnGuardarUsuario_Click" CausesValidation="true" ValidationGroup="Usuario" /><asp:Button ID="btnCancelarEdicion" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarEdicion_Click" /></div>
                    <%-- Permisos del usuario (patrón Composite): el usuario es un compuesto con su rol y sus permisos particulares.
                             Se pueden agregar o quitar los particulares; los del perfil se cambian en «Roles y permisos». --%>
                        <section class="admin-card mt-4">
                            <h3>Permisos de <asp:Literal ID="litPermisosDe" runat="server" /></h3>
                            <p>Patrón Composite: el usuario es la raíz del árbol. Adentro está su rol, con sus familias, y colgando directo del usuario, las familias y los permisos que se le dieron a él, que se suman a los del rol. Para quitar uno, seleccionalo en el árbol.</p>
                            <asp:TreeView ID="tvPermisosUsuario" runat="server" ShowLines="true" />
                            <div class="inline-form mt-3">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlPermisoUsuario" Text="Permiso" /><asp:DropDownList ID="ddlPermisoUsuario" runat="server" CssClass="form-select" /></div>
                                <asp:Button ID="btnAgregarPermisoUsuario" runat="server" CssClass="btn btn-brand" Text="Agregar permiso" CausesValidation="false" OnClick="btnAgregarPermisoUsuario_Click" />
                            </div>
                            <div class="inline-form mt-2">
                                <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlFamiliaUsuario" Text="Familia" /><asp:DropDownList ID="ddlFamiliaUsuario" runat="server" CssClass="form-select" /></div>
                                <asp:Button ID="btnAgregarFamiliaUsuario" runat="server" CssClass="btn btn-brand" Text="Agregar familia" CausesValidation="false" OnClick="btnAgregarFamiliaUsuario_Click" />
                            </div>
                            <div class="botonera mt-2">
                                <asp:Button ID="btnEliminarPermisoUsuario" runat="server" CssClass="btn btn-outline-danger" Text="Quitar seleccionado" CausesValidation="false" OnClick="btnEliminarPermisoUsuario_Click"
                                    OnClientClick="return confirm('¿Quitarle al usuario el permiso o la familia seleccionada?');" />
                            </div>
                        </section>
                    </asp:Panel>
                </asp:Panel>

                <asp:Panel ID="pnlRoles" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light"><asp:Literal ID="litEtiquetaAdmin" runat="server" /></span><h1><asp:Literal ID="litTituloAdmin" runat="server" /></h1><p><asp:Literal ID="litBajadaAdmin" runat="server" /></p></div></div>
                    <div class="admin-grid">
                        <section id="secNuevoRol" runat="server" class="admin-card"><h2><asp:Literal ID="litNuevoRol" runat="server" /></h2><asp:Label runat="server" AssociatedControlID="ddlTipoComponente" Text="Tipo" /><asp:DropDownList ID="ddlTipoComponente" runat="server" CssClass="form-select"><asp:ListItem Value="Rol">Rol (perfil)</asp:ListItem><asp:ListItem Value="Familia">Familia de permisos</asp:ListItem></asp:DropDownList><asp:Label runat="server" AssociatedControlID="txtRol" Text="Nombre" /><div class="inline-form"><asp:TextBox ID="txtRol" runat="server" CssClass="form-control" MaxLength="50" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtRol" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Rol" /><asp:Button ID="btnAgregarRol" runat="server" CssClass="btn btn-brand" OnClick="btnAgregarRol_Click" CausesValidation="true" ValidationGroup="Rol" /></div></section>
                        <section class="admin-card"><h2>Editar rol o familia</h2><asp:Label runat="server" AssociatedControlID="ddlRoles" Text="Rol o familia a editar" /><asp:DropDownList ID="ddlRoles" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlRoles_SelectedIndexChanged" /><asp:Label ID="lblPermisosRol" runat="server" AssociatedControlID="ddlPermisos"><asp:Literal ID="litNuevoPermiso" runat="server" /></asp:Label><div class="inline-form"><asp:DropDownList ID="ddlPermisos" runat="server" CssClass="form-select" /><asp:Button ID="btnAgregarPermiso" runat="server" CssClass="btn btn-brand" OnClick="btnAgregarPermiso_Click" /></div><asp:Label ID="lblFamiliasRol" runat="server" AssociatedControlID="ddlFamilias" Text="Familia que querés incluir" /><div class="inline-form"><asp:DropDownList ID="ddlFamilias" runat="server" CssClass="form-select" /><asp:Button ID="btnAgregarFamilia" runat="server" Text="Agregar familia" CssClass="btn btn-brand" OnClick="btnAgregarFamilia_Click" /></div><p>Los cambios se guardan al agregar. Una familia puede contener permisos y otras familias.</p></section>
                    </div>
                    <asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />
                    <section class="admin-section"><h2>Contenido del rol o familia</h2><p>Seleccioná una rama directa para quitarla. Quitar una familia de este árbol no elimina la familia ni sus permisos.</p><asp:TreeView ID="tvPermisos" runat="server" ShowLines="true" /><div class="inline-form"><asp:Button ID="btnQuitarElemento" runat="server" Text="Quitar elemento seleccionado" CssClass="btn btn-outline-dark" OnClick="btnQuitarElemento_Click" CausesValidation="false" /><asp:Button ID="btnEliminarRol" runat="server" Text="Eliminar rol o familia seleccionada" CssClass="btn btn-outline-danger" CausesValidation="false" OnClick="btnEliminarRol_Click" OnClientClick="return confirm('¿Eliminar el rol o familia seleccionado y todos sus vínculos? Los grupos que lo incluyen dejarán de heredar sus permisos. Sus permisos y familias internas no se borrarán. Esta acción no se puede deshacer desde la pantalla.');" /></div></section>
                    <section class="admin-section"><div class="section-title"><div><span>01</span><h2><asp:Literal ID="litRoles" runat="server" /></h2></div></div><div class="roles-grid"><asp:Repeater ID="rptRoles" runat="server"><ItemTemplate><article class="role-card"><div class="role-card-icon"><%#: Eval("Inicial") %></div><h3><%#: Eval("Nombre") %></h3><p><%#: Eval("PermisosTexto") %></p></article></ItemTemplate></asp:Repeater></div></section>
                </asp:Panel>

                <asp:Panel ID="pnlBitacora" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light">AUDITORÍA</span><h1><asp:Literal ID="litBitacora" runat="server" /></h1><p>Eventos registrados por el sistema.</p></div></div>
                    <section class="admin-section audit-section">
                        <div class="section-title"><div><span>01</span><h2>Eventos</h2></div><small>10 eventos por página</small></div>
                        <div class="admin-filters">
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlBitacoraOrigen" Text="Período" /><asp:DropDownList ID="ddlBitacoraOrigen" runat="server" CssClass="form-select"><asp:ListItem Value="" Text="Últimos 30 días" /><asp:ListItem Value="Historial" Text="Historial XML (más de 30 días)" /></asp:DropDownList></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBitacoraDesde" Text="Desde" /><asp:TextBox ID="txtBitacoraDesde" runat="server" CssClass="form-control" TextMode="Date" /></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBitacoraHasta" Text="Hasta" /><asp:TextBox ID="txtBitacoraHasta" runat="server" CssClass="form-control" TextMode="Date" /></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBitacoraUsuario" Text="Usuario" /><asp:TextBox ID="txtBitacoraUsuario" runat="server" CssClass="form-control" MaxLength="50" /></div>
                            <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlBitacoraCriticidad" Text="Criticidad" /><asp:DropDownList ID="ddlBitacoraCriticidad" runat="server" CssClass="form-select"><asp:ListItem Value="" Text="Todas" /><asp:ListItem Value="Informativa" Text="Informativa" /><asp:ListItem Value="Advertencia" Text="Advertencia" /><asp:ListItem Value="Crítica" Text="Crítica" /></asp:DropDownList></div>
                            <asp:Button ID="btnFiltrarBitacora" runat="server" CssClass="btn btn-brand" Text="Filtrar" CausesValidation="false" OnClick="btnFiltrarBitacora_Click" />
                            <asp:Button ID="btnExportarBitacora" runat="server" CssClass="btn btn-outline-dark" Text="Exportar XML" CausesValidation="false" OnClick="btnExportarBitacora_Click" />
                        </div>
                        <asp:Label ID="lblBitacora" runat="server" CssClass="panel-message" role="status" />
                        <div class="table-responsive"><asp:GridView ID="gvBitacora" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None" EmptyDataText="Sin eventos"><Columns><asp:BoundField DataField="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm:ss}" /><asp:BoundField DataField="IdUsuario" /><asp:BoundField DataField="Usuario" /><asp:BoundField DataField="Descripcion" /><asp:BoundField DataField="Criticidad" /></Columns></asp:GridView></div>
                        <div class="audit-pager"><asp:LinkButton ID="btnAnterior" runat="server" CssClass="btn btn-outline-dark btn-sm" CausesValidation="false" OnClick="btnAnterior_Click" /><span><asp:Literal ID="litPagina" runat="server" /></span><asp:LinkButton ID="btnSiguiente" runat="server" CssClass="btn btn-outline-dark btn-sm" CausesValidation="false" OnClick="btnSiguiente_Click" /></div>
                    </section>
                </asp:Panel>

                <asp:Panel ID="pnlBaseDatos" runat="server" Visible="false">
                    <div class="panel-heading"><div><span class="eyebrow eyebrow-light">MANTENIMIENTO</span><h1>Base de datos</h1><p>Copia de seguridad y restauración de LogTechDB.</p></div></div>
                    <div class="admin-grid">
                        <section id="secBackup" runat="server" class="admin-card">
                            <h2>Generar backup</h2>
                            <p>Crea un archivo .bak con toda la base y lo descarga.</p>
                            <asp:Button ID="btnGenerarBackup" runat="server" CssClass="btn btn-brand" Text="Generar backup" OnClick="btnGenerarBackup_Click" />
                        </section>
                        <section id="secRestaurar" runat="server" class="admin-card">
                            <h2>Restaurar backup</h2>
                            <p>Reemplaza la base actual con el contenido del archivo. Los datos actuales se pierden.</p>
                            <asp:FileUpload ID="fuBackup" runat="server" CssClass="form-control mb-3" accept=".bak" />
                            <asp:Button ID="btnRestaurar" runat="server" CssClass="btn btn-outline-danger" Text="Restaurar" OnClick="btnRestaurar_Click"
                                OnClientClick="return confirm('Esto reemplaza toda la base de datos actual y no se puede deshacer. ¿Continuar?');" />
                        </section>
                    </div>
                    <asp:Label ID="lblMantenimiento" runat="server" CssClass="panel-message" role="status" />
                    <section class="admin-section">
                        <h2>Verificar integridad</h2>
                        <p>Compara los dígitos verificadores (DVH y DVV) con los datos actuales y muestra qué filas o tablas fueron alteradas.</p>
                        <div class="botonera">
                            <asp:Button ID="btnVerificarIntegridad" runat="server" CssClass="btn btn-outline-dark" Text="Verificar integridad" OnClick="btnVerificarIntegridad_Click" />
                            <asp:Button ID="btnRecalcular" runat="server" CssClass="btn btn-brand" Text="Recalcular dígitos verificadores" CausesValidation="false" OnClick="btnRecalcular_Click" />
                        </div>
                        <div class="table-responsive"><asp:GridView ID="gvIntegridad" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None" Visible="false"><Columns><asp:BoundField DataField="Tabla" HeaderText="Tabla" /><asp:BoundField DataField="Fila" HeaderText="Registro" NullDisplayText="-" /><asp:BoundField DataField="Detalle" HeaderText="Detalle" /></Columns></asp:GridView></div>
                    </section>
                </asp:Panel>

                <uc:MisCargas ID="ucMisCargas" runat="server" Visible="false" />
                <uc:BolsaCargas ID="ucBolsaCargas" runat="server" Visible="false" />
                <uc:MisViajes ID="ucMisViajes" runat="server" Visible="false" />
                <uc:Vehiculos ID="ucVehiculos" runat="server" Visible="false" />
                <uc:Conductores ID="ucConductores" runat="server" Visible="false" />
                <uc:Documentacion ID="ucDocumentacion" runat="server" Visible="false" />
                <uc:ValidarDocumentacion ID="ucValidarDocumentacion" runat="server" Visible="false" />
                <uc:Remitos ID="ucRemitos" runat="server" Visible="false" />
                <uc:ReporteFlota ID="ucReporteFlota" runat="server" Visible="false" />
                <uc:Billetera ID="ucBilletera" runat="server" Visible="false" />
                <uc:Seguros ID="ucSeguros" runat="server" Visible="false" />
                <uc:Facturas ID="ucFacturas" runat="server" Visible="false" />
                <uc:Suscripcion ID="ucSuscripcion" runat="server" Visible="false" />
                <uc:Disputas ID="ucDisputas" runat="server" Visible="false" />
                <uc:ViajesAdmin ID="ucViajesAdmin" runat="server" Visible="false" />
            </section>
        </div>
    </main>
    <script>
        (function () {
            var toggle = document.getElementById('panelMenuToggle');
            var sidebar = document.getElementById('panelSidebar');
            var overlay = document.getElementById('panelMenuOverlay');
            function cambiarMenu() {
                var abierto = sidebar.classList.toggle('is-open');
                overlay.classList.toggle('is-open', abierto);
                toggle.setAttribute('aria-expanded', abierto ? 'true' : 'false');
            }
            toggle.addEventListener('click', cambiarMenu);
            overlay.addEventListener('click', cambiarMenu);
        }());
    </script>
    <script>
        // R4 / R18: si la pantalla tiene campos de ubicación, el navegador los completa con la
        // posición actual (si el usuario da permiso). Sin permiso quedan vacíos y el sistema avisa.
        (function () {
            var latitudes = document.querySelectorAll('input[type=hidden][id$=hfLatitud]');
            if (latitudes.length === 0 || !navigator.geolocation) return;
            navigator.geolocation.getCurrentPosition(function (posicion) {
                for (var i = 0; i < latitudes.length; i++) {
                    var longitud = document.getElementById(latitudes[i].id.replace(/hfLatitud$/, 'hfLongitud'));
                    latitudes[i].value = posicion.coords.latitude.toFixed(6);
                    if (longitud) longitud.value = posicion.coords.longitude.toFixed(6);
                }
            }, function () { }, { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 });
        }());
    </script>
</asp:Content>
