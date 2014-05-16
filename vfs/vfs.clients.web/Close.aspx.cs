using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Close : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            Global.TerminateSession(Session.SessionID);
            Master.checkSession();
        }
    }
}