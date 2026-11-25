<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Billetera.ascx.cs" Inherits="LogTech.Controles.Billetera" %>
<%@ Register Src="~/Controles/BilleteraResumen.ascx" TagPrefix="uc" TagName="BilleteraResumen" %>
<%-- CU-005-025 Gestionar Fondos Escrow (tarjetas) y CU-005-026 Consultar Estado de Pago (billetera). --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">PAGOS</span><h1>Billetera</h1><p><asp:Literal ID="litBajada" runat="server" /></p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<uc:BilleteraResumen ID="ucResumen" runat="server" />

<div class="admin-grid">
    <asp:Panel ID="pnlCargar" runat="server" CssClass="admin-section">
        <h2>Cargar saldo</h2>
        <p>Con una de tus tarjetas, para tener saldo disponible en la billetera.</p>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlTarjetaCarga" Text="Tarjeta" /><asp:DropDownList ID="ddlTarjetaCarga" runat="server" CssClass="form-select" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtMontoCarga" Text="Monto ($)" /><asp:TextBox ID="txtMontoCarga" runat="server" CssClass="form-control" TextMode="Number" step="0.01" min="1000" max="5000000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMontoCarga" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /><asp:CustomValidator runat="server" ControlToValidate="txtMontoCarga" ClientValidationFunction="validarRango" ErrorMessage="El monto tiene que ser de $ 1.000 a $ 5.000.000." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Carga" /></div>
        <asp:Button ID="btnCargar" runat="server" CssClass="btn btn-brand" Text="Cargar saldo" CausesValidation="true" OnClick="btnCargar_Click" ValidationGroup="Carga" />
    </asp:Panel>
    <asp:Panel ID="pnlRetirar" runat="server" CssClass="admin-section">
        <h2>Retirar a tu cuenta</h2>
        <p>Transferimos lo que pidas, hasta tu saldo disponible, a un CBU o alias.</p>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtMontoRetiro" Text="Monto ($)" /><asp:TextBox ID="txtMontoRetiro" runat="server" CssClass="form-control" TextMode="Number" step="0.01" min="1000" max="5000000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtMontoRetiro" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Retiro" /><asp:CustomValidator runat="server" ControlToValidate="txtMontoRetiro" ClientValidationFunction="validarRango" ErrorMessage="El monto tiene que ser de $ 1.000 a $ 5.000.000." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Retiro" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtDestino" Text="CBU o alias" /><asp:TextBox ID="txtDestino" runat="server" CssClass="form-control" MaxLength="22" placeholder="22 números o un alias" data-tipo="cbu" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtDestino" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Retiro" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtDestino" ValidationExpression="^(\d{22}|(?=.*[A-Za-z])[A-Za-z0-9.\-]{6,20})$" ErrorMessage="CBU de 22 números o alias de 6 a 20 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Retiro" /></div>
        <asp:Button ID="btnRetirar" runat="server" CssClass="btn btn-outline-dark" Text="Retirar" CausesValidation="true" OnClick="btnRetirar_Click" OnClientClick="if (!Page_ClientValidate('Retiro') || !confirm('¿Transferir ese monto a la cuenta indicada?')) return false;" ValidationGroup="Retiro" />
    </asp:Panel>
</div>

