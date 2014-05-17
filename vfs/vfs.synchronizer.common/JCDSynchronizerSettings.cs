using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.synchronizer.common
{
    public static class JCDSynchronizerSettings {
        public const string PublicAddress = "http://localhost:8080";
        public const string PublicLoginAddress = "http://localhost:8080/Account/Login";
        public const string HubName = "JCDVFSSynchronizerHub";
        public const string LoginCookieName = ".AspNet.Cookies";
    }
}
