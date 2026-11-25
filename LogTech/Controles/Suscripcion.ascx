<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Suscripcion.ascx.cs" Inherits="LogTech.Controles.Suscripcion" %>
<%-- CU-008-040 Alta, CU-008-041 Modificar, CU-008-042 Baja y CU-008-043 Consultar suscripción: la flota contrata la
     Flota SaaS y paga por vehículo; el transportista, la membresía Premium. --%>
<div class="panel-heading">
    <div><span class="eyebrow eyebrow-light">PAGOS</span><h1><asp:Literal ID="litTitulo" runat="server" /></h1><p><asp:Literal ID="litBajada" runat="server" /></p></div>
</div>
<asp:Label ID="lblMensaje" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlVigente" runat="server" CssClass="admin-section" Visible="false">
    <div class="section-title"><div><h2>Tu suscripción</h2><p><asp:Literal ID="litEstado" runat="server" /></p></div></div>
    <div class="tablero-kpis kpis-flota">
        <article class="kpi-card"><small>Estado</small><strong><asp:Literal ID="litEstadoCorto" runat="server" /></strong><span><asp:Literal ID="litDesde" runat="server" /></span></article>
        <article class="kpi-card"><small>Pagás por mes</small><strong><asp:Literal ID="litPrecio" runat="server" /></strong><span><asp:Literal ID="litCupo" runat="server" /></span></article>
        <article class="kpi-card"><small>Vence</small><strong><asp:Literal ID="litVence" runat="server" /></strong><span><asp:Literal ID="litTarjeta" runat="server" /></span></article>
    </div>
</asp:Panel>

<asp:Panel ID="pnlFormulario" runat="server" CssClass="admin-section">
    <div class="section-title"><div><h2><asp:Literal ID="litFormulario" runat="server" /></h2><p><asp:Literal ID="litBeneficios" runat="server" /></p></div></div>
    <div class="form-grid">
        <div class="form-field">
            <asp:Label runat="server" AssociatedControlID="ddlTarjeta" Text="Cómo pagás" />
            <asp:DropDownList ID="ddlTarjeta" runat="server" CssClass="form-select" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlTarjeta" InitialValue="" ErrorMessage="Elegí una tarjeta o el saldo de la billetera." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Suscripcion" />
        </div>
    </div>
    <p class="mt-3"><strong><asp:Literal ID="litTotal" runat="server" /></strong></p>
    <div class="botonera">
        <asp:Button ID="btnGuardar" runat="server" CssClass="btn btn-brand" ValidationGroup="Suscripcion" OnClick="btnGuardar_Click"
            OnClientClick="if (!Page_ClientValidate('Suscripcion') || !confirm('¿Confirmás la suscripción? Se cobra con lo que elegiste y se renueva sola cada mes.')) return false;" />
    </div>
</asp:Panel>

<asp:Panel ID="pnlBaja" runat="server" CssClass="admin-section" Visible="false">
    <div class="section-title"><div><h2>Dar de baja</h2><p>Dejás de pagar desde el próximo mes. Seguís usando la suscripción hasta el último día pagado.</p></div></div>
    <asp:Button ID="btnBaja" runat="server" CssClass="btn btn-outline-danger" Text="Dar de baja la suscripción" CausesValidation="false" OnClick="btnBaja_Click" />
    <asp:Panel ID="pnlConfirmarBaja" runat="server" Visible="false" CssClass="mt-3">
        <p><asp:Literal ID="litAvisoBaja" runat="server" /></p>
        <div class="form-field"><asp:Label runat="server" AssociatedControlID="ddlMotivo" Text="Motivo" />
            <asp:DropDownList ID="ddlMotivo" runat="server" CssClass="form-select">
                <asp:ListItem Value="" Text="Elegí el motivo" /><asp:ListItem Value="Ya no lo necesito" Text="Ya no lo necesito" />
                <asp:ListItem Value="Es caro" Text="Es caro" /><asp:ListItem Value="Uso otra plataforma" Text="Uso otra plataforma" /><asp:ListItem Value="Otro motivo" Text="Otro motivo" />
            </asp:DropDownList>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlMotivo" InitialValue="" ErrorMessage="Elegí el motivo de la baja." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Baja" />
        </div>
        <div class="botonera">
            <asp:Button ID="btnConfirmarBaja" runat="server" CssClass="btn btn-outline-danger" Text="Confirmar la baja" ValidationGroup="Baja" OnClick="btnConfirmarBaja_Click"
                OnClientClick="if (!Page_ClientValidate('Baja') || !confirm('¿Dar de baja la suscripción? No se vuelve a cobrar y termina al vencer el mes pagado.')) return false;" />
            <asp:Button ID="btnCancelarBaja" runat="server" CssClass="btn btn-outline-dark" Text="No, mantenerla" CausesValidation="false" OnClick="btnCancelarBaja_Click" />
        </div>
    </asp:Panel>
</asp:Panel>

<section class="admin-section">
    <div class="section-title"><div><h2>Facturas de la suscripción</h2><p>Cada mes cobrado tiene su factura. Las descargás en PDF desde la sección Facturas.</p></div></div>
    <div class="table-responsive"><asp:GridView ID="gvFacturas" runat="server" AutoGenerateColumns="false" CssClass="table audit-table tabla-modulo" GridLines="None" EmptyDataText="Todavía no hay facturas de la suscripción.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="Comprobante" HeaderText="Comprobante" ItemStyle-Wrap="false" />
            <asp:BoundField DataField="Detalle" HeaderText="Detalle" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:C2}" ItemStyle-Wrap="false" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
        </Columns>
    </asp:GridView></div>
</section>
