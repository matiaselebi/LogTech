<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ViajesAdmin.ascx.cs" Inherits="LogTech.Controles.ViajesAdmin" %>
<%@ Register Src="~/Controles/EstadoViajeBadge.ascx" TagPrefix="uc" TagName="EstadoViajeBadge" %>
<%-- Administrador: los viajes de toda la plataforma y la plata retenida en escrow (CU-005-025). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">SUPERVISIÓN</span><h1>Viajes</h1><p>Viajes pedidos, viajes en curso y la plata retenida en escrow hasta cada remito.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<div class="tablero-kpis kpis-flota">
    <article class="kpi-card"><small>Fondos en escrow</small><strong><asp:Literal ID="litEscrow" runat="server" /></strong><span><asp:Literal ID="litEscrowDetalle" runat="server" /></span></article>
    <article class="kpi-card"><small>Viajes solicitados</small><strong><asp:Literal ID="litSolicitados" runat="server" /></strong><span>Esperando que un transportista los acepte</span></article>
    <article class="kpi-card"><small>Viajes en progreso</small><strong><asp:Literal ID="litEnCurso" runat="server" /></strong><span>De Asignado a En destino</span></article>
</div>

<asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="admin-section">
    <div class="section-title">
        <div><h2>Viaje N° <asp:Literal ID="litDetalleViaje" runat="server" /></h2></div>
        <asp:LinkButton ID="btnCerrarDetalle" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Cerrar" CausesValidation="false" OnClick="btnCerrarDetalle_Click" />
    </div>
    <div class="kpi-card"><small>Entrega estimada</small><strong><asp:Literal ID="litEntrega" runat="server" /></strong><span><asp:Literal ID="litEntregaDetalle" runat="server" /></span></div>
    <dl class="remito-datos"><asp:Literal ID="litDatos" runat="server" /></dl>
</asp:Panel>

<section class="admin-section">
    <div class="section-title"><div><h2>Viajes en progreso</h2></div></div>
    <div class="table-responsive"><asp:GridView ID="gvEnCurso" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay viajes en curso." OnRowCommand="gvEnCurso_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="N°" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Dador" HeaderText="Dador de carga" />
            <asp:BoundField DataField="Transportista" HeaderText="Transportista" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><uc:EstadoViajeBadge runat="server" Estado='<%# Eval("Estado") %>' /></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Pago"><ItemTemplate><%#: Eval("MedioPago") %><br /><small><%#: Eval("EstadoPago") %></small></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="En escrow"><ItemTemplate><%#: EnEscrow(Container.DataItem) %></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Acción"><ItemTemplate><asp:LinkButton runat="server" Text="Detalles" CommandName="Detalles" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" /></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<section class="admin-section">
    <div class="section-title"><div><h2>Viajes solicitados</h2></div></div>
    <div class="table-responsive"><asp:GridView ID="gvSolicitados" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay viajes esperando transportista.">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="N°" />
            <asp:BoundField DataField="FechaPedido" HeaderText="Pedido" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="FechaCarga" HeaderText="Carga" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Dador" HeaderText="Dador de carga" />
            <asp:BoundField DataField="PesoToneladas" HeaderText="Peso (t)" DataFormatString="{0:0.##}" />
            <asp:BoundField DataField="PrecioDador" HeaderText="Precio" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="MedioPago" HeaderText="Pago" />
        </Columns>
    </asp:GridView></div>
</section>
