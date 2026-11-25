<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="SemaforoDocumentacion.ascx.cs" Inherits="LogTech.Controles.SemaforoDocumentacion" %>
<div class="semaforo semaforo-<%: (Color ?? "Verde").ToLowerInvariant() %>" role="status">
    <span class="semaforo-luz" aria-hidden="true"></span>
    <div><strong><%: Titulo %></strong><small><%: Detalle %></small></div>
</div>
