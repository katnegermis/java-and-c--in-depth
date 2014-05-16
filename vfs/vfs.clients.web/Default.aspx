<%@ Page Title="Browser" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="vfs.clients.web._Default" ValidateRequest="false" %>
<%@ MasterType virtualpath="~/Site.Master" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ImageButton id="parentFolder" ImageUrl="parent.png" OnClick="up" AccessKey="u" TabIndex="-1" runat="server" />
    <asp:TextBox ID="search" runat="server" AutoPostBack="true" OnTextChanged="makeSearch" placeholder="Search in this folder... (a)" AccessKey="a" TabIndex="-1"></asp:TextBox><br />
    <asp:CheckBox ID="caseSensitive" Text="Case sensitive (t)" AutoPostBack="true" OnCheckedChanged="makeSearch" AccessKey="t" TabIndex="-1" runat="server"/>
    <asp:CheckBox ID="noSubfolders" Text="Don't search in subfolders (b)" AutoPostBack="true" OnCheckedChanged="makeSearch" AccessKey="b" TabIndex="-1" runat="server"/><br />
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
                        <asp:LinkButton ID="fileName" runat="server" Text='<%#: Bind("Name") %>' OnClick="makeDownload" Visible='<%# !(bool)Eval("IsFolder") %>' TabIndex="2"></asp:LinkButton>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:ImageButton ID="saveButton" ImageUrl="save.png" CommandName="Update" TabIndex="1" runat="server" />
                        <asp:ImageButton ID="cancelButton" ImageUrl="cancel.png" CommandName="Cancel" TabIndex="1" runat="server" />
                        <asp:TextBox ID="changeFileName" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox> 
                    </EditItemTemplate> 
                </asp:TemplateField>
            <asp:TemplateField HeaderText="Size">
                <ItemTemplate>
                    <asp:Label ID="fileSize" runat="server" Text='<%#: Eval("Size") %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>(Empty directory)</EmptyDataTemplate>
    </asp:GridView>
    <br />

    <asp:Button ID="copy" Text="Copy (c)" OnClick="makeCopy" AccessKey="c" TabIndex="-1" runat="server" />
    <asp:Button ID="cut" Text="Cut (x)" OnClick="makeCut" AccessKey="x" TabIndex="-1" runat="server" />
    <asp:Button ID="delete" Text="Delete (r)" OnClick="makeDelete" AccessKey="r" TabIndex="-1" runat="server" />
    <asp:Button ID="paste" Text="Paste (v)" OnClick="makePaste" AccessKey="v" TabIndex="-1" runat="server" />
    <asp:Button ID="newFolder" Text="Create new folder (i)" OnClick="makeCreateFolder" AccessKey="i" TabIndex="-1" runat="server" />
    
    &nbsp;&nbsp;Upload Files (p): &nbsp;<asp:FileUpload ID="upload" AllowMultiple="true" runat="server" AccessKey="p"  TabIndex="-1" onchange="document.getElementById('uploadFile').click()" />
    <asp:Button ID="uploadFile" runat="server" Text="" ClientIDMode="Static" OnClick="makeUpload" TabIndex="-1" class="hidden" />
    <br />

    <asp:GridView ID="resultsView" AutoGenerateColumns="False" Font-Size="Larger" runat="server">
        <Columns>
            <asp:ImageField HeaderText="" DataImageUrlField="TypeURL">
            </asp:ImageField>
            <asp:TemplateField HeaderText="Name" SortExpression="Name">
                    <ItemTemplate>
                        <asp:LinkButton ID="openDirButton" runat="server" Text='<%#: Eval("Path") %>' OnClick="openContainingFolder" TabIndex="2"></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            <asp:BoundField HeaderText="Size" DataField="Size" ReadOnly="true" />
        </Columns>
        <EmptyDataTemplate>(Nothing found)</EmptyDataTemplate>
    </asp:GridView>


    <asp:HiddenField ID="SessionIDField" runat="server" ClientIDMode="Static" />
    <script src="Scripts/jquery.signalR-2.0.3.min.js"></script>
    <script src="/signalr/hubs"></script>
    <script type="text/javascript">
        $(function () {
            $.connection.hub.qs = "session=" + document.getElementById("SessionIDField").value
            $.connection.updates.client.update = function () {
                //console.log("There is an update available!")

                //Refresh without POST
                window.location = window.location
            };
            $.connection.hub.start()
        });
    </script>

</asp:Content>
