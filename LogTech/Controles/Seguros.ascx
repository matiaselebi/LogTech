<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Seguros.ascx.cs" Inherits="LogTech.Controles.Seguros" %>
<%-- CU-006-031 Alta Contratación de Seguro, CU-006-032 Modificar Póliza Activa (cambiar de plan), CU-006-033 Baja
     (cancelar al vencer) y CU-006-034 Consultar Pólizas Activas. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">PAGOS</span><h1>Seguros</h1><p>Asegurá tu carga por mes con una aseguradora. El seguro se renueva solo cada mes hasta que lo canceles.</p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlActivo" runat="server" CssClass="admin-section" Visible="false">
    <div class="section-title"><div><h2>Tu seguro activo</h2><p><asp:Literal ID="litActivoEstado" runat="server" /></p></div></div>
    <div class="tablero-kpis kpis-flota">
        <article class="kpi-card"><small>Plan</small><strong><asp:Literal ID="litActivoPlan" runat="server" /></strong><span><asp:Literal ID="litActivoAseguradora" runat="server" /></span></article>
        <article class="kpi-card"><small>Precio por mes</small><strong><asp:Literal ID="litActivoPrecio" runat="server" /></strong><span><asp:Literal ID="litActivoTarjeta" runat="server" /></span></article>
        <article class="kpi-card"><small>Vence</small><strong><asp:Literal ID="litActivoVence" runat="server" /></strong><span><asp:Literal ID="litActivoRenovacion" runat="server" /></span></article>
    </div>
    <dl class="remito-datos"><asp:Literal ID="litActivoDetalle" runat="server" /></dl>
    <asp:Button ID="btnCancelarSeguro" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar suscripción" CausesValidation="false" OnClick="btnCancelarSeguro_Click" OnClientClick="return confirm('¿Cancelar el seguro? Sigue activo hasta que venza el mes pagado y después no se renueva.');" />
</asp:Panel>

<section class="admin-section">
    <div class="section-title"><div><h2>Aseguradoras</h2><p>Elegí una para ver sus planes. Podés tener un solo seguro activo a la vez.</p></div></div>
    <div class="botonera">
        <asp:Repeater ID="rptAseguradoras" runat="server" OnItemCommand="rptAseguradoras_ItemCommand">
            <ItemTemplate><asp:LinkButton runat="server" CssClass="btn btn-outline-dark" CommandName="Ver" CommandArgument='<%# Container.DataItem %>' CausesValidation="false" Text='<%#: Container.DataItem %>' /></ItemTemplate>
        </asp:Repeater>
    </div>
    <asp:Panel ID="pnlPlanes" runat="server" Visible="false">
        <h3 class="mt-4">Planes de <asp:Literal ID="litAseguradora" runat="server" /></h3>
        <div class="table-responsive"><asp:GridView ID="gvPlanes" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" OnRowCommand="gvPlanes_RowCommand">
            <Columns>
                <asp:BoundField DataField="Nombre" HeaderText="Plan" />
                <asp:BoundField DataField="CoberturaPorViaje" HeaderText="Cobertura por viaje" DataFormatString="{0:C0}" />
                <asp:BoundField DataField="TextoViajes" HeaderText="Viajes por mes" />
                <asp:BoundField DataField="Detalle" HeaderText="Qué cubre" />
                <asp:BoundField DataField="Precio" HeaderText="Precio por mes" DataFormatString="{0:C0}" />
                <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                    <asp:LinkButton runat="server" Text="Contratar" CommandName="Contratar" CommandArgument='<%# Eval("IdPlan") %>' CausesValidation="false" Visible='<%# !TieneSeguro %>' />
                    <asp:LinkButton runat="server" Text="Cambiar a este plan" CommandName="Cambiar" CommandArgument='<%# Eval("IdPlan") %>' CausesValidation="false" Visible='<%# PuedeCambiarA((int)Eval("IdPlan")) %>' />
                </ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView></div>
    </asp:Panel>
</section>

