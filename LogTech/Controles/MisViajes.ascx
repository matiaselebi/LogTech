<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MisViajes.ascx.cs" Inherits="LogTech.Controles.MisViajes" %>
<%@ Register Src="~/Controles/EstadoViajeBadge.ascx" TagPrefix="uc" TagName="EstadoViajeBadge" %>
<%@ Register Src="~/Controles/DetalleViaje.ascx" TagPrefix="uc" TagName="DetalleViaje" %>
<%-- Transportista y flota: CU-007-035 (estado), CU-004-024 (incidencia), CU-004-022 (remito),
     CU-002-015 (asignación interna) y CU-007-036 (monitoreo). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">OPERACIÓN LOGÍSTICA</span><h1>Mis viajes</h1><p>Actualizá el estado, reportá incidencias y emití el remito al entregar.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<section class="admin-section">
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlVista" Text="Mostrar" />
            <asp:DropDownList ID="ddlVista" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlVista_SelectedIndexChanged">
                <asp:ListItem Value="curso" Text="Viajes en curso" /><asp:ListItem Value="entregados" Text="Viajes entregados" />
            </asp:DropDownList>
        </div>
    </div>
    <div class="table-responsive"><asp:GridView ID="gvViajes" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No tenés viajes en esta lista." OnRowCommand="gvViajes_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="N°" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="FechaCarga" HeaderText="Carga" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="Mercaderia" HeaderText="Mercadería" />
            <asp:BoundField DataField="PesoToneladas" HeaderText="Peso (t)" DataFormatString="{0:0.##}" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><uc:EstadoViajeBadge runat="server" Estado='<%# Eval("Estado") %>' /></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Cobrás"><ItemTemplate><%#: ((decimal)Eval("NetoTransportista")).ToString("C0") %><br /><small><%#: Eval("MedioPago") %> · <%#: Eval("EstadoPago") %></small></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" NullDisplayText="Sin asignar" />
            <asp:BoundField DataField="Conductor" HeaderText="Conductor" NullDisplayText="Sin asignar" />
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><div class="acciones-fila">
                <asp:LinkButton runat="server" Text="Asignar conductor y vehículo" CommandName="Asignar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# EsFlota && Es(Eval("Estado"), "Asignado") && Tiene("Asignar viajes internos") %>' />
                <asp:LinkButton runat="server" Text="Actualizar estado" CommandName="Estado" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# EnCurso(Eval("Estado")) && !Es(Eval("Estado"), "En destino") && Tiene("Actualizar estado de viaje") %>' />
                <asp:LinkButton runat="server" Text="Reportar incidencia" CommandName="Incidencia" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# EnCurso(Eval("Estado")) && !Es(Eval("Estado"), "Asignado") && Tiene("Reportar incidencias de viaje") %>' />
                <asp:LinkButton runat="server" Text="Emitir remito" CommandName="Remito" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Es(Eval("Estado"), "En destino") && Tiene("Gestionar remitos digitales") %>' />
                <asp:LinkButton runat="server" Text="Ver remito" CommandName="VerRemito" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# (bool)Eval("TieneRemito") && Tiene("Gestionar remitos digitales") %>' />
                <asp:LinkButton runat="server" Text="Monitorear" CommandName="Monitorear" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# EsFlota && Tiene("Consultar monitoreo de viaje") %>' />
            </div></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<asp:Panel ID="pnlEstado" runat="server" Visible="false" CssClass="admin-section">
    <h2>Actualizar estado del viaje N° <asp:Literal ID="litEstadoViaje" runat="server" /></h2>
    <p>Estado actual: <strong><asp:Literal ID="litEstadoActual" runat="server" /></strong>. Los estados van en orden: en camino a carga, en origen, en tránsito y en destino.</p>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstadoNuevo" Text="Nuevo estado" /><asp:DropDownList ID="ddlEstadoNuevo" runat="server" CssClass="form-select" /></div>
    <%-- El navegador completa la ubicación actual del camión (R18). --%>
    <asp:HiddenField ID="hfLatitud" runat="server" /><asp:HiddenField ID="hfLongitud" runat="server" />
    <p><small>Si el navegador lo permite, se guarda tu ubicación actual para el monitoreo del dador.</small></p>
    <div class="botonera">
        <asp:Button ID="btnGuardarEstado" runat="server" CssClass="btn btn-brand" Text="Guardar estado" OnClick="btnGuardarEstado_Click" />
        <asp:Button ID="btnCancelarEstado" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarPanel_Click" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlIncidencia" runat="server" Visible="false" CssClass="admin-section">
    <h2>Reportar incidencia del viaje N° <asp:Literal ID="litIncidenciaViaje" runat="server" /></h2>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlTipoIncidencia" Text="Tipo de problema" /><asp:DropDownList ID="ddlTipoIncidencia" runat="server" CssClass="form-select" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuFotoIncidencia" Text="Foto como evidencia (opcional)" /><asp:FileUpload ID="fuFotoIncidencia" runat="server" CssClass="form-control" accept=".jpg,.jpeg,.pdf" /><small>JPG o PDF, hasta 5 MB.</small></div>
        <div class="form-field form-grid-full"><asp:Label runat="server" AssociatedControlID="txtDescripcion" Text="Descripción" /><asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="500" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtDescripcion" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Incidencia" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtDescripcion" ValidationExpression="^[\s\S]{10,500}$" ErrorMessage="Entre 10 y 500 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Incidencia" /></div>
    </div>
    <div class="botonera">
        <asp:Button ID="btnReportar" runat="server" CssClass="btn btn-brand" Text="Enviar reporte" OnClick="btnReportar_Click" CausesValidation="true" ValidationGroup="Incidencia" />
        <asp:Button ID="btnCancelarIncidencia" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarPanel_Click" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlRemito" runat="server" Visible="false" CssClass="admin-section">
    <h2>Remito digital del viaje N° <asp:Literal ID="litRemitoViaje" runat="server" /></h2>
    <p>Al emitir el remito el viaje queda entregado. El remito no se puede modificar después.</p>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtReceptor" Text="Nombre y apellido de quien recibe" /><asp:TextBox ID="txtReceptor" runat="server" CssClass="form-control" MaxLength="100" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtReceptor" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Remito" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtReceptor" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Remito" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtDniReceptor" Text="DNI de quien recibe" /><asp:TextBox ID="txtDniReceptor" runat="server" CssClass="form-control" MaxLength="8" inputmode="numeric" data-tipo="digitos" placeholder="Solo números" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtDniReceptor" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Remito" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtDniReceptor" ValidationExpression="^\d{7,8}$" ErrorMessage="El DNI tiene que tener 7 u 8 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Remito" /></div>
        <div class="form-field form-grid-full"><asp:Label runat="server" AssociatedControlID="fuFotoRemito" Text="Foto del remito firmado" /><asp:FileUpload ID="fuFotoRemito" runat="server" CssClass="form-control" accept=".jpg,.jpeg,.pdf" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuFotoRemito" ErrorMessage="Adjuntá la foto del remito firmado." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Remito" /><small>JPG o PDF, hasta 5 MB.</small></div>
    </div>
    <div class="botonera">
        <asp:Button ID="btnEmitirRemito" runat="server" CssClass="btn btn-brand" Text="Emitir remito y entregar" OnClick="btnEmitirRemito_Click" OnClientClick="if (!Page_ClientValidate('Remito') || !confirm('Emitir el remito cierra el viaje como entregado. ¿Continuar?')) return false;" CausesValidation="true" ValidationGroup="Remito" />
        <asp:Button ID="btnCancelarRemito" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarPanel_Click" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlAsignar" runat="server" Visible="false" CssClass="admin-section">
    <h2>Asignar el viaje N° <asp:Literal ID="litAsignarViaje" runat="server" /></h2>
    <p><asp:Literal ID="litAsignarDetalle" runat="server" /></p>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlConductor" Text="Conductor" /><asp:DropDownList ID="ddlConductor" runat="server" CssClass="form-select" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlVehiculo" Text="Vehículo" /><asp:DropDownList ID="ddlVehiculo" runat="server" CssClass="form-select" /></div>
    </div>
    <%-- Por qué no aparecen algunos conductores o vehículos (en otro viaje, documentación en revisión, no soportan el peso). --%>
    <p><small><asp:Literal ID="litAvisoAsignar" runat="server" /></small></p>
    <div class="botonera">
        <asp:Button ID="btnAsignar" runat="server" CssClass="btn btn-brand" Text="Confirmar asignación" OnClick="btnAsignar_Click" />
        <asp:Button ID="btnCancelarAsignar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarPanel_Click" />
    </div>
</asp:Panel>

<uc:DetalleViaje ID="ucDetalle" runat="server" />
