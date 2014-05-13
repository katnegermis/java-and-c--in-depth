using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Delete : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            Master.checkSession();
        }

        public void deleteVFS(object sender, EventArgs e) {
            Global.vfsSession.DeleteVFS();
            Global.TerminateSession();
            Master.checkSession();
        }

        public void cancel(object sender, EventArgs e) {
            Response.Redirect("~/");
        }
    }
}