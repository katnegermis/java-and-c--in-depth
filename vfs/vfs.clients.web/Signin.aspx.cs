using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Signin : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if(Request.QueryString["fail"] == "true") {
                Master.errorText = "Wrong username or password";

                //http://stackoverflow.com/questions/853899/is-there-a-way-to-clear-query-string-parameters-when-posting-back
                foreach(Control ctrl in Page.Controls) {
                    if(ctrl is Button) {
                        ((Button) ctrl).PostBackUrl = Request.ServerVariables["URL"];
                    }
                }
            }
        }

        public void SignIn(object sender, EventArgs e) {
            if(username.Text.Trim() == "" || password.Text == "") {
                Master.errorText = "Please enter a username and a password";
                return;
            }

            if(Global.vfsSession != null) {
                try {
                    Global.vfsSession.LogIn(username.Text, password.Text);
                    proceed();
                }
                catch(Exception ex) {
                    //TODO: it could be a connection error
                    Master.errorText = "Wrong username or password";
                }
            }
            else {
                HttpContext.Current.Session["tmpUsername"] = username.Text;
                HttpContext.Current.Session["tmpPassword"] = password.Text;
                Response.Redirect("~/Retrieve");
            }
        }

        public void CreateAccount(object sender, EventArgs e) {
            if(username.Text.Trim() == "" || password.Text == "") {
                Master.errorText = "Please enter a username and a password";
                return;
            }

            try {
                VFSSession.CreateAccount(username.Text, password.Text);
                if(Global.vfsSession != null) {
                    Global.vfsSession.LogIn(username.Text, password.Text);
                }
                proceed();
            }
            catch(Exception ex) {
                Master.errorText = "Could not connect to synchonization server";
            }
        }


        private void proceed() {
            HttpContext.Current.Session["username"] = username.Text;
            HttpContext.Current.Session["password"] = password.Text;

            Master.checkSession();

            Response.Redirect("~/");
        }
    }
}