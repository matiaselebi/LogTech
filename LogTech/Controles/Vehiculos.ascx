<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Vehiculos.ascx.cs" Inherits="LogTech.Controles.Vehiculos" %>
<%@ Register Src="~/Controles/DetalleVehiculoCard.ascx" TagPrefix="uc" TagName="DetalleVehiculoCard" %>
<%-- Transportista y flota: CU-002-007 a CU-002-010. El transportista tiene un solo camión y lo ve en Mi perfil.
     Cada vehículo se carga con su documentación (seguro, VTV y cédula verde) y su semáforo dice si puede hacer viajes. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light"><asp:Literal ID="litEtiqueta" runat="server" Text="MI FLOTA" /></span><h1><asp:Literal ID="litTitulo" runat="server" Text="Mis vehículos" /></h1><p><asp:Literal ID="litBajada" runat="server" Text="Cargá tus camiones con su documentación para poder tomar viajes." /></p></div>
    <asp:Button ID="btnNuevo" runat="server" CssClass="btn btn-brand" Text="Agregar vehículo" CausesValidation="false" OnClick="btnNuevo_Click" />
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="admin-section">
    <h2><asp:Literal ID="litTituloFormulario" runat="server" /></h2>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPatente" Text="Patente" /><asp:TextBox ID="txtPatente" runat="server" CssClass="form-control" MaxLength="7" placeholder="AB123CD o ABC123" data-tipo="patente" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPatente" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtPatente" ValidationExpression="^([A-Z]{2}\d{3}[A-Z]{2}|[A-Z]{3}\d{3})$" ErrorMessage="La patente tiene que ser AB123CD o ABC123." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtMarca" Text="Marca" /><asp:TextBox ID="txtMarca" runat="server" CssClass="form-control" MaxLength="40" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMarca" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtMarca" ValidationExpression="^[\s\S]{2,40}$" ErrorMessage="Entre 2 y 40 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtModelo" Text="Modelo" /><asp:TextBox ID="txtModelo" runat="server" CssClass="form-control" MaxLength="40" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtModelo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtCapacidad" Text="Capacidad de carga (toneladas)" /><asp:TextBox ID="txtCapacidad" runat="server" CssClass="form-control" TextMode="Number" step="0.01" min="0.01" max="60" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtCapacidad" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /><asp:CustomValidator runat="server" ControlToValidate="txtCapacidad" ClientValidationFunction="validarRango" ErrorMessage="Entre 0,01 y 60 toneladas." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlAcoplado" Text="Tipo de acoplado" /><asp:DropDownList ID="ddlAcoplado" runat="server" CssClass="form-select" /><asp:RequiredFieldValidator runat="server" ControlToValidate="ddlAcoplado" InitialValue="" ErrorMessage="Elegí el tipo de acoplado." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Vehiculo" /></div>
    </div>
    <%-- Solo en el alta: la documentación legal del vehículo. Queda en revisión hasta que LogTech la apruebe. --%>
    <asp:Panel ID="pnlDocumentos" runat="server">
        <h3 class="mt-4">Documentación del vehículo</h3>
        <p>PDF o JPG de hasta 5 MB. Hasta que LogTech la revise, el vehículo queda en amarillo y no puede hacer viajes.</p>
        <div class="form-grid">
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuSeguro" Text="Seguro del vehículo" /><asp:FileUpload ID="fuSeguro" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuSeguro" ErrorMessage="Adjuntá el seguro." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceSeguro" Text="Vencimiento del seguro" /><asp:TextBox ID="txtVenceSeguro" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceSeguro" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceSeguro" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuVtv" Text="VTV" /><asp:FileUpload ID="fuVtv" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuVtv" ErrorMessage="Adjuntá la VTV." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceVtv" Text="Vencimiento de la VTV" /><asp:TextBox ID="txtVenceVtv" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceVtv" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceVtv" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="fuCedula" Text="Cédula verde" /><asp:FileUpload ID="fuCedula" runat="server" CssClass="form-control" accept=".pdf,.jpg,.jpeg" /><asp:RequiredFieldValidator runat="server" ControlToValidate="fuCedula" ErrorMessage="Adjuntá la cédula verde." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVenceCedula" Text="Vencimiento de la cédula verde" /><asp:TextBox ID="txtVenceCedula" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVenceCedula" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /><asp:CustomValidator runat="server" ControlToValidate="txtVenceCedula" ClientValidationFunction="validarRango" ErrorMessage="Tiene que ser posterior a hoy." Display="Dynamic" CssClass="validador" ValidationGroup="Vehiculo" /></div>
        </div>
    </asp:Panel>
    <div class="botonera">
        <asp:Button ID="btnGuardar" runat="server" CssClass="btn btn-brand" Text="Guardar vehículo" OnClick="btnGuardar_Click" CausesValidation="true" ValidationGroup="Vehiculo" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>

