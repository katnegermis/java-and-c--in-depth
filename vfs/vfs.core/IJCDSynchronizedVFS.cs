using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core {
    public interface IJCDSynchronizedVFS : IJCDBasicVFS {
        JCDSynchronizationMessage LogIn(string username, string password);
        void LogOut();
        bool LoggedIn();
    }

    public class JCDSynchronizationMessage {
        private string message;
        public string Message { get { return message; } }

        private int statusCode;
        public int StatusCode { get { return statusCode; } }

        private byte[] data;
        public byte[] Data { get { return data; } }

        private int messageType;
        public int MessageType { get { return messageType; } }

        public JCDSynchronizationMessage(int statusCode, string message) {
            this.message = message;
            this.statusCode = statusCode;
            this.data = null;
            this.messageType = 0;
        }
    }
}
