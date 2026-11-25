<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Disputas.ascx.cs" Inherits="LogTech.Controles.Disputas" %>
<%-- CU-005-027 Gestionar Disputa Comercial: el dador la abre y el administrador la resuelve. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">PAGOS</span><h1>Disputas</h1><p><asp:Literal ID="litBajada" runat="server" /></p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlAbrir" runat="server" CssClass="admin-section">
    <h2>Abrir una disputa</h2>
    <p>Solo por viajes pagados con tarjeta o con saldo, mientras el pago está retenido (antes del remito). El pago queda congelado hasta que lo resuelva el administrador.</p>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlViaje" Text="Viaje" /><asp:DropDownList ID="ddlViaje" runat="server" CssClass="form-select" /></div>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtMotivo" Text="Qué pasó" /><asp:TextBox ID="txtMotivo" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="500" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMotivo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Disputa" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtMotivo" ValidationExpression="^[\s\S]{10,500}$" ErrorMessage="Entre 10 y 500 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Disputa" /></div>
    <asp:Button ID="btnAbrir" runat="server" CssClass="btn btn-brand" Text="Abrir disputa" CausesValidation="true" OnClick="btnAbrir_Click" OnClientClick="if (!Page_ClientValidate('Disputa') || !confirm('¿Abrir la disputa? El pago queda congelado hasta que la resuelva el administrador.')) return false;" ValidationGroup="Disputa" />
</asp:Panel>

<section class="admin-section">
    <div class="table-responsive"><asp:GridView ID="gvDisputas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay disputas." OnRowCommand="gvDisputas_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="Viaje" />
            <asp:BoundField DataField="Recorrido" HeaderText="Recorrido" />
            <asp:BoundField DataField="Dador" HeaderText="Dador de carga" />
            <asp:BoundField DataField="Transportista" HeaderText="Transportista" NullDisplayText="-" />
            <asp:BoundField DataField="Monto" HeaderText="Pago" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="Fecha" HeaderText="Abierta el" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="Motivo" HeaderText="Motivo" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:BoundField DataField="Resolucion" HeaderText="Resolución" NullDisplayText="-" />
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Resolver" CommandName="Resolver" CommandArgument='<%# Eval("IdDisputa") %>' CausesValidation="false" Visible='<%# PuedeResolver && Eval("Estado").ToString() == "Abierta" %>' />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<asp:Panel ID="pnlResolver" runat="server" Visible="false" CssClass="admin-section">
    <h2>Resolver la disputa del viaje N° <asp:Literal ID="litResolverViaje" runat="server" /></h2>
    <p><asp:Literal ID="litResolverDetalle" runat="server" /></p>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtComentario" Text="Motivo de la decisión" /><asp:TextBox ID="txtComentario" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="500" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtComentario" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Resolucion" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtComentario" ValidationExpression="^[\s\S]{5,500}$" ErrorMessage="Entre 5 y 500 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Resolucion" /></div>
    <div class="botonera">
        <asp:Button ID="btnDevolver" runat="server" CssClass="btn btn-brand" Text="Devolver al dador" CausesValidation="true" OnClick="btnDevolver_Click" OnClientClick="if (!Page_ClientValidate('Resolucion') || !confirm('¿Devolverle el pago al dador?')) return false;" ValidationGroup="Resolucion" />
        <asp:Button ID="btnPagar" runat="server" CssClass="btn btn-outline-dark" Text="Pagar al transportista" CausesValidation="true" OnClick="btnPagar_Click" OnClientClick="if (!Page_ClientValidate('Resolucion') || !confirm('¿Pagarle al transportista?')) return false;" ValidationGroup="Resolucion" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>