<asp:Panel ID="pnlCambiar" runat="server" CssClass="admin-section" Visible="false">
    <div class="section-title"><div><h2>Cambiar al plan <asp:Literal ID="litCambioPlan" runat="server" /></h2><p><asp:Literal ID="litCambioResumen" runat="server" /></p></div></div>
    <p><asp:Literal ID="litCambioAjuste" runat="server" /></p>
    <div class="botonera">
        <asp:Button ID="btnConfirmarCambio" runat="server" CssClass="btn btn-brand" Text="Cambiar de plan" CausesValidation="false" OnClick="btnConfirmarCambio_Click"
            OnClientClick="return confirm('¿Cambiar de plan? La aseguradora emite una póliza nueva y la diferencia de precio se ajusta en la próxima renovación.');" />
        <asp:Button ID="btnCerrarCambio" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarCambio_Click" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlContratar" runat="server" CssClass="admin-section" Visible="false">
    <div class="section-title"><div><h2>Contratar el plan <asp:Literal ID="litPlanElegido" runat="server" /></h2><p><asp:Literal ID="litPlanResumen" runat="server" /></p></div></div>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="rblPago" Text="Cómo pagás" />
        <asp:RadioButtonList ID="rblPago" runat="server" CssClass="medio-pago-opciones" RepeatDirection="Horizontal" RepeatLayout="Flow" AutoPostBack="true" OnSelectedIndexChanged="rblPago_SelectedIndexChanged">
            <asp:ListItem Value="Guardada" Text="Tarjeta guardada" Selected="True" /><asp:ListItem Value="Nueva" Text="Otra tarjeta" /><asp:ListItem Value="Saldo" Text="Saldo de la billetera" /><asp:ListItem Value="MercadoPago" Text="Mercado Pago" />
        </asp:RadioButtonList>
    </div>
    <asp:Panel ID="pnlSaldo" runat="server" CssClass="form-field" Visible="false"><small><asp:Literal ID="litSaldo" runat="server" /></small></asp:Panel>
    <asp:Panel ID="pnlTarjetaGuardada" runat="server" CssClass="form-field"><asp:Label runat="server" AssociatedControlID="ddlTarjeta" Text="Tarjeta" /><asp:DropDownList ID="ddlTarjeta" runat="server" CssClass="form-select" /></asp:Panel>
    <asp:Panel ID="pnlTarjetaNueva" runat="server" Visible="false">
        <p>La tarjeta queda guardada en tu Billetera para los próximos pagos.</p>
        <div class="form-grid">
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtTitular" Text="Titular" /><asp:TextBox ID="txtTitular" runat="server" CssClass="form-control" MaxLength="25" placeholder="Como figura en la tarjeta" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtTitular" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtTitular" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtNumero" Text="Número" /><asp:TextBox ID="txtNumero" runat="server" CssClass="form-control" MaxLength="19" inputmode="numeric" autocomplete="off" data-tipo="tarjeta" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtNumero" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtNumero" ValidationExpression="^(\d{4} ?){3}\d{4}$" ErrorMessage="El número tiene 16 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /><asp:CustomValidator runat="server" ControlToValidate="txtNumero" ClientValidationFunction="validarTarjeta" ErrorMessage="Número de tarjeta inválido. Aceptamos Visa y Mastercard." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVencimiento" Text="Vencimiento" /><asp:TextBox ID="txtVencimiento" runat="server" CssClass="form-control" TextMode="Month" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVencimiento" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /><asp:CustomValidator runat="server" ControlToValidate="txtVencimiento" ClientValidationFunction="validarRango" ErrorMessage="Elegí este mes o uno posterior." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /></div>
            <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtCodigo" Text="Código de seguridad" /><asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" MaxLength="3" TextMode="Password" inputmode="numeric" autocomplete="off" data-tipo="digitos" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtCodigo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtCodigo" ValidationExpression="^\d{3}$" ErrorMessage="El código tiene 3 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="SeguroTarjeta" /></div>
        </div>
    </asp:Panel>
    <div class="botonera">
        <asp:Button ID="btnConfirmar" runat="server" CssClass="btn btn-brand" Text="Pagar y contratar" CausesValidation="true" OnClick="btnConfirmar_Click" ValidationGroup="SeguroTarjeta" />
        <asp:Button ID="btnCerrarContratar" runat="server" CssClass="btn btn-outline-dark" Text="Cancelar" CausesValidation="false" OnClick="btnCerrarContratar_Click" />
    </div>
</asp:Panel>

<section class="admin-section">
    <div class="section-title"><div><h2>Tus seguros</h2><p>Los que contrataste, con su póliza y su estado.</p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvHistorial" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no contrataste seguros.">
        <Columns>
            <asp:BoundField DataField="FechaAlta" HeaderText="Contratado" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Plan"><ItemTemplate><%#: Eval("Plan.Nombre") %><br /><small><%#: Eval("Plan.Aseguradora") %></small></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Monto" HeaderText="Precio por mes" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="NumeroPoliza" HeaderText="Póliza" NullDisplayText="-" />
            <asp:TemplateField HeaderText="Vence"><ItemTemplate><%#: ((DateTime)Eval("Vence")).ToString("dd/MM/yyyy") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
        </Columns>
    </asp:GridView></div>
</section>
