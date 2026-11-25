<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="BolsaCargas.ascx.cs" Inherits="LogTech.Controles.BolsaCargas" %>
<%-- Transportista y flota: CU-001-004 (consultar), CU-001-005 (aceptar) y CU-001-006 (match). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">OPERACIÓN LOGÍSTICA</span><h1>Bolsa de cargas</h1><p>Buscá cargas pendientes, encontrá las más cercanas y aceptá el viaje.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<section class="admin-section">
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlOrigen" Text="Origen" /><asp:DropDownList ID="ddlOrigen" runat="server" CssClass="form-select" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlDestino" Text="Destino" /><asp:DropDownList ID="ddlDestino" runat="server" CssClass="form-select" /></div>
        <asp:Button ID="btnBuscar" runat="server" CssClass="btn btn-brand" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
    </div>
    <asp:Panel ID="pnlMatch" runat="server" CssClass="filtros-modulo match-filtros">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlReferencia" Text="Buscar cerca de" /><asp:DropDownList ID="ddlReferencia" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlReferencia_SelectedIndexChanged" /></div>
        <%-- El navegador completa la ubicación (R4) en estos campos ocultos. --%>
        <asp:HiddenField ID="hfLatitud" runat="server" /><asp:HiddenField ID="hfLongitud" runat="server" />
    </asp:Panel>
    <div class="table-responsive"><asp:GridView ID="gvBolsa" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay cargas disponibles que coincidan con la búsqueda." OnRowCommand="gvBolsa_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="N°" />
            <asp:BoundField DataField="FechaCarga" HeaderText="Carga" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="DistanciaKm" HeaderText="Distancia al origen" DataFormatString="{0:N0} km" NullDisplayText="-" Visible="false" />
            <asp:BoundField DataField="Mercaderia" HeaderText="Mercadería" />
            <asp:BoundField DataField="PesoToneladas" HeaderText="Peso (t)" DataFormatString="{0:0.##}" />
            <asp:BoundField DataField="NetoTransportista" HeaderText="Cobrás" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="MedioPago" HeaderText="Pago" />
            <asp:BoundField DataField="Dador" HeaderText="Dador de carga" />
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Aceptar" CommandName="Aceptar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Tiene("Aceptar ofertas de carga") %>' />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<asp:Panel ID="pnlAceptar" runat="server" Visible="false" CssClass="admin-section">
    <h2>Aceptar la oferta N° <asp:Literal ID="litAceptarViaje" runat="server" /></h2>
    <p><asp:Literal ID="litAceptarDetalle" runat="server" /></p>
    <asp:Panel ID="pnlVehiculo" runat="server" CssClass="form-field">
        <asp:Label runat="server" AssociatedControlID="ddlVehiculo" Text="Vehículo con el que vas a hacer el viaje" />
        <asp:DropDownList ID="ddlVehiculo" runat="server" CssClass="form-select" />
        <small><asp:Literal ID="litAvisoVehiculos" runat="server" /></small>
    </asp:Panel>
    <asp:Panel ID="pnlAvisoFlota" runat="server"><p>Después de aceptar, asigná el conductor y el vehículo desde "Mis viajes".</p></asp:Panel>
    <div class="botonera">
        <asp:Button ID="btnConfirmarAceptar" runat="server" CssClass="btn btn-brand" Text="Confirmar y tomar el viaje" OnClick="btnConfirmarAceptar_Click" OnClientClick="return confirm('¿Confirmás que vas a hacer este viaje?');" />
        <asp:Button ID="btnCancelarAceptar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarAceptar_Click" />
    </div>
</asp:Panel>
