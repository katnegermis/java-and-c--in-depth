using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using vfs.core;
using vfs.core.indexing;
using vfs.exceptions;
using vfs.common;

namespace vfs.clients.web {

    public partial class _Default : Page {

        private void showPage() {
            ls();
            search_ls();
            checkOperationsEnabled(null, null);
            showSpace();
        }

        private void ls() {
            filesView.DataSource = Global.vfsSession.ListCurrentDirectory();
            filesView.DataBind();
        }

        private void search_ls() {
            if(Global.vfsSession.currentSearchResults != null) {
                resultsView.DataSource = Global.vfsSession.currentSearchResults;
                resultsView.DataBind();
            }
        }

        protected void checkOperationsEnabled(object sender, EventArgs e) {
            bool atLeastOne = false;
            foreach(GridViewRow r in filesView.Rows) {
                CheckBox c = (CheckBox) r.Cells[0].Controls[1];
                if(c.Checked) {
                    atLeastOne = true;
                    break;
                }
            }
            copy.Enabled = cut.Enabled = delete.Enabled = atLeastOne;

            paste.Enabled = Global.vfsSession.clipBoardNonEmpty();
        }

        private void showSpace() {
            freeSpace.Text = Global.vfsSession.FreeSpace.ToString() + " bytes";
            occupiedSpace.Text = Global.vfsSession.OccupiedSpace.ToString() + " bytes";
        }

        protected void Page_Load(object sender, EventArgs e) {
            Master.checkSession();

            Page.Form.DefaultButton = "";
            SessionIDField.Value = Session.SessionID;

            if(!Page.IsPostBack) {
                hideSearch();
                showPage();
            }

            Global.vfsSession.updateScheduled = false;

            /*System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += (Object sa, System.Timers.ElapsedEventArgs ea) => {
                Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<Updates>()
                    .Clients.Client(Updates.sessionToConnection[Session.SessionID]).update();
            };
            aTimer.Interval = 6000;
            aTimer.Enabled = true;*/
        }

        protected void up(object sender, EventArgs e) {
            Master.checkSession();

            Global.vfsSession.MoveBack();

            hideSearch();
            showPage();
        }

        protected void cd(object sender, EventArgs e) {
            Master.checkSession();

            LinkButton b = (LinkButton) sender;
            Global.vfsSession.MoveInto(Server.HtmlDecode(b.Text), false);

            hideSearch();
            showPage();
        }

        private string[] getSelectedListViewItemTexts() {
            List<string> names = new List<string>();
            foreach(GridViewRow r in filesView.Rows) {
                CheckBox c = (CheckBox) r.FindControl("selectBox");
                if(c.Checked) {
                    LinkButton l = (LinkButton) r.FindControl("fileName");
                    names.Add(Server.HtmlDecode(l.Text));
                }
            }
            return names.ToArray();
        }

        protected void makeCopy(object sender, EventArgs e) {
            try {
                Master.checkSession();

                var names = getSelectedListViewItemTexts();
                Global.vfsSession.Copy(names);
            }
            catch(Exception ex) {
                Master.errorText = ex.ToString();
            }

            hideSearch();
            showPage();
        }

        protected void makeCut(object sender, EventArgs e) {
            try {
                Master.checkSession();

                var names = getSelectedListViewItemTexts();
                Global.vfsSession.Cut(names);

                //TODO show item with greyed out icon
            }
            catch(Exception ex) {
                Master.errorText = ex.ToString();
            }

            hideSearch();
            showPage();
        }

        protected void makePaste(object sender, EventArgs e) {
            try {
                Master.checkSession();

                Global.vfsSession.Paste();
                ls();
            }
            catch(Exception ex) {
                Master.errorText = ex.ToString();
            }

            hideSearch();
            showPage();
        }

        protected void makeDelete(object sender, EventArgs e) {
            try {
                Master.checkSession();

                var names = getSelectedListViewItemTexts();

                Global.vfsSession.Delete(names);
            }
            catch(Exception ex) {
                Master.errorText = ex.ToString();
            }

            hideSearch();
            showPage();
        }

