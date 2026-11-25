<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DetalleVehiculoCard.ascx.cs" Inherits="LogTech.Controles.DetalleVehiculoCard" %>
<article class="vehiculo-card<%: Vehiculo != null && Vehiculo.Estado == "Inactivo" ? " is-inactivo" : "" %>">
    <%-- Semáforo de la documentación, arriba a la derecha (no en el match del dador ni en los dados de baja). --%>
    <% if (ConSemaforo) { %>
    <span class="vehiculo-semaforo semaforo-<%: Vehiculo.Semaforo.ToLowerInvariant() %>" title="<%: Vehiculo.MotivoSemaforo %>"><span class="semaforo-luz" aria-hidden="true"></span><%: TextoSemaforo %></span>
    <% } %>
    <div class="vehiculo-patente"><%: Vehiculo == null ? "" : Vehiculo.Patente %></div>
    <h3><%: Vehiculo == null ? "" : Vehiculo.Marca + " " + Vehiculo.Modelo %></h3>
    <ul>
        <li>Capacidad: <strong><%: Vehiculo == null ? "" : Vehiculo.CapacidadToneladas.ToString("0.##") + " t" %></strong></li>
        <li>Acoplado: <strong><%: Vehiculo == null ? "" : Vehiculo.TipoAcoplado %></strong></li>
        <li>Estado: <strong><%: Situacion %></strong></li>
        <% if (Vehiculo != null && Vehiculo.ConductorDesignado != null) { %>
        <li>Conductor designado: <strong><%: Vehiculo.ConductorDesignado %></strong></li>
        <% } %>
    </ul>
    <% if (ConSemaforo && Vehiculo.MotivoSemaforo != null) { %>
    <p class="vehiculo-motivo motivo-<%: Vehiculo.Semaforo.ToLowerInvariant() %>"><%: Vehiculo.MotivoSemaforo %></p>
    <% } %>
</article>
