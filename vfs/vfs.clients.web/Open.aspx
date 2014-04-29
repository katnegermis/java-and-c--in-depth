<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Open.aspx.cs" Inherits="vfs.clients.web.Open" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    Enter the full path of the VFS on the server's disk: <asp:TextBox id="vfsPath" runat="server" />
    <asp:Button id="submit" text="Open VFS" onclick="openVFS" runat="server" />

</asp:Content>
