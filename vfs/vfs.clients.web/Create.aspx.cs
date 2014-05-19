using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace vfs.clients.web {
    public partial class Create : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            vfsPath.Focus();
            maxSize.Attributes.Add("type", "number");
            maxSize.Attributes.Add("min", "0");
            maxSize.Attributes.Add("step", "1");
            //maxSize.Attributes.Add("pattern", "\\d*");
        }

        public void createVFS(object sender, EventArgs e) {
            UInt64 mSize;
            try {
                mSize = UInt64.Parse(maxSize.Text);
            }
            catch(FormatException ex) {
                Master.errorText = ex.ToString();
                return;
            }
            if(String.IsNullOrWhiteSpace(vfsPath.Text)) {
                Master.errorText = "Please enter a path";
                return;
            }

            if(Global.vfsSession != null) {
                try {
                    Global.vfsSession.Close();
                }
                catch(Exception) {
                }
            }

            try {
                Global.vfsSession = VFSSession.CreateVFS(Session.SessionID, vfsPath.Text, mSize);
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