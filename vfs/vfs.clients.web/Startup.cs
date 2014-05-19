using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(vfs.clients.web.Startup))]

namespace vfs.clients.web {
    public class Startup {
        //Bad static analyzer. This method has to be defined this way.
        public void Configuration(IAppBuilder app) {
            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888
            app.MapSignalR();
        }
    }
}
