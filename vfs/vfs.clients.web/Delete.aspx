<%@ Page Title="Delete VFS" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Delete.aspx.cs" Inherits="vfs.clients.web.Delete" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    Are you sure you want to delete this VFS?
    <asp:Button ID="no" OnClick="cancel" Text="No" runat="server" />
    <asp:Button ID="yes" OnClick="deleteVFS" Text="Yes" runat="server" />
</asp:Content>
