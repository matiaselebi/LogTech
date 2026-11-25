<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ValidarDocumentacion.ascx.cs" Inherits="LogTech.Controles.ValidarDocumentacion" %>
<%-- Analista de operaciones (Administrador): CU-003-019 (todos los legajos) y CU-003-020. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">ANÁLISIS DE OPERACIONES</span><h1>Validar documentación</h1><p>Revisá los documentos que subieron los usuarios y aprobalos o rechazalos.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<section class="admin-section">
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstado" Text="Estado" />
            <%-- Filtra apenas se elige un estado (AutoPostBack). --%>
            <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlEstado_SelectedIndexChanged">
                <asp:ListItem Value="Pendiente" Text="Pendientes" /><asp:ListItem Value="Aprobado" Text="Aprobados" /><asp:ListItem Value="Rechazado" Text="Rechazados" /><asp:ListItem Value="Vencido" Text="Vencidos" /><asp:ListItem Value="" Text="Todos" />
            </asp:DropDownList>
        </div>
    </div>
    <div class="table-responsive"><asp:GridView ID="gvDocumentos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay documentos en este estado." OnRowCommand="gvDocumentos_RowCommand">
        <Columns>
            <asp:BoundField DataField="Usuario" HeaderText="Usuario" />
            <asp:BoundField DataField="Tipo" HeaderText="Documento" />
            <asp:BoundField DataField="DeQuien" HeaderText="Vehículo o conductor" />
            <asp:BoundField DataField="FechaVencimiento" HeaderText="Vence" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="FechaCarga" HeaderText="Cargado" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><div class="acciones-fila">
                <asp:HyperLink runat="server" Text="Ver archivo" NavigateUrl='<%# "~/Archivo.ashx?tipo=documento&id=" + Eval("IdDocumento") %>' Target="_blank" />
                <asp:LinkButton runat="server" Text="Revisar" CommandName="Revisar" CommandArgument='<%# Eval("IdDocumento") %>' CausesValidation="false" Visible='<%# Eval("Estado").ToString() == "Pendiente" %>' />
            </div></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<asp:Panel ID="pnlRevision" runat="server" Visible="false" CssClass="admin-section">
    <h2>Revisar: <asp:Literal ID="litRevisionTitulo" runat="server" /></h2>
    <p><asp:Literal ID="litRevisionDetalle" runat="server" /></p>
    <iframe id="frmDocumento" runat="server" class="documento-vista" title="Documento a revisar"></iframe>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtMotivo" Text="Motivo del rechazo (obligatorio para rechazar)" /><asp:TextBox ID="txtMotivo" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" MaxLength="250" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMotivo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Rechazo" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtMotivo" ValidationExpression="^[\s\S]{5,250}$" ErrorMessage="Entre 5 y 250 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Rechazo" /></div>
    <div class="botonera">
        <asp:Button ID="btnAprobar" runat="server" CssClass="btn btn-brand" Text="Aprobar" OnClick="btnAprobar_Click" />
        <asp:Button ID="btnRechazar" runat="server" CssClass="btn btn-outline-danger" Text="Rechazar" OnClick="btnRechazar_Click" CausesValidation="true" ValidationGroup="Rechazo" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>
