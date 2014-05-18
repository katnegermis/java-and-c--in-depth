using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class SiteMaster : MasterPage {
        protected void Page_Load(object sender, EventArgs e) {
            errorText = "";

            if(Global.vfsSession == null) {
                setClosedLinks();
            }
            else {
                setOpenLinks();
            }

            if(HttpContext.Current.Session["username"] != null) {
                setSignoutLink();
            }
            else {
                setSigninLink();
            }
        }

        public string errorText {
            set {
                errorLabel.Text = value;
            }
        }

        public void setOpenLinks() {
            openLink.Visible = createLink.Visible = false;
            closeLink.Visible = deleteLink.Visible = true;
        }

        public void setClosedLinks() {
            closeLink.Visible = deleteLink.Visible = false;
            openLink.Visible = createLink.Visible = true;
        }

        public void setSigninLink() {
            signoutLink.Visible = false;
            signinLink.Visible = true;

            if(Global.vfsSession == null) {
                signinReason.InnerText = "retrieve VFS";
            }
            else if(Global.vfsSession.IsSynchronized()) {
                signinReason.InnerText = "synchronize VFS";
            }
            else {
                signinReason.InnerText = "add VFS to account";
            }
        }

        public void setSignoutLink() {
            signinLink.Visible = false;
            signoutLink.Visible = true;

            if(Global.vfsSession == null) {
                retrieveLink.Visible = true;
                addLink.Visible = removeLink.Visible = false;
            }
            else if(Global.vfsSession.IsSynchronized()) {
                removeLink.Visible = true;
                addLink.Visible = retrieveLink.Visible = false;
            }
            else {
                addLink.Visible = true;
                retrieveLink.Visible = removeLink.Visible = false;
            }
        }

        public void checkSession() {
            if(Global.vfsSession == null) {
                if(HttpContext.Current.Session["username"] == null) {
                    Response.Redirect("~/Welcome");
                }
                else {
                    Response.Redirect("~/Retrieve");
                }
            }
        }
    }
}