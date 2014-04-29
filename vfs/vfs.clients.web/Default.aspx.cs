using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using vfs.core;
using vfs.core.indexing;

namespace vfs.clients.web {

    public partial class _Default : Page {

        private void showPage() {
            ls();
            checkOperationsEnabled(null, null);
        }

        private void ls() {
            filesView.DataSource = Global.vfsSession.ListCurrentDirectory();
            filesView.DataBind();
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

        protected void Page_Load(object sender, EventArgs e) {
            Master.checkSession();

            if(!Page.IsPostBack) {
                showPage();
            }
        }

        protected void up(object sender, EventArgs e) {
            Master.checkSession();

            Global.vfsSession.MoveBack();

            showPage();
        }

        protected void cd(object sender, EventArgs e) {
            Master.checkSession();

            LinkButton b = (LinkButton) sender;
            Global.vfsSession.MoveInto(b.Text, false);

            showPage();
        }

        private string[] getSelectedListViewItemTexts() {
            List<string> names = new List<string>();
            foreach(GridViewRow r in filesView.Rows) {
                CheckBox c = (CheckBox) r.Cells[0].Controls[1];
                if(c.Checked) {
                    LinkButton b = (LinkButton) r.Cells[2].Controls[1];
                    names.Add(b.Text);
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

            showPage();
        }

    }
}