<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Facturas.ascx.cs" Inherits="LogTech.Controles.Facturas" %>
<%-- CU-008-039 Generar Factura Corporativa: el cliente ve y descarga sus facturas; el administrador ve las de cada mes,
     exporta ReporteFacturacion.xml y vuelve a pedir el CAE de las pendientes (ERR022). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">PAGOS</span><h1>Facturas</h1><p>Facturas electrónicas de LogTech, autorizadas por ARCA con su CAE. Los precios incluyen el IVA.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlAdministrador" runat="server" CssClass="admin-section" Visible="false">
    <div class="filtros-modulo">
        <%-- Muestra el mes apenas se elige (AutoPostBack). --%>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlMes" Text="Mes" /><asp:DropDownList ID="ddlMes" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlMes_SelectedIndexChanged" /></div>
        <asp:Button ID="btnExportarXml" runat="server" CssClass="btn btn-outline-dark" Text="Exportar ReporteFacturacion.xml" CausesValidation="false" OnClick="btnExportarXml_Click" />
        <asp:Button ID="btnReintentar" runat="server" CssClass="btn btn-outline-dark" Text="Reintentar CAE pendientes" CausesValidation="false" OnClick="btnReintentar_Click" />
    </div>
    <div class="tablero-kpis kpis-flota">
        <article class="kpi-card"><small>Facturas del mes</small><strong><asp:Literal ID="litCantidad" runat="server" /></strong><span><asp:Literal ID="litPendientes" runat="server" /></span></article>
        <article class="kpi-card"><small>Total facturado</small><strong><asp:Literal ID="litTotal" runat="server" /></strong><span>Con IVA incluido</span></article>
        <article class="kpi-card"><small>IVA del mes</small><strong><asp:Literal ID="litIva" runat="server" /></strong><span>21 %</span></article>
    </div>
</asp:Panel>

<section class="admin-section">
    <div class="table-responsive"><asp:GridView ID="gvFacturas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no hay facturas." OnRowCommand="gvFacturas_RowCommand">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="Comprobante" HeaderText="Comprobante" ItemStyle-Wrap="false" />
            <asp:BoundField DataField="Cliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Servicio" HeaderText="Servicio" />
            <asp:BoundField DataField="Detalle" HeaderText="Detalle" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:C2}" ItemStyle-Wrap="false" />
            <asp:BoundField DataField="Cae" HeaderText="CAE" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='<%# (string)Eval("Estado") == "Emitida" ? "doc-estado doc-aprobado" : "doc-estado doc-pendiente" %>'><%#: Eval("Estado") %></span></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="PDF"><ItemTemplate><asp:LinkButton runat="server" Text="Descargar" CommandName="Pdf" CommandArgument='<%# Eval("IdFactura") %>' CausesValidation="false" Visible='<%# (string)Eval("Estado") == "Emitida" %>' /></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>
