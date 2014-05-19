using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace vfs.clients.web {
    public class Updates : Hub {
        //The static analyzer should learn a thing or two about the lives of SignalR users. They don't have it easy.
        public static Dictionary<string, string> sessionToConnection = new Dictionary<string, string>();
        //public static Dictionary<string, string> connectionToSession = new Dictionary<string, string>();

        public override Task OnConnected() {
            sessionToConnection[Context.QueryString["session"]] = Context.ConnectionId;
            //connectionToSession[Context.ConnectionId] = Context.QueryString["session"];
            return base.OnConnected();
        }

        //Now handled in Global.Session_OnEnd()
        /*public override Task OnDisconnected() {
            sessionToConnection.Remove(connectionToSession[Context.ConnectionId]);
            connectionToSession.Remove(Context.ConnectionId);
            return base.OnDisconnected();
        }*/

        //We can't actually get a reference to this object, it seems, so there's no point in having this method here...
        /*public void Update(string sessionId) {
            Clients.Client(sessionToConnection[sessionId]).update();
        }*/
    }
}