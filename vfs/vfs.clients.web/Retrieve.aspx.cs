using System;
using System.IO;
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

            statusText.Text = "";

            /*if(!Page.IsPostBack && VFSes.Count > 0) {
                GroupRadioButton g = (GroupRadioButton) retrieveView.Rows[0].FindControl("selectButton");
                //g.Checked = true;
            }*/
        }

        private Tuple<long, string> getSelected() {
            long id;
            try {
                id = long.Parse(Request.Form["VFSid"]);
            }
            catch(Exception ex) {
                //nothing selected
                return null;
            }
            foreach(GridViewRow r in retrieveView.Rows) {
                long rid = long.Parse(((HiddenField) r.FindControl("VFSidComp")).Value);
                if(id == rid) {
                    string name = ((Label) r.FindControl("VFSname")).Text;
                    return new Tuple<long, string>(id, name);
                }
            }
            return null;
        }

        public void download(object sender, EventArgs e) {
            Tuple<long, string> vfsMetaData = getSelected();
            if(vfsMetaData == null) {
                return;
            }

            Response.Clear();
            Response.ClearHeaders();
            Response.ClearContent();
            Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + vfsMetaData.Item2 + "\"");

            Tuple<long, byte[]> vfs = VFSSession.RetrieveVFS((string) HttpContext.Current.Session["username"],
                (string) HttpContext.Current.Session["password"], vfsMetaData.Item1);

            Response.AppendHeader("Content-Length", vfs.Item2.Length.ToString());
            Response.AppendHeader("Cache-Control", "private, max-age=0, no-cache");
            Response.ContentType = "application/octet-stream";
            Response.Flush();

            Response.OutputStream.Write(vfs.Item2, 0, vfs.Item2.Length);

            Response.End();
        }

        public void saveOnServer(object sender, EventArgs e) {
            if(serverPath.Text.Trim() == "") {
                Master.errorText = "Please enter a path to save the VFS in.";
                return;
            }

            if(serverPath.Text.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
                Master.errorText = "The entered path is invalid.";
                return;
            }

            Tuple<long, string> vfsMetaData = getSelected();
            if(vfsMetaData == null) {
                return;
            }

            Tuple<long, byte[]> vfs = VFSSession.RetrieveVFS((string) HttpContext.Current.Session["username"],
                (string) HttpContext.Current.Session["password"], vfsMetaData.Item1);

            if(File.Exists(serverPath.Text)) {
                Master.errorText = "There is already a file with that name!";
            }
            else {
                try {
                    File.WriteAllBytes(serverPath.Text, vfs.Item2);
                    statusText.Text = "Successfully saved the VFS on the server";
                }
                catch(Exception ex) {
                    Master.errorText = e.ToString();
                }
            }
        }
    }
}