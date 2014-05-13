<%@ Page Title="Browser" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="vfs.clients.web._Default" ValidateRequest="false" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ImageButton id="parentFolder" ImageUrl="parent.png" OnClick="up" AccessKey="u" TabIndex="-1" runat="server" />
    <asp:GridView ID="filesView" AutoGenerateColumns="False" Font-Size="Larger" runat="server"
        OnRowEditing="RowEditing" OnRowCancelingEdit="RowCancelingEditing" OnRowUpdating="RowUpdating">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="selectBox" Checked="false" AutoPostBack="true" OnCheckedChanged="checkOperationsEnabled" TabIndex="3" runat="server" />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:CheckBox ID="selectBox" Checked="false" TabIndex="-1" runat="server" Enabled="false" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:ImageField HeaderText="" DataImageUrlField="TypeURL" ReadOnly="true">
            </asp:ImageField>
            <asp:TemplateField HeaderText="Name" SortExpression="Name">
                    <ItemTemplate>
                        <asp:ImageButton ID="editButton" ImageUrl="edit.png" CommandName="Edit" TabIndex="4" runat="server" />
                        <asp:LinkButton ID="openButton" runat="server" Text='<%#: Bind("Name") %>' OnClick="cd" Visible='<%# Eval("IsFolder") %>' TabIndex="2"></asp:LinkButton>
                        <asp:Label ID="fileName" runat="server" Text='<%#: Bind("Name") %>' Visible='<%# !(bool)Eval("IsFolder") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:ImageButton ID="saveButton" ImageUrl="save.png" CommandName="Update" TabIndex="1" runat="server" />
                        <asp:ImageButton ID="cancelButton" ImageUrl="cancel.png" CommandName="Cancel" TabIndex="1" runat="server" />
                        <asp:TextBox ID="changeFileName" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox> 
                    </EditItemTemplate> 
                </asp:TemplateField>
            <asp:BoundField HeaderText="Size" DataField="Size" ReadOnly="true" />
        </Columns>
    </asp:GridView>
    <br />
    <asp:Button ID="copy" Text="Copy (c)" OnClick="makeCopy" AccessKey="c" TabIndex="-1" runat="server" />
    <asp:Button ID="cut" Text="Cut (x)" OnClick="makeCut" AccessKey="x" TabIndex="-1" runat="server" />
    <asp:Button ID="delete" Text="Delete (r)" OnClick="makeDelete" AccessKey="r" TabIndex="-1" runat="server" />
    <asp:Button ID="paste" Text="Paste (v)" OnClick="makePaste" AccessKey="v" TabIndex="-1" runat="server" />
    <asp:Button ID="newFolder" Text="Create new folder (i)" OnClick="makeCreateFolder" AccessKey="i" TabIndex="-1" runat="server" />


</asp:Content>
