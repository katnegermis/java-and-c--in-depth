using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

using vfs.core;
using vfs.core.indexing;

namespace vfs.clients.web {
    public class Global : HttpApplication {

        /// <summary>
        /// The current VFS Session with the mounted VFS.
        /// </summary>
        public static VFSSession vfsSession {
            get {
                return (VFSSession) HttpContext.Current.Session["vfs"];
            }
            set {
                HttpContext.Current.Session["vfs"] = value;
            }
        }

        public static void TerminateSession(string SessionID) {
            Updates.sessionToConnection.Remove(SessionID);

            if(HttpContext.Current != null && vfsSession != null) {
                try {
                    vfsSession.Close();
                }
                catch(Exception) {
                    //Response.Write(e.ToString());
                }
                vfsSession = null;
            }
        }

        void Application_Start(object sender, EventArgs e) {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        public void Session_OnEnd() {
            TerminateSession(Session.SessionID);
        }
    }
}