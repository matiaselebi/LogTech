<%@ Page Title="Recuperar contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperarClave.aspx.cs" Inherits="LogTech.RecuperarClave" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="account-page">
        <section class="container recovery-shell" aria-labelledby="recoveryTitle">
            <div class="account-card">
                <span class="eyebrow">RECUPERACIÓN DE ACCESO</span>
                <h1 id="recoveryTitle">Recuperá tu contraseña</h1>
                <p>Ingresá el correo de tu cuenta. Te vamos a enviar un enlace para crear una contraseña nueva.</p>
                <p class="required-note"><span>*</span> Campos obligatorios</p>
                <div class="form-field"><label for="<%= txtCorreo.ClientID %>">Correo electrónico <span class="required-mark">*</span></label><asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100" autocomplete="email" data-tipo="correo" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtCorreo" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Recuperar" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtCorreo" ValidationExpression="^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$" ErrorMessage="Ingresá un correo válido (ej.: nombre@empresa.com)." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Recuperar" /></div>
                <asp:Label ID="lblMensaje" runat="server" CssClass="form-error" role="alert" />
                <div class="account-buttons"><asp:Button ID="btnRecuperar" runat="server" Text="Recuperar contraseña" CssClass="btn btn-brand btn-lg" OnClick="btnRecuperar_Click" CausesValidation="true" ValidationGroup="Recuperar" /><a runat="server" href="~/Login.aspx" class="btn btn-outline-dark btn-lg">Volver</a></div>
            </div>
        </section>
    </main>
</asp:Content>
