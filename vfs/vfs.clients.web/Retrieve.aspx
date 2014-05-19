<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Retrieve.aspx.cs" Inherits="vfs.clients.web.Retrieve" %>
<%@ MasterType virtualpath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <asp:GridView ID="retrieveView" AutoGenerateColumns="False" Font-Size="Larger" runat="server">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <input type="radio" name='VFSid' value='<%# Eval("Item1") %>' />
                    <asp:HiddenField ID="VFSidComp" Value='<%# Eval("Item1") %>' runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Name">
                <ItemTemplate>
                    <asp:Label ID="VFSname" Text='<%# Eval("Item2") %>' runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            
        </Columns>
        <EmptyDataTemplate>There are no VFSes connected to this account</EmptyDataTemplate>
    </asp:GridView>
    <br />
    <asp:Button ID="downloadButton" OnClick="download" Text="Download VFS" runat="server" /><br />
    or<br />
    Enter the full path on the server: <asp:TextBox ID="serverPath" runat="server"></asp:TextBox>
    <asp:Button ID="serverSaveButton" OnClick="saveOnServer" Text="Save VFS on server" runat="server" /> <br />
    <asp:Label ID="statusText" ForeColor="Green" runat="server"></asp:Label>

</asp:Content>