<%-- «Ver detalles»: lo que hizo el vehículo, su documentación y (la flota) su conductor designado. --%>
<asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="admin-section">
    <div class="section-title"><div><h2>Detalles del <asp:Literal ID="litDetallePatente" runat="server" /></h2><p><asp:Literal ID="litDetalleVehiculo" runat="server" /></p></div></div>
    <div class="tablero-kpis kpis-flota">
        <article class="kpi-card"><small>Viajes entregados</small><strong><asp:Literal ID="litDetalleViajes" runat="server" /></strong><span>En los últimos 5 años</span></article>
        <article class="kpi-card"><small>Cobrado</small><strong><asp:Literal ID="litDetalleNeto" runat="server" /></strong><span>Lo que pagaron los dadores menos la comisión y el remito</span></article>
        <article class="kpi-card"><small>Kilómetros recorridos</small><strong><asp:Literal ID="litDetalleKilometros" runat="server" /></strong><span><asp:Literal ID="litDetalleToneladas" runat="server" /> toneladas transportadas</span></article>
        <article class="kpi-card"><small>Incidencias</small><strong><asp:Literal ID="litDetalleIncidencias" runat="server" /></strong><span>Reportadas en sus viajes</span></article>
        <article class="kpi-card"><small>Último viaje</small><strong><asp:Literal ID="litDetalleUltimo" runat="server" /></strong><span>Fecha de carga</span></article>
        <article class="kpi-card"><small>Ahora</small><strong><asp:Literal ID="litDetalleAhora" runat="server" /></strong><span><asp:Literal ID="litDetalleViajeActual" runat="server" /></span></article>
    </div>
    <asp:Panel ID="pnlDetalleConductores" runat="server">
        <h3 class="mt-4">Conductores que lo manejaron</h3>
        <p><asp:Literal ID="litDetalleConductores" runat="server" /></p>
    </asp:Panel>
    <h3 class="mt-4">Documentación</h3>
    <div class="table-responsive"><asp:GridView ID="gvDetalleDocumentos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No tiene documentos cargados.">
        <Columns>
            <asp:BoundField DataField="Tipo" HeaderText="Documento" />
            <asp:BoundField DataField="FechaVencimiento" HeaderText="Vence" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='doc-estado doc-<%# Eval("Estado").ToString().ToLowerInvariant() %>'><%#: Eval("Estado") %></span><%# Eval("Motivo") == null ? "" : "<small class='doc-motivo'>" + HttpUtility.HtmlEncode(Eval("Motivo")) + "</small>" %></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
    <asp:Panel ID="pnlConductorDesignado" runat="server" CssClass="mt-4">
        <h3>Conductor designado</h3>
        <p>Si el camión tiene un conductor fijo (por ejemplo, su dueño trabaja en tu flota), el camión solo sale con él.</p>
        <div class="filtros-modulo">
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlConductorDesignado" Text="Conductor" /><asp:DropDownList ID="ddlConductorDesignado" runat="server" CssClass="form-select" /></div>
            <asp:Button ID="btnGuardarDesignado" runat="server" CssClass="btn btn-brand" Text="Guardar conductor" CausesValidation="false" OnClick="btnGuardarDesignado_Click" />
        </div>
    </asp:Panel>
    <div class="botonera"><asp:Button ID="btnCerrarDetalle" runat="server" CssClass="btn btn-outline-dark" Text="Cerrar" CausesValidation="false" OnClick="btnCerrarDetalle_Click" /></div>
</asp:Panel>

<section class="admin-section">
    <%-- Filtran solos: el texto al salir del campo o al apretar Enter y el estado al elegirlo. --%>
    <asp:Panel ID="pnlFiltros" runat="server" CssClass="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtBuscar" Text="Buscar (patente, marca o modelo)" /><asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control" MaxLength="40" AutoPostBack="true" OnTextChanged="Filtrar" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstado" Text="Estado" />
            <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="Filtrar"><asp:ListItem Value="Activo" Text="Solo activos" /><asp:ListItem Value="Inactivo" Text="Dados de baja" /><asp:ListItem Value="" Text="Todos" /></asp:DropDownList>
        </div>
    </asp:Panel>
    <asp:Label ID="lblVacio" runat="server" CssClass="panel-message" Visible="false" Text="No hay vehículos que coincidan con la búsqueda." />
    <div class="vehiculos-grid">
        <asp:Repeater ID="rptVehiculos" runat="server" OnItemCommand="rptVehiculos_ItemCommand">
            <ItemTemplate>
                <div class="vehiculo-item">
                    <uc:DetalleVehiculoCard runat="server" Vehiculo='<%# (LogTech.BE.VehiculoBE)Container.DataItem %>' />
                    <div class="acciones-fila">
                        <asp:LinkButton runat="server" Text="Modificar" CommandName="Modificar" CommandArgument='<%# Eval("IdVehiculo") %>' CausesValidation="false" Visible='<%# Eval("Estado").ToString() == "Activo" %>' />
                        <asp:LinkButton runat="server" Text="Dar de baja" CommandName="Baja" CommandArgument='<%# Eval("IdVehiculo") %>' CausesValidation="false" Visible='<%# Eval("Estado").ToString() == "Activo" %>' OnClientClick='<%# ConfirmarBaja %>' />
                        <asp:LinkButton runat="server" Text="Ver detalles" CommandName="Detalles" CommandArgument='<%# Eval("IdVehiculo") %>' CausesValidation="false" />
                        <asp:HyperLink runat="server" Text="Documentación" NavigateUrl='<%# "~/Panel.aspx?seccion=Documentacion&vehiculo=" + Eval("IdVehiculo") %>' Visible='<%# !UnSoloCamion && Eval("Estado").ToString() == "Activo" %>' />
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</section>
