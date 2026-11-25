<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MisCargas.ascx.cs" Inherits="LogTech.Controles.MisCargas" %>
<%@ Register Src="~/Controles/EstadoViajeBadge.ascx" TagPrefix="uc" TagName="EstadoViajeBadge" %>
<%@ Register Src="~/Controles/DetalleVehiculoCard.ascx" TagPrefix="uc" TagName="DetalleVehiculoCard" %>
<%@ Register Src="~/Controles/CalificacionEstrellas.ascx" TagPrefix="uc" TagName="CalificacionEstrellas" %>
<%@ Register Src="~/Controles/DetalleViaje.ascx" TagPrefix="uc" TagName="DetalleViaje" %>
<%-- Dador de carga: CU-001-001 a 004, CU-001-006 (camiones compatibles) y CU-007-037 (calificar). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">OPERACIÓN LOGÍSTICA</span><h1>Mis cargas</h1><p>Publicá tus cargas, seguí cada viaje y calificá el servicio.</p></div>
    <asp:Button ID="btnNueva" runat="server" CssClass="btn btn-brand" Text="Publicar nueva oferta" CausesValidation="false" OnClick="btnNueva_Click" />
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="admin-section">
    <h2><asp:Literal ID="litTituloFormulario" runat="server" /></h2>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlOrigen" Text="Origen" /><asp:DropDownList ID="ddlOrigen" runat="server" CssClass="form-select" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlDestino" Text="Destino" /><asp:DropDownList ID="ddlDestino" runat="server" CssClass="form-select" /></div>
        <div class="form-field form-grid-full"><asp:Label runat="server" AssociatedControlID="txtMercaderia" Text="Mercadería" /><asp:TextBox ID="txtMercaderia" runat="server" CssClass="form-control" MaxLength="120" placeholder="Ej.: Bobinas de acero" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMercaderia" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtMercaderia" ValidationExpression="^[\s\S]{3,120}$" ErrorMessage="Entre 3 y 120 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtPeso" Text="Peso (toneladas)" /><asp:TextBox ID="txtPeso" runat="server" CssClass="form-control" TextMode="Number" step="0.01" min="0.01" max="60" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtPeso" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><asp:CustomValidator runat="server" ControlToValidate="txtPeso" ClientValidationFunction="validarRango" ErrorMessage="Entre 0,01 y 60 toneladas." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtFechaCarga" Text="Fecha de carga" /><asp:TextBox ID="txtFechaCarga" runat="server" CssClass="form-control" TextMode="Date" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtFechaCarga" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><asp:CustomValidator runat="server" ControlToValidate="txtFechaCarga" ClientValidationFunction="validarRango" ErrorMessage="Elegí una fecha entre hoy y los próximos 6 meses." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtTarifa" Text="Precio final ($)" /><asp:TextBox ID="txtTarifa" runat="server" CssClass="form-control" TextMode="Number" step="0.01" min="10000" max="100000000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtTarifa" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><asp:CustomValidator runat="server" ControlToValidate="txtTarifa" ClientValidationFunction="validarRango" ErrorMessage="El precio tiene que ser de al menos $ 10.000." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><small>Es lo que pagás, con todo incluido: la comisión de LogTech (7 %) y el remito digital ($ 1.500). En tus 3 primeros viajes no pagás la comisión.</small></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="rblMedioPago" Text="Cómo vas a pagar" />
            <asp:RadioButtonList ID="rblMedioPago" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="medio-pago-opciones" AutoPostBack="true" OnSelectedIndexChanged="rblMedioPago_SelectedIndexChanged" CausesValidation="false">
                <asp:ListItem Value="Efectivo" Text="Efectivo (Pago Fácil o Rapipago)" Selected="True" /><asp:ListItem Value="Tarjeta" Text="Tarjeta" /><asp:ListItem Value="Saldo" Text="Saldo de la billetera" /><asp:ListItem Value="MercadoPago" Text="Mercado Pago" />
            </asp:RadioButtonList>
            <asp:Panel ID="pnlTarjeta" runat="server" Visible="false"><asp:DropDownList ID="ddlTarjeta" runat="server" CssClass="form-select" /><small>Se te cobra cuando un transportista acepta el viaje y queda retenido hasta el remito.</small></asp:Panel>
            <asp:Panel ID="pnlSaldo" runat="server" Visible="false"><small><asp:Literal ID="litSaldo" runat="server" /></small></asp:Panel>
        </div>
        <div class="form-field form-grid-full"><asp:Button ID="btnCotizar" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Calcular tarifa sugerida" CausesValidation="false" OnClick="btnCotizar_Click" /> <asp:Label ID="lblCotizacion" runat="server" CssClass="panel-message" role="status" /></div>
    </div>
    <div class="botonera">
        <asp:Button ID="btnGuardar" runat="server" CssClass="btn btn-brand" Text="Guardar y publicar" OnClick="btnGuardar_Click" CausesValidation="true" ValidationGroup="Carga" />
        <asp:Button ID="btnCancelar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
    </div>
</asp:Panel>

