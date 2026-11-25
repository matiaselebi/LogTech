<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Remitos.ascx.cs" Inherits="LogTech.Controles.Remitos" %>
<%@ Register Src="~/Controles/DetalleViaje.ascx" TagPrefix="uc" TagName="DetalleViaje" %>
<%-- Transportista, flota y dador: CU-004-023 Consultar Remito Digital. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">COMPROBANTES</span><h1>Remitos digitales</h1><p>Comprobantes de entrega de tus viajes, protegidos con un token SHA-256.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<section class="admin-section">
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBuscar" Text="Buscar (origen, destino, receptor o N° de viaje)" /><asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control" MaxLength="60" /></div>
        <asp:Button ID="btnBuscar" runat="server" CssClass="btn btn-brand" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
    </div>
    <div class="table-responsive"><asp:GridView ID="gvRemitos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay remitos que coincidan con la búsqueda." OnRowCommand="gvRemitos_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="Viaje N°" />
            <asp:BoundField DataField="FechaEmision" HeaderText="Emitido" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Mercaderia" HeaderText="Mercadería" />
            <asp:BoundField DataField="Receptor" HeaderText="Recibió" />
            <asp:TemplateField HeaderText="Integridad"><ItemTemplate><span class='<%# (bool)Eval("Valido") ? "doc-estado doc-aprobado" : "doc-estado doc-rechazado" %>'><%# (bool)Eval("Valido") ? "Verificado" : "Alterado" %></span></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Ver remito" CommandName="Ver" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" /><br />
                <asp:LinkButton runat="server" Text="Descargar XML" CommandName="Xml" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" /><br />
                <%-- Se abre en otra pestaña: el formulario se envía a _blank solo para este clic. --%>
                <asp:LinkButton runat="server" Text="Imprimir" CommandName="Imprimir" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false"
                    OnClientClick="var f = document.forms[0]; f.target = '_blank'; setTimeout(function () { f.target = ''; }, 500);" />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<uc:DetalleViaje ID="ucDetalle" runat="server" />
