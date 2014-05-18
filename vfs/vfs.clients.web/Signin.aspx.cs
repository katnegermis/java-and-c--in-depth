using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Signin : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {

        }

        public void SignIn(object sender, EventArgs e) {
            if(username.Text.Trim() == "" || password.Text == "") {
                Master.errorText = "Please enter a username and a password";
                return;
            }

            try {
                Global.vfsSession.LogIn(username.Text, password.Text);
                proceed();
            }
            catch(Exception ex) {
                //TODO: it could be a connection error
                Master.errorText = "Wrong username or password";
            }
        }

        public void CreateAccount(object sender, EventArgs e) {
            if(username.Text.Trim() == "" || password.Text == "") {
                Master.errorText = "Please enter a username and a password";
                return;
            }

            try {
                Global.vfsSession.LogIn(username.Text, password.Text);
                proceed();
            }
            catch(Exception ex) {
                //TODO: it could be a connection error
                Master.errorText = "Wrong username or password";
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