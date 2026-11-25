<%@ Page Title="Restablecer contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RestablecerClave.aspx.cs" Inherits="LogTech.RestablecerClave" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="account-page">
        <section class="container recovery-shell" aria-labelledby="resetTitle">
            <div class="account-card">
                <span class="eyebrow">RECUPERACIÓN DE ACCESO</span>
                <h1 id="resetTitle">Creá una nueva contraseña</h1>
                <asp:Panel ID="pnlFormulario" runat="server">
                    <p>Escribí tu contraseña nueva dos veces. Al confirmar, también se quita el bloqueo por intentos fallidos.</p>
                    <p class="required-note"><span>*</span> Campos obligatorios</p>
                    <div class="form-field"><label for="<%= txtNuevaClave.ClientID %>">Nueva contraseña <span class="required-mark">*</span></label><asp:TextBox ID="txtNuevaClave" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtNuevaClave" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Restablecer" /><asp:RegularExpressionValidator runat="server" ControlToValidate="txtNuevaClave" ValidationExpression="^(?=.*[A-ZÀ-ÖØ-Þ])(?=.*[^A-Za-zÀ-ÖØ-öø-ÿ0-9\s]).{8,}$" ErrorMessage="Mínimo 8 caracteres, una mayúscula y un carácter especial." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Restablecer" /><small>Mínimo 8 caracteres, una mayúscula y un carácter especial.</small></div>
                    <div class="form-field"><label for="<%= txtConfirmarClave.ClientID %>">Confirmar contraseña <span class="required-mark">*</span></label><asp:TextBox ID="txtConfirmarClave" MaxLength="64" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /><asp:RequiredFieldValidator runat="server" ControlToValidate="txtConfirmarClave" ErrorMessage="Completá este campo." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Restablecer" /><asp:CompareValidator runat="server" ControlToValidate="txtConfirmarClave" ControlToCompare="txtNuevaClave" ErrorMessage="Las contraseñas no coinciden." Display="Dynamic" CssClass="validador" SetFocusOnError="true" ValidationGroup="Restablecer" /></div>
                    <asp:Label ID="lblMensaje" runat="server" CssClass="form-error" role="alert" />
                    <div class="account-buttons"><asp:Button ID="btnGuardar" runat="server" Text="Actualizar contraseña" CssClass="btn btn-brand btn-lg" OnClick="btnGuardar_Click" CausesValidation="true" ValidationGroup="Restablecer" /><a runat="server" href="~/Login.aspx" class="btn btn-outline-dark btn-lg">Volver</a></div>
                </asp:Panel>
                <asp:Panel ID="pnlInvalido" runat="server" Visible="false">
                    <p>El enlace no es válido o ya venció. Pedí uno nuevo desde la pantalla de recuperación.</p>
                    <div class="account-buttons"><a runat="server" href="~/RecuperarClave.aspx" class="btn btn-brand btn-lg">Pedir un enlace nuevo</a></div>
                </asp:Panel>
            </div>
        </section>
    </main>
</asp:Content>
