<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="BilleteraResumen.ascx.cs" Inherits="LogTech.Controles.BilleteraResumen" %>
<div class="billetera-resumen">
    <% if (MostrarSaldo) { %><div><small>Saldo disponible</small><strong><%: SaldoDisponible.ToString("C2") %></strong></div><% } %>
    <div><small>Fondos en escrow</small><strong><%: SaldoRetenido.ToString("C2") %></strong><span><%: DetalleRetenido %></span></div>
</div>
