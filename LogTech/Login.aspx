<%@ Page Title="Iniciar sesión" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LogTech.Login" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="auth-page">
        <section class="auth-shell" aria-labelledby="loginTitle">
            <div class="auth-copy">
                <span class="eyebrow"><asp:Literal ID="litEtiqueta" runat="server" /></span>
                <h1 id="loginTitle"><asp:Literal ID="litTitulo" runat="server" /></h1>
                <p><asp:Literal ID="litBajada" runat="server" /></p>
            </div>
            <div class="login-card">
                <div class="login-card-head"><div><strong><asp:Literal ID="litAcceso" runat="server" /></strong><small>LogTech</small></div></div>
                <div class="form-field">
                    <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="txtUsuario" />
                    <asp:TextBox ID="txtUsuario" MaxLength="200" runat="server" CssClass="form-control" autocomplete="username" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtUsuario" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Login" />
                </div>
                <div class="form-field">
                    <asp:Label ID="lblClave" runat="server" AssociatedControlID="txtClave" />
                    <asp:TextBox ID="txtClave" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="current-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtClave" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Login" />
                </div>
                <asp:Label ID="lblError" runat="server" CssClass="form-error" role="alert" />
                <asp:Button ID="btnIngresar" runat="server" CssClass="btn btn-brand btn-lg w-100" OnClick="btnIngresar_Click" CausesValidation="true" ValidationGroup="Login" />
                <div class="auth-actions">
                    <a runat="server" href="~/Registro.aspx" class="btn btn-outline-dark">Crear cuenta</a>
                    <a runat="server" href="~/RecuperarClave.aspx" class="auth-recovery-link">Olvidé mi contraseña</a>
                </div>
            </div>
        </section>
    </main>
</asp:Content>
