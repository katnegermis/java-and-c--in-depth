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

        public void checkSession() {
            if(Global.vfsSession == null) {
                setClosedLinks();
                Response.Redirect("~/Welcome");
            }
        }
    }
}