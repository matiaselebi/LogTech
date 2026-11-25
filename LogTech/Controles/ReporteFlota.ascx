<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ReporteFlota.ascx.cs" Inherits="LogTech.Controles.ReporteFlota" %>
<%@ Register Assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>
<%-- Administrador de flota: CU-008-044 Generar Reporte Gerencial (el reporte de su flota). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">MI FLOTA</span><h1>Reporte de la flota</h1><p>Los viajes que entregó tu flota (o uno de tus vehículos), lo que cobró y cómo rinde cada conductor.</p></div>
    <div class="tablero-acciones">
        <div class="form-field tablero-filtro"><asp:Label runat="server" AssociatedControlID="ddlPeriodo" Text="Ver por" /><asp:DropDownList ID="ddlPeriodo" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlPeriodo_SelectedIndexChanged"><asp:ListItem Value="Diario" Text="Día (últimos 30 días)" /><asp:ListItem Value="Mensual" Text="Mes (últimos 12 meses)" Selected="True" /><asp:ListItem Value="Anual" Text="Año (últimos 5 años)" /></asp:DropDownList></div>
        <div class="form-field tablero-filtro"><asp:Label runat="server" AssociatedControlID="ddlVehiculo" Text="Vehículo" /><asp:DropDownList ID="ddlVehiculo" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlPeriodo_SelectedIndexChanged" /></div>
        <asp:Button ID="btnExportar" runat="server" CssClass="btn btn-brand" Text="Exportar reporte (CSV)" CausesValidation="false" OnClick="btnExportar_Click" />
    </div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<div class="tablero-kpis kpis-flota">
    <article class="kpi-card"><small>Viajes entregados</small><strong><asp:Literal ID="litViajes" runat="server" /></strong><span><asp:Literal ID="litPeriodo" runat="server" /></span></article>
    <article class="kpi-card"><small>Cobrado por la flota</small><strong><asp:Literal ID="litNeto" runat="server" /></strong><span>Lo que pagaron los dadores menos la comisión y el remito de LogTech</span></article>
    <article class="kpi-card"><small>Facturado a los dadores</small><strong><asp:Literal ID="litFacturado" runat="server" /></strong><span>Para LogTech (comisión y remitos): <asp:Literal ID="litComision" runat="server" /></span></article>
    <article class="kpi-card"><small>Kilómetros recorridos</small><strong><asp:Literal ID="litKilometros" runat="server" /></strong><span><asp:Literal ID="litToneladas" runat="server" /> toneladas transportadas</span></article>
    <article class="kpi-card"><small>Incidencias</small><strong><asp:Literal ID="litIncidencias" runat="server" /></strong><span>Reportadas en esos viajes</span></article>
    <article class="kpi-card"><small>Viajes en curso</small><strong><asp:Literal ID="litEnCurso" runat="server" /></strong><span>Ahora, entre todos los conductores</span></article>
    <article class="kpi-card"><small>Gastos del mes</small><strong><asp:Literal ID="litGastos" runat="server" /></strong><span><asp:Literal ID="litGastosDetalle" runat="server" /></span></article>
</div>

<section class="admin-section">
    <div class="section-title"><div><span>01</span><h2>Viajes de la flota</h2></div></div>
    <div class="admin-grid">
        <div class="tablero-grafico"><h3>Viajes entregados</h3>
            <asp:Chart ID="chrViajes" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                <Series><asp:Series Name="Serie" ChartType="Column" XValueMember="Periodo" YValueMembers="Cantidad" Color="#79B51C" /></Series>
                <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" /></AxisY></asp:ChartArea></ChartAreas>
            </asp:Chart>
        </div>
        <div class="tablero-grafico"><h3>Cobrado por la flota</h3>
            <asp:Chart ID="chrNeto" runat="server" Width="560px" Height="280px" AntiAliasing="All" TextAntiAliasingQuality="Normal">
                <Series><asp:Series Name="Serie" ChartType="Line" BorderWidth="3" MarkerStyle="Circle" XValueMember="Periodo" YValueMembers="Monto" Color="#123D32" /></Series>
                <ChartAreas><asp:ChartArea Name="Area"><AxisX Interval="1" LineColor="#DFE7E2"><MajorGrid Enabled="false" /><LabelStyle ForeColor="#4C625B" /></AxisX><AxisY LineColor="#DFE7E2"><MajorGrid LineColor="#EEF2EF" /><LabelStyle ForeColor="#4C625B" Format="C0" /></AxisY></asp:ChartArea></ChartAreas>
            </asp:Chart>
        </div>
    </div>
    <details class="tablero-reporte"><summary>Ver reporte por período</summary>
        <div class="table-responsive"><asp:GridView ID="gvPeriodos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table" GridLines="None"><Columns><asp:BoundField DataField="Periodo" HeaderText="Período" /><asp:BoundField DataField="Cantidad" HeaderText="Viajes entregados" /><asp:BoundField DataField="Monto" HeaderText="Cobrado por la flota" DataFormatString="{0:C0}" /></Columns></asp:GridView></div>
    </details>
</section>

<section class="admin-section">
    <div class="section-title"><div><span>02</span><h2>Conductores</h2><p>Lo que hizo cada conductor en el período, del que más cobró al que menos.</p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvConductores" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="La flota no tiene conductores activos ni viajes en el período.">
        <Columns>
            <asp:BoundField DataField="Conductor" HeaderText="Conductor" />
            <asp:BoundField DataField="Viajes" HeaderText="Viajes entregados" />
            <asp:BoundField DataField="Toneladas" HeaderText="Toneladas" DataFormatString="{0:N1}" />
            <asp:BoundField DataField="Kilometros" HeaderText="Kilómetros" DataFormatString="{0:N0}" />
            <asp:BoundField DataField="Neto" HeaderText="Cobrado" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="Incidencias" HeaderText="Incidencias" />
            <asp:BoundField DataField="EnCurso" HeaderText="En curso" />
            <asp:BoundField DataField="UltimoViaje" HeaderText="Último viaje" DataFormatString="{0:dd/MM/yyyy}" NullDisplayText="-" />
        </Columns>
    </asp:GridView></div>
</section>
