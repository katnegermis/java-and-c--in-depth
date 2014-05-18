<%@ Page Title="Sing in" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Signin.aspx.cs" Inherits="vfs.clients.web.Signin" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    Username: <asp:TextBox id="username" runat="server" />
    <asp:Button id="Submit" text="Sign in" onclick="signin" runat="server" /><br />
    Password: <asp:TextBox id="password" TextMode="Password" runat="server" />
</asp:Content>