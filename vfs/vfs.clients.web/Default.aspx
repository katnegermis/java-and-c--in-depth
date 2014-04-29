<%@ Page Title="Browser" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="vfs.clients.web._Default" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ImageButton id="parentFolder" ImageUrl="parent.png" OnClick="up" runat="server" />
    <asp:GridView ID="filesView" AutoGenerateColumns="False" Font-Size="Larger" runat="server">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="selectBox" Checked="false" AutoPostBack="true" OnCheckedChanged="checkOperationsEnabled" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:ImageField HeaderText="" DataImageUrlField="TypeURL">
            </asp:ImageField>
            <asp:TemplateField HeaderText="Name" SortExpression="Name">
                    <itemtemplate>
                        <asp:LinkButton ID="openButton" runat="server" Text='<%# Eval("Name") %>' OnClick="cd" Enabled='<%# Eval("IsFolder") %>'></asp:LinkButton>
                    </itemtemplate>
                </asp:TemplateField>
            <asp:BoundField HeaderText="Size" DataField="Size" />
        </Columns>
    </asp:GridView>
    <br />
    <asp:Button ID="copy" Text="Copy" OnClick="makeCopy" runat="server" />
    <asp:Button ID="cut" Text="Cut" OnClick="makeCut" runat="server" />
    <asp:Button ID="delete" Text="Delete" OnClick="makeDelete" runat="server" />
    <asp:Button ID="paste" Text="Paste" OnClick="makePaste" runat="server" />


</asp:Content>
