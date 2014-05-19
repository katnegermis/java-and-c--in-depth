<%@ Page Title="Sing in" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Signin.aspx.cs" Inherits="vfs.clients.web.Signin" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    Username: <asp:TextBox id="username" runat="server" TabIndex="1" />
    <asp:Button id="signin" text="Sign in" onclick="SignIn" runat="server" /><br />
    Password: <asp:TextBox id="password" TextMode="Password" runat="server" TabIndex="2" />
    <asp:Button id="create" text="Create account" onclick="CreateAccount" runat="server" />
</asp:Content>