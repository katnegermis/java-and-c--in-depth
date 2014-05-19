using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Retrieve : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if(Global.vfsSession != null) {
                Response.Redirect("~/");
            }

            List<Tuple<long, string>> VFSes = new List<Tuple<long, string>>();

            if(HttpContext.Current.Session["tmpUsername"] != null
                && HttpContext.Current.Session["tmpPassword"] != null) {
                try {
                    VFSes = VFSSession.ListVFSes((string) HttpContext.Current.Session["tmpUsername"],
                        (string) HttpContext.Current.Session["tmpPassword"]);
                    HttpContext.Current.Session["username"] = HttpContext.Current.Session["tmpUsername"];
                    HttpContext.Current.Session["password"] = HttpContext.Current.Session["tmpPassword"];
                    HttpContext.Current.Session["tmpUsername"] = null;
                    HttpContext.Current.Session["tmpPassword"] = null;
                }
                catch(Exception ex) {
                    Response.Redirect("~/Signin?fail=true");
                }
            }
            else if(HttpContext.Current.Session["username"] != null
                && HttpContext.Current.Session["password"] != null) {
                try {
                    VFSes = VFSSession.ListVFSes((string) HttpContext.Current.Session["username"],
                        (string) HttpContext.Current.Session["password"]);
                }
                catch(Exception ex) {
                    retrieveView.Visible = false;
                    Master.errorText = "Could not connect to synchonization server";
                    return;
                }
            }
            else {
                Response.Redirect("~/Signin");
            }

            retrieveView.DataSource = VFSes;
            retrieveView.DataBind();
        }
    }
}