<asp:Panel ID="pnlCbu" runat="server" CssClass="admin-section">
    <div class="section-title"><div><h2>CBU para el depósito</h2><p>Donde te depositamos la plata cuando retirás. Queda cargado en «Retirar a tu cuenta».</p></div></div>
    <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtCbu" Text="CBU o alias" /><asp:TextBox ID="txtCbu" runat="server" CssClass="form-control" MaxLength="22" placeholder="22 números o un alias" data-tipo="cbu" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtCbu" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Cbu" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtCbu" ValidationExpression="^(\d{22}|(?=.*[A-Za-z])[A-Za-z0-9.\-]{6,20})$" ErrorMessage="CBU de 22 números o alias de 6 a 20 caracteres." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Cbu" /></div>
    <div class="botonera">
        <asp:Button ID="btnGuardarCbu" runat="server" CssClass="btn btn-brand" Text="Guardar CBU" CausesValidation="true" OnClick="btnGuardarCbu_Click" ValidationGroup="Cbu" />
        <asp:Button ID="btnEliminarCbu" runat="server" CssClass="btn btn-outline-dark" Text="Eliminar" CausesValidation="false" OnClick="btnEliminarCbu_Click" OnClientClick="return confirm('¿Eliminar el CBU guardado?');" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlAPagar" runat="server" CssClass="admin-section">
    <div class="section-title"><div><h2>Viajes en efectivo para pagar</h2><p>Pagás en Pago Fácil o Rapipago con el código del viaje, hasta 30 días después del remito. En esta versión de demostración el pago se simula con el botón.</p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvAPagar" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No tenés viajes en efectivo para pagar." OnRowCommand="gvAPagar_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="Viaje N°" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %><br /><small>Transporta <%#: Eval("Transportista") %></small></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="PrecioDador" HeaderText="A pagar" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="VencePago" HeaderText="Vence" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="CodigoPago" HeaderText="Código de pago" />
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Simular pago en Pago Fácil" CommandName="Pagar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" OnClientClick="return confirm('¿Simular el pago en efectivo de este viaje?');" />
                <asp:LinkButton runat="server" Text="Pagar con saldo" CommandName="PagarSaldo" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" OnClientClick="return confirm('¿Pagar este viaje con el saldo de la billetera?');" />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</asp:Panel>

<asp:Panel ID="pnlFactoring" runat="server" CssClass="admin-section">
    <div class="section-title"><div><h2>Factoring</h2><p>Cobrá hoy los viajes en efectivo que el dador todavía no pagó, menos el 4 %. Se aprueba solo con un puntaje de 60 o más.</p></div></div>
    <div class="tablero-kpis kpis-flota">
        <article class="kpi-card"><small>Tu puntaje</small><strong><asp:Literal ID="litPuntaje" runat="server" /></strong><span>de 100 puntos</span></article>
        <article class="kpi-card"><small>Reputación</small><strong><asp:Literal ID="litPuntajeReputacion" runat="server" /></strong><span>hasta 60, por tus estrellas</span></article>
        <article class="kpi-card"><small>Viajes y disputas</small><strong><asp:Literal ID="litPuntajeViajes" runat="server" /></strong><span>hasta 30 por viajes entregados y 10 sin disputas perdidas</span></article>
    </div>
    <div class="table-responsive"><asp:GridView ID="gvAdelantables" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="No tenés viajes en efectivo a cobrar." OnRowCommand="gvAdelantables_RowCommand">
        <Columns>
            <asp:BoundField DataField="IdViaje" HeaderText="Viaje N°" />
            <asp:TemplateField HeaderText="Recorrido"><ItemTemplate><%#: Eval("Origen") %> → <%#: Eval("Destino") %><br /><small>Dador: <%#: Eval("Dador") %></small></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="NetoTransportista" HeaderText="Cobrás" DataFormatString="{0:C0}" />
            <asp:TemplateField HeaderText="Recibís hoy"><ItemTemplate><%#: Adelanto((decimal)Eval("NetoTransportista")).ToString("C0") %></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="VencePago" HeaderText="El dador paga hasta" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Pedir adelanto" CommandName="Adelantar" CommandArgument='<%# Eval("IdViaje") %>' CausesValidation="false" OnClientClick="return confirm('¿Pedir el adelanto de este viaje con el 4 % de comisión?');" />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
    <h3>Tus pedidos</h3>
    <div class="table-responsive"><asp:GridView ID="gvSolicitudes" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no pediste adelantos.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:TemplateField HeaderText="Viaje"><ItemTemplate>N° <%#: Eval("IdViaje") %><br /><small><%#: Eval("Recorrido") %></small></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="MontoAdelantado" HeaderText="Adelanto" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="Comision" HeaderText="Comisión" DataFormatString="{0:C0}" />
            <asp:BoundField DataField="Puntaje" HeaderText="Puntaje" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><%#: Eval("Estado") %><br /><small><%#: Eval("Motivo") %></small></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
