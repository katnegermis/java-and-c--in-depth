using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Signout : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if(Global.vfsSession != null && Global.vfsSession.LoggedIn()) {
                Global.vfsSession.LogOut();
            }
            HttpContext.Current.Session["username"] = null;
            HttpContext.Current.Session["password"] = null;

            Response.Redirect("~/");
        }
    }
}