        protected void makeCreateFolder(object sender, EventArgs e) {
            Master.checkSession();

            string newFolderName = "New Folder";
            uint index = 1;
            bool success = false;
            while(!success) {
                try {
                    Global.vfsSession.CreateDir(newFolderName);
                    success = true;
                }
                catch(FileAlreadyExistsException) {
                    newFolderName = String.Format("New Folder ({0})", index++);
                }
            }

            showPage();

            DirectoryEntry[] contents = Global.vfsSession.ListCurrentDirectory();
            for(int i = 0; i < contents.Length; i++) {
                if(contents[i].Name == newFolderName) {
                    RowEditing(i);
                    break;
                }
            }
        }

        protected void RowEditing(object sender, GridViewEditEventArgs e) {
            Master.checkSession();

            RowEditing(e.NewEditIndex);
        }

        private void RowEditing(int rowIndex) {
            LinkButton l = (LinkButton) filesView.Rows[rowIndex].FindControl("fileName");
            HttpContext.Current.Session["editOldName"] = Server.HtmlDecode(l.Text);

            filesView.EditIndex = rowIndex;
            hideSearch();
            showPage();
            filesView.Rows[rowIndex].FindControl("changeFileName").Focus();
            Page.Form.DefaultButton = filesView.Rows[rowIndex].FindControl("saveButton").UniqueID;

        }

        protected void RowCancelingEditing(object sender, GridViewCancelEditEventArgs e) {
            Master.checkSession();

            e.Cancel = true;
            filesView.EditIndex = -1;
            showPage();
        }

        protected void RowUpdating(object sender, GridViewUpdateEventArgs e) {
            Master.checkSession();

            string newName = e.NewValues["Name"].ToString();

            //To avoid error in Path.Combine()
            char[] invalid = System.IO.Path.GetInvalidPathChars();

            foreach(char c in invalid) { //Ugly, slow, works (probably)
                newName = newName.Replace(c.ToString(), "");
            }

            filesView.EditIndex = -1;

            try {
                Global.vfsSession.Rename((string) HttpContext.Current.Session["editOldName"], newName);
            }
            catch(Exception ex) {
                Master.errorText = "While trying to rename \"" + HttpContext.Current.Session["editOldName"] + "\" to \""
                    + newName + "\"\n" + ex.ToString();
            }

            showPage();
        }

        protected void makeSearch(object sender, EventArgs e) {
            Master.checkSession();

            if(search.Text.Trim() == "") {
                hideSearch();
            }
            else {
                Global.vfsSession.SearchCaseSensitive = caseSensitive.Checked;
                Global.vfsSession.SearchLocation = (noSubfolders.Checked ? SearchLocation.Folder : SearchLocation.SubFolder);
                //var count = Global.vfsSession.Search(search.Text).Length;
                Global.vfsSession.Search(search.Text);
                resultsView.Visible = true;
                //Master.errorText = (search.Text == Global.vfsSession.ListCurrentDirectory()[0].Name).ToString() + " " + count.ToString();
            }

            showPage();
        }

        private void hideSearch() {
            Global.vfsSession.currentSearchResults = null;
            search.Text = "";
            resultsView.DataBind();
            resultsView.Visible = false;
        }

        public void openContainingFolder(object sender, EventArgs e) {
            Master.checkSession();

            LinkButton b = (LinkButton) sender;
            Global.vfsSession.MoveInto(Helpers.PathGetDirectoryName(Server.HtmlDecode(b.Text)), true);

            showPage();
        }

        public void makeDownload(object sender, EventArgs e) {
            LinkButton b = (LinkButton) sender;
            Label size = (Label) b.Parent.Parent.FindControl("fileSize");
            //Master.errorText = "'" + size.Text + "'";
            Global.vfsSession.Download(Server.HtmlDecode(b.Text), size.Text, Response);
        }

        public void makeUpload(object sender, EventArgs e) {
            if(upload.HasFile) {
                Global.vfsSession.Upload(upload.PostedFiles);
            }

            hideSearch();
            showPage();
        }
    }
}