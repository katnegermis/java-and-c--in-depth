using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Open : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            vfsPath.Focus();
        }

        public void openVFS(object sender, EventArgs e) {
            if(vfsPath.Text == "") {
                Master.errorText = "Please enter a path";
                return;
            }

            //submit.Text = vfsPath.Text;
            if(Global.vfsSession != null) {
                try {
                    Global.vfsSession.Close();
                }
                catch(Exception) {
                }
            }

            try {
                Global.vfsSession = VFSSession.OpenVFS(vfsPath.Text);
                if(Global.vfsSession != null) {
                    //success
                    Response.Redirect("~/");
                }
            }
            catch(Exception ex) {
                Master.errorText = ex.ToString();
                //error. =  ex.ToString();
            }
        }
    }
}