</asp:Panel>

<section id="secMovimientos" runat="server" class="admin-section">
    <div class="section-title"><div><h2>Movimientos</h2><p><asp:Literal ID="litMovimientos" runat="server" /></p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvMovimientos" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no hay movimientos.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="Tipo" HeaderText="Tipo" />
            <asp:BoundField DataField="Detalle" HeaderText="Detalle" />
            <asp:TemplateField HeaderText="Monto"><ItemTemplate><span class='<%# (decimal)Eval("Monto") < 0 ? "monto-negativo" : "monto-positivo" %>'><%#: ((decimal)Eval("Monto")).ToString("C2") %></span></ItemTemplate></asp:TemplateField>
            <asp:BoundField DataField="Comprobante" HeaderText="Comprobante" NullDisplayText="-" />
        </Columns>
    </asp:GridView></div>
</section>

<section id="secMediosPago" runat="server" class="admin-section">
    <div class="section-title"><div><h2>Medios de pago</h2><p>Guardamos solo la marca y los últimos 4 números: el resto lo valida la pasarela de pago.</p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvTarjetas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no agregaste tarjetas." OnRowCommand="gvTarjetas_RowCommand">
        <Columns>
            <asp:BoundField DataField="Descripcion" HeaderText="Tarjeta" />
            <asp:BoundField DataField="Titular" HeaderText="Titular" />
            <asp:BoundField DataField="Vencimiento" HeaderText="Vence" />
            <asp:TemplateField HeaderText="Acción"><ItemTemplate>
                <asp:LinkButton runat="server" Text="Quitar" CommandName="Quitar" CommandArgument='<%# Eval("IdTarjeta") %>' CausesValidation="false" OnClientClick="return confirm('¿Quitar esta tarjeta?');" />
            </ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView></div>
    <h3>Agregar una tarjeta</h3>
    <div class="form-grid">
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtTitular" Text="Titular" /><asp:TextBox ID="txtTitular" runat="server" CssClass="form-control" MaxLength="25" placeholder="Como figura en la tarjeta" data-tipo="nombre" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtTitular" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtTitular" ValidationExpression="^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ' \-]+$" ErrorMessage="Usá solo letras (mínimo 2)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtNumero" Text="Número" /><asp:TextBox ID="txtNumero" runat="server" CssClass="form-control" MaxLength="19" inputmode="numeric" autocomplete="off" data-tipo="tarjeta" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtNumero" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtNumero" ValidationExpression="^(\d{4} ?){3}\d{4}$" ErrorMessage="El número tiene 16 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /><asp:CustomValidator runat="server" ControlToValidate="txtNumero" ClientValidationFunction="validarTarjeta" ErrorMessage="Número de tarjeta inválido. Aceptamos Visa y Mastercard." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtVencimiento" Text="Vencimiento" /><asp:TextBox ID="txtVencimiento" runat="server" CssClass="form-control" TextMode="Month" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtVencimiento" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /><asp:CustomValidator runat="server" ControlToValidate="txtVencimiento" ClientValidationFunction="validarRango" ErrorMessage="Elegí este mes o uno posterior." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /></div>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="txtCodigo" Text="Código de seguridad" /><asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" MaxLength="3" TextMode="Password" inputmode="numeric" autocomplete="off" data-tipo="digitos" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtCodigo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtCodigo" ValidationExpression="^\d{3}$" ErrorMessage="El código tiene 3 números." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Tarjeta" /></div>
    </div>
    <div class="botonera">
        <asp:Button ID="btnAgregarTarjeta" runat="server" CssClass="btn btn-brand" Text="Agregar tarjeta" CausesValidation="true" OnClick="btnAgregarTarjeta_Click" ValidationGroup="Tarjeta" />
        <asp:Button ID="btnMercadoPago" runat="server" CssClass="btn btn-outline-dark" Text="Vincular Mercado Pago" CausesValidation="false" OnClick="btnMercadoPago_Click" />
    </div>
</section>
