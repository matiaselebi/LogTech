<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DetalleViaje.ascx.cs" Inherits="LogTech.Controles.DetalleViaje" %>
<%@ Register Src="~/Controles/EstadoViajeBadge.ascx" TagPrefix="uc" TagName="EstadoViajeBadge" %>
<%-- Monitoreo (CU-007-036) y remito digital (CU-004-023) de un viaje. Lo usan "Mis cargas",
     "Mis viajes" y "Remitos". --%>
<asp:Label ID="lblDetalle" runat="server" CssClass="panel-message" role="status" />

<asp:Panel ID="pnlMonitoreo" runat="server" Visible="false" CssClass="admin-section detalle-viaje">
    <div class="section-title">
        <div><h2>Monitoreo del viaje N° <asp:Literal ID="litViaje" runat="server" /></h2></div>
        <asp:LinkButton ID="btnCerrarMonitoreo" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Cerrar" CausesValidation="false" OnClick="btnCerrar_Click" />
    </div>
    <p class="detalle-resumen"><asp:Literal ID="litRecorrido" runat="server" /> <uc:EstadoViajeBadge ID="ucEstado" runat="server" /></p>
    <asp:Label ID="lblAviso" runat="server" CssClass="panel-message error" Visible="false" role="status" />
    <div class="monitoreo-grid">
        <iframe id="frmMapa" runat="server" class="monitoreo-mapa" title="Mapa con la ubicación del camión"></iframe>
        <ol class="linea-tiempo">
            <asp:Repeater ID="rptEventos" runat="server">
                <ItemTemplate>
                    <li class='<%# (bool)Eval("EsIncidencia") ? "es-incidencia" : "" %>'>
                        <time><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></time>
                        <strong><%#: Eval("Titulo") %></strong>
                        <span><%#: Eval("Detalle") %></span>
                        <asp:HyperLink runat="server" Visible='<%# (bool)Eval("TieneFoto") %>' NavigateUrl='<%# "~/Archivo.ashx?tipo=incidencia&id=" + Eval("IdIncidencia") %>' Target="_blank" Text="Ver foto" />
                    </li>
                </ItemTemplate>
            </asp:Repeater>
        </ol>
    </div>
</asp:Panel>

<asp:Panel ID="pnlRemito" runat="server" Visible="false" CssClass="admin-section remito-digital">
    <div class="section-title">
        <div><h2>Remito digital del viaje N° <asp:Literal ID="litRemitoViaje" runat="server" /></h2></div>
        <asp:LinkButton ID="btnCerrarRemito" runat="server" CssClass="btn btn-outline-dark btn-sm" Text="Cerrar" CausesValidation="false" OnClick="btnCerrar_Click" />
    </div>
    <asp:Label ID="lblIntegridad" runat="server" />
    <dl class="remito-datos">
        <dt>Recorrido</dt><dd><asp:Literal ID="litRemitoRecorrido" runat="server" /></dd>
        <dt>Mercadería</dt><dd><asp:Literal ID="litRemitoMercaderia" runat="server" /></dd>
        <dt>Dador de carga</dt><dd><asp:Literal ID="litRemitoDador" runat="server" /></dd>
        <dt>Transportista</dt><dd><asp:Literal ID="litRemitoTransportista" runat="server" /></dd>
        <dt>Recibió</dt><dd><asp:Literal ID="litRemitoReceptor" runat="server" /></dd>
        <dt>Fecha de emisión</dt><dd><asp:Literal ID="litRemitoFecha" runat="server" /></dd>
        <dt>Token</dt><dd><code><asp:Literal ID="litRemitoToken" runat="server" /></code></dd>
    </dl>
    <asp:HyperLink ID="lnkRemitoFoto" runat="server" CssClass="btn btn-outline-dark" Target="_blank" Text="Ver foto del remito firmado" />
</asp:Panel>
