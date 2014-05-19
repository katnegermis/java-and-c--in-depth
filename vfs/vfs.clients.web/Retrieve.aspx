<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Retrieve.aspx.cs" Inherits="vfs.clients.web.Retrieve" %>
<%@ MasterType virtualpath="~/Site.Master" %>

<%@ Register TagPrefix="vs" Namespace="Vladsm.Web.UI.WebControls" Assembly="GroupRadioButton" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <asp:GridView ID="retrieveView" AutoGenerateColumns="False" Font-Size="Larger" runat="server">
        <Columns>
            <asp:TemplateField>
                    <ItemTemplate>
                        <vs:GroupRadioButton ID="selectButton" GroupName="selectButton" runat="server" TabIndex="1"></vs:GroupRadioButton>
                    </ItemTemplate>
                </asp:TemplateField>
            <asp:BoundField HeaderText="Name" DataField="Name" />
        </Columns>
        <EmptyDataTemplate>There are no VFSes connected to this account</EmptyDataTemplate>
    </asp:GridView>

</asp:Content>