<section class="admin-section">
    <div class="filtros-modulo">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlEstado" Text="Estado" /><asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select" /></div>
        <asp:Button ID="btnBuscar" runat="server" CssClass="btn btn-brand" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
    </div>
    <div class="table-responsive"><asp:GridView ID="gvOfertas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No hay ofertas que coincidan con la búsqueda." OnRowCommand="gvOfertas_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="N°" />
            <asp:BoundField DataField="FechaCarga" HeaderText="Carga" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Mercaderia" HeaderText="Mercadería" />
            <asp:BoundField DataField="PesoToneladas" HeaderText="Peso (t)" DataFormatString="{0:0.##}" />
            <asp:BoundField DataField="PrecioDador" HeaderText="Precio" DataFormatString="{0:C0}" />
            <asp:TemplateField HeaderText="Pago"><ItemTemplate><%#: Eval("MedioPago") %><br /><small><%#: Eval("EstadoPago") %></small></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><uc:EstadoViajeBadge runat="server" Estado='<%# Eval("Estado") %>' /></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Transportista" HeaderText="Transportista" NullDisplayText="-" />
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><div class="acciones-fila">
                <asp:LinkButton runat="server" Text="Modificar" CommandName="Modificar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Es(Eval("Estado"), "Pendiente") && Tiene("Gestionar ofertas de carga") %>' />
                <asp:LinkButton runat="server" Text="Dar de baja" CommandName="Baja" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Es(Eval("Estado"), "Pendiente") && Tiene("Gestionar ofertas de carga") %>' OnClientClick="return confirm('¿Dar de baja esta oferta? Los transportistas ya no la van a ver.');" />
                <asp:LinkButton runat="server" Text="Camiones compatibles" CommandName="Match" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Es(Eval("Estado"), "Pendiente") && Tiene("Consultar match de cargas") %>' />
                <asp:LinkButton runat="server" Text="Monitorear" CommandName="Monitorear" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# !Es(Eval("Estado"), "Pendiente") && !Es(Eval("Estado"), "Cancelado") && !Es(Eval("Estado"), "Entregado") && Tiene("Consultar monitoreo de viaje") %>' />
                <asp:LinkButton runat="server" Text="Ver remito" CommandName="Remito" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# (bool)Eval("TieneRemito") && Tiene("Gestionar remitos digitales") %>' />
                <asp:LinkButton runat="server" Text="Calificar" CommandName="Calificar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" Visible='<%# Es(Eval("Estado"), "Entregado") && !(bool)Eval("Calificado") && Tiene("Calificar servicio de transporte") %>' />
            </div></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</section>

<asp:Panel ID="pnlMatch" runat="server" Visible="false" CssClass="admin-section">
    <div class="section-title">
        <div><h2>Camiones compatibles con la oferta N° <asp:Literal ID="litMatchViaje" runat="server" /></h2></div>
        <asp:LinkButton ID="btnCerrarMatch" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Cerrar" CausesValidation="false" OnClick="btnCerrarMatch_Click" />
    </div>
    <p>Camiones libres que soportan el peso de la carga: primero los más cercanos al origen y después los mejor calificados.</p>
    <div class="vehiculos-grid">
        <asp:Repeater ID="rptMatch" runat="server">
            <ItemTemplate>
                <div class="vehiculo-item">
                    <uc:DetalleVehiculoCard runat="server" Vehiculo='<%# (LogTech.BE.VehiculoBE)Container.DataItem %>' />
                    <p>Dueño: <strong><%#: Eval("Propietario") %></strong></p>
                    <p><uc:CalificacionEstrellas runat="server" Puntaje='<%# Convert.ToDecimal(Eval("Reputacion")) %>' Cantidad='<%# Eval("CantidadCalificaciones") %>' /></p>
                    <p><%#: Eval("DistanciaKm") == null ? "Sin ubicación reportada" : string.Format("A {0:N0} km del origen", Eval("DistanciaKm")) %></p>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Panel>

<asp:Panel ID="pnlCalificar" runat="server" Visible="false" CssClass="admin-section">
    <h2>Calificar el viaje N° <asp:Literal ID="litCalificarViaje" runat="server" /></h2>
    <p><asp:Literal ID="litCalificarDetalle" runat="server" /></p>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="rblPuntaje" Text="Puntaje" />
        <asp:RadioButtonList ID="rblPuntaje" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="puntaje-opciones">
            <asp:ListItem Value="1">★<span class="visually-hidden">1</span></asp:ListItem><asp:ListItem Value="2">★<span class="visually-hidden">2</span></asp:ListItem><asp:ListItem Value="3">★<span class="visually-hidden">3</span></asp:ListItem><asp:ListItem Value="4">★<span class="visually-hidden">4</span></asp:ListItem><asp:ListItem Value="5">★<span class="visually-hidden">5</span></asp:ListItem>
        </asp:RadioButtonList><asp:RequiredFieldValidator runat="server" ControlToValidate="rblPuntaje" ErrorMessage="Elegí de 1 a 5 estrellas." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Calificacion" />
    </div>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtResena" Text="Reseña (opcional)" /><asp:TextBox ID="txtResena" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="500" /></div>
    <div class="botonera">
        <asp:Button ID="btnCalificar" runat="server" CssClass="btn btn-brand" Text="Guardar calificación" OnClick="btnCalificar_Click" CausesValidation="true" ValidationGroup="Calificacion" />
        <asp:Button ID="btnCancelarCalificacion" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarCalificacion_Click" />
    </div>
</asp:Panel>

<uc:DetalleViaje ID="ucDetalle" runat="server" />
