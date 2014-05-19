using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Add : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if(Global.vfsSession != null && Global.vfsSession.LoggedIn()) {
                //try {
                    Global.vfsSession.AddVFS();
                    Response.Redirect("~/");
                //}
                //catch(Exception ex) {
                //    Master.errorText = ex.ToString();
                //}
            }
            else {
                Master.checkSession();
                Response.Redirect("~/");
            }
        }
    }
}