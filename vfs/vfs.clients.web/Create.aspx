<%@ Page Title="Create VFS" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Create.aspx.cs" Inherits="vfs.clients.web.Create" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    Enter the full server-side path where the VFS should be created: <asp:TextBox id="vfsPath" runat="server" />
    <asp:Button id="Submit" text="Create VFS" onclick="createVFS" runat="server" /><br />
    Enter the maximum size of the new VFS: <asp:TextBox id="maxSize" runat="server" />

</asp:Content>
