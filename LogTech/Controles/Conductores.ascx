<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Conductores.ascx.cs" Inherits="LogTech.Controles.Conductores" %>
<%-- Administrador de flota: CU-002-011 a CU-002-014 (transportistas internos). Cada conductor se carga con su
     documentación legal (DNI, licencia profesional y antecedentes penales) y su semáforo dice si puede hacer viajes. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">MI FLOTA</span><h1>Conductores</h1><p>Choferes de tu flota, con su documentación, para asignarles viajes.</p></div>
    <asp:Button ID="btnNuevo" runat="server" CssClass="btn btn-brand" Text="Agregar conductor" CausesValidation="false" OnClick="btnNuevo_Click" />
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="admin-section">
    <h2><asp:Literal ID="litTituloFormulario" runat="server" /></h2>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtNombre" Text="Nombre" /><asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtNombre" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtApellido" Text="Apellido" /><asp:TextBox ID="txtApellido" runat="server" CssClass="form-control" MaxLength="60" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtApellido" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtDni" Text="DNI" /><asp:TextBox ID="txtDni" runat="server" CssClass="form-control" MaxLength="8" inputmode="numeric" placeholder="Solo números" data-tipo="digitos" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtDni" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtDni" ValidationExpression="^\d{7,8}$" ErrorMessage="El DNI tiene que tener 7 u 8 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtLicencia" Text="Número de licencia" /><asp:TextBox ID="txtLicencia" runat="server" CssClass="form-control" MaxLength="40" data-tipo="licencia" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtLicencia" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtLicencia" ValidationExpression="^[A-Za-z0-9\-]{4,40}$" ErrorMessage="De 4 a 40 letras, números o guiones." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtTelefono" Text="Teléfono" /><asp:TextBox ID="txtTelefono" runat="server" CssClass="form-control" MaxLength="10" TextMode="Phone" data-tipo="digitos" inputmode="numeric" placeholder="Solo números" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtTelefono" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtTelefono" ValidationExpression="^\d{10}$" ErrorMessage="El teléfono tiene que tener 10 números (código de área sin 0 y número sin 15)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Conductor" /></div>
    </div>
    <%-- Solo en el alta: la documentación legal del conductor. Queda en revisión hasta que LogTech la apruebe. --%>
    <asp:Panel ID="pnlDocumentos" runat="server">
        <h3 class="mt-4">Documentación del conductor</h3>
        <p>PDF o JPG de hasta 5 MB. Hasta que LogTech la revise, el conductor queda en amarillo y no se le pueden asignar viajes.</p>
        <div class="form-grid">
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuDni" Text="DNI (frente y dorso)" /><asp:FileUpload ID="fuDni" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuDni" ErrorMessage="Adjuntá el DNI." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceDni" Text="Vencimiento del DNI" /><asp:TextBox ID="txtVenceDni" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceDni" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceDni" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuLicencia" Text="Licencia profesional" /><asp:FileUpload ID="fuLicencia" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuLicencia" ErrorMessage="Adjuntá la licencia." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceLicencia" Text="Vencimiento de la licencia" /><asp:TextBox ID="txtVenceLicencia" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceLicencia" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceLicencia" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuAntecedentes" Text="Certificado de antecedentes penales" /><asp:FileUpload ID="fuAntecedentes" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuAntecedentes" ErrorMessage="Adjuntá el certificado." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceAntecedentes" Text="Vencimiento del certificado" /><asp:TextBox ID="txtVenceAntecedentes" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceAntecedentes" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceAntecedentes" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Conductor" /></div>
        </div>
    </asp:Panel>
    <div class="botonera">
        <asp:Button ID="btnGuardar" runat="server" CssClass="btn btn-brand" Text="Guardar conductor" OnClick="btnGuardar_Click" CausesValidation="true" ValidationGroup="Conductor" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>

<section class="admin-section">
    <%-- Filtran solos: el texto al salir del campo o al apretar Enter y el estado al elegirlo. --%>
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBuscar" Text="Buscar (DNI, nombre o apellido)" /><asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control" MaxLength="60" AutoPostBack="true" OnTextChanged="Filtrar" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstado" Text="Estado" />
            <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="Filtrar"><asp:ListItem Value="Activo" Text="Solo activos" /><asp:ListItem Value="Inactivo" Text="Dados de baja" /><asp:ListItem Value="" Text="Todos" /></asp:DropDownList>
        </div>
    </div>
    <div class="table-responsive"><asp:GridView ID="gvConductores" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay conductores que coincidan con la búsqueda." OnRowCommand="gvConductores_RowCommand">
        <Columns>
            <asp:BoundField DataField="NombreCompleto" HeaderText="Apellido y nombre" />
            <asp:BoundField DataField="Dni" HeaderText="DNI" />
            <asp:BoundField DataField="NumeroLicencia" HeaderText="Licencia" />
            <asp:BoundField DataField="Telefono" HeaderText="Teléfono" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:TemplateField HeaderText="Documentación"><ItemTemplate><span class='vehiculo-semaforo-fila semaforo-<%# Eval("Semaforo").ToString().ToLowerInvariant() %>'><span class="semaforo-luz" aria-hidden="true"></span><%# TextoSemaforo(Eval("Semaforo")) %></span><%# Eval("MotivoSemaforo") == null ? "" : "<small class='doc-motivo'>" + HttpUtility.HtmlEncode(Eval("MotivoSemaforo")) + "</small>" %></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Situación"><ItemTemplate><%# (bool)Eval("EnViaje") ? "En viaje" : "Disponible" %></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><div class="acciones-fila">
                <asp:LinkButton runat="server" Text="Modificar" CommandName="Modificar" CommandArgument='<%# Eval("IdConductor") %>' CausesValidation="false" Visible='<%# Eval("Estado").ToString() == "Activo" %>' />
                <asp:LinkButton runat="server" Text="Dar de baja" CommandName="Baja" CommandArgument='<%# Eval("IdConductor") %>' CausesValidation="false" Visible='<%# Eval("Estado").ToString() == "Activo" %>' OnClientClick="return confirm('¿Dar de baja este conductor? Ya no va a poder recibir viajes.');" />
                <asp:HyperLink runat="server" Text="Documentación" NavigateUrl='<%# "~/Panel.aspx?seccion=Documentacion&conductor=" + Eval("IdConductor") %>' Visible='<%# Eval("Estado").ToString() == "Activo" %>' />
            </div></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>
