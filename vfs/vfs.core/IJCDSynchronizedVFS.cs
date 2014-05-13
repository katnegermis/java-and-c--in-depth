using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core {
    public struct JCDSynchronizerReply {
        private string message;
        public string Message { get { return message; } }

        private JCDSynchronizerStatusCode statusCode;
        public JCDSynchronizerStatusCode StatusCode { get { return statusCode; } }

        private object[] data;
        public object[] Data { get { return data; } }

        public JCDSynchronizerReply(string msg, JCDSynchronizerStatusCode status) {
            this.message = msg;
            this.statusCode = status;
            this.data = null;
        }

        public JCDSynchronizerReply(string msg, JCDSynchronizerStatusCode status, object[] data) {
            this.message = msg;
            this.statusCode = status;
            this.data = data;
        }
    }

    public enum JCDSynchronizerStatusCode {
        OK,
        FAILED,
    }

    public interface IJCDSynchronizedVFS : IJCDBasicVFS {
        JCDSynchronizerReply LogIn(string username, string password);

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        /// <summary>
        /// Add a VFS to the user account.
        /// SynchronizerReply.Data will be set to an int, the new id of the VFS.
        /// </summary>
        /// <param name="vfsName">Name of the VFS.</param>
        /// <param name="data">The data of the VFS</param>
        /// <returns>SynchronizerReply with Data being the id (int) of the VFS.</returns>
        JCDSynchronizerReply AddVFS();

        /// <summary>
        /// Delete a VFS from the user account.
        /// </summary>
        /// <param name="id">ID of the VFS to delete.</param>
        JCDSynchronizerReply RemoveVFS();

        /*/// <summary>
        /// Retrieve an entire VFS.
        /// SynchronizerReply.Data will be set to a byte array which is the data of the full VFS.
        /// </summary>
        /// <param name="vfsId">ID of the vfs to retrieve. Can be found by calling ListVFSes</param>
        /// <returns>SynchronizerReply with Data set to a byte array which contains the full VFS.</returns>
        JCDSynchronizerReply RetrieveVFS(int vfsId);

        /// <summary>
        /// Retrieve a list of tuples of VFS ids and names.
        /// </summary>
        /// <returns>SynchronizerReply with Data set to a list of tuples of VFS ids and names.</returns>
        JCDSynchronizerReply ListVFSes();
         * 
        JCDSynchronizerReply Register(string username, string password); 
         * */
    }
}
