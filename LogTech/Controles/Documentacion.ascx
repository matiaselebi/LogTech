<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Documentacion.ascx.cs" Inherits="LogTech.Controles.Documentacion" %>
<%@ Register Src="~/Controles/SemaforoDocumentacion.ascx" TagPrefix="uc" TagName="SemaforoDocumentacion" %>
<%-- Transportista, flota y dador: CU-003-016 a CU-003-019 (su propio legajo, el de sus vehículos y el de sus conductores). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">LEGAJO</span><h1>Mi documentación</h1><p>Subí tus documentos legales (PDF o JPG) para que LogTech los valide.</p></div>
    <asp:Button ID="btnNuevo" runat="server" CssClass="btn btn-brand" Text="Subir documento" CausesValidation="false" OnClick="btnNuevo_Click" />
</div>
<uc:SemaforoDocumentacion ID="ucSemaforo" runat="server" />
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="admin-section">
    <h2><asp:Literal ID="litTituloFormulario" runat="server" /></h2>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlTipo" Text="Tipo de documento" /><asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-select" /></div>
        <asp:Panel ID="pnlVehiculo" runat="server" CssClass="form-field"><asp:Label runat="server" AssociatedControlID="ddlVehiculo" Text="Vehículo (seguro, VTV o cédula verde)" /><asp:DropDownList ID="ddlVehiculo" runat="server" CssClass="form-select" /></asp:Panel>
        <asp:Panel ID="pnlConductor" runat="server" CssClass="form-field"><asp:Label runat="server" AssociatedControlID="ddlConductor" Text="Conductor (DNI, licencia profesional o antecedentes)" /><asp:DropDownList ID="ddlConductor" runat="server" CssClass="form-select" /></asp:Panel>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVencimiento" Text="Fecha de vencimiento" /><asp:TextBox ID="txtVencimiento" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVencimiento" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Documento" /><asp:CustomValidator runat="server" ControlToValidate="txtVencimiento" ClientValidationFunction="validarRango" ErrorMessage="La fecha de vencimiento tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Documento" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuArchivo" Text="Archivo" /><asp:FileUpload ID="fuArchivo" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><small><asp:Literal ID="litAyudaArchivo" runat="server" /></small></div>
    </div>
    <div class="botonera">
        <asp:Button ID="btnGuardar" runat="server" CssClass="btn btn-brand" Text="Guardar documento" OnClick="btnGuardar_Click" CausesValidation="true" ValidationGroup="Documento" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>

<section class="admin-section">
    <%-- Desde la ficha de un vehículo o la fila de un conductor se ven solo sus documentos. --%>
    <asp:Panel ID="pnlFiltro" runat="server" Visible="false" CssClass="panel-message">
        <asp:Literal ID="litFiltro" runat="server" /> <asp:HyperLink runat="server" NavigateUrl="~/Panel.aspx?seccion=Documentacion" Text="Ver todos" />
    </asp:Panel>
    <div class="table-responsive"><asp:GridView ID="gvDocumentos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no cargaste documentos." OnRowCommand="gvDocumentos_RowCommand">
        <Columns>
            <asp:BoundField DataField="Tipo" HeaderText="Documento" />
            <asp:BoundField DataField="DeQuien" HeaderText="Vehículo o conductor" />
            <asp:BoundField DataField="FechaVencimiento" HeaderText="Vence" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='doc-estado doc-<%# Eval("Estado").ToString().ToLowerInvariant() %>'><%#: Eval("Estado") %></span><%# Eval("Motivo") == null ? "" : "<small class='doc-motivo'>" + HttpUtility.HtmlEncode(Eval("Motivo")) + "</small>" %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="FechaCarga" HeaderText="Cargado" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><div class="acciones-fila">
                <asp:HyperLink runat="server" Text="Ver archivo" NavigateUrl='<%# "~/Archivo.ashx?tipo=documento&id=" + Eval("IdDocumento") %>' Target="_blank" />
                <asp:LinkButton runat="server" Text="Modificar" CommandName="Modificar" CommandArgument='<%# Eval("IdDocumento") %>' CausesValidation="false" />
                <asp:LinkButton runat="server" Text="Dar de baja" CommandName="Baja" CommandArgument='<%# Eval("IdDocumento") %>' CausesValidation="false" Visible='<%# (string)Eval("Estado") != "Aprobado" %>' OnClientClick="return confirm('Dar de baja un documento obligatorio puede inhabilitarte para operar. ¿Continuar?');" />
            </div></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<%-- La lista de vehículos solo se puede usar con los documentos de un vehículo, y la de conductores con los de un
     conductor; con los de la cuenta (CUIT, CNRT, licencia) quedan en «No es de…». El servidor lo vuelve a controlar. --%>
<script>
    (function () {
        var tipo = document.getElementById('<%= ddlTipo.ClientID %>');
        if (!tipo) return;
        var listas = [
            { lista: document.getElementById('<%= ddlVehiculo.ClientID %>'), tipos: <%= TiposJs(negocio.DocumentosDelVehiculo()) %> },
            { lista: document.getElementById('<%= ddlConductor.ClientID %>'), tipos: <%= TiposJs(negocio.DocumentosDelConductor()) %> }
        ];
        // Al deshabilitar una lista se recuerda lo que estaba elegido (por ejemplo, el vehículo desde el que se llegó).
        function actualizar() {
            listas.forEach(function (x) {
                if (!x.lista) return;
                var usa = x.tipos.indexOf(tipo.value) >= 0;
                if (!usa && x.lista.selectedIndex > 0) x.elegido = x.lista.selectedIndex;
                if (usa && x.lista.selectedIndex === 0 && x.elegido) x.lista.selectedIndex = x.elegido;
                if (!usa) x.lista.selectedIndex = 0;
                x.lista.disabled = !usa;
            });
        }
        tipo.addEventListener('change', actualizar);
        actualizar();
    }());
</script>
