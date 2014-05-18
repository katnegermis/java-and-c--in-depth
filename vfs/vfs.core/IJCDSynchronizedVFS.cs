using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core {
    public struct JCDSynchronizerReply {
        public string Message;

        public JCDSynchronizerStatusCode StatusCode;

        public object[] Data;

        public JCDSynchronizerReply(string msg, JCDSynchronizerStatusCode status) {
            this.Message = msg;
            this.StatusCode = status;
            this.Data = null;
        }

        public JCDSynchronizerReply(string msg, JCDSynchronizerStatusCode status, params object[] data) {
            this.Message = msg;
            this.StatusCode = status;
            this.Data = data;
        }
    }

    public enum JCDSynchronizerStatusCode {
        OK,
        FAILED,
    }

    public interface IJCDSynchronizedVFS : IJCDBasicVFS {

        /// <summary>
        /// Log in to the server.
        /// 
        /// </summary>
        /// <exception cref="FailedToLogIn">A FailedToLogIn exception will be thrown if logging in fails.</exception>
        /// <param name="username">Username to log in with</param>
        /// <param name="password">Password to log in with</param>
        void LogIn(string username, string password);

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        /// <summary>
        /// Add a VFS to the user account.
        /// </summary>
        /// <param name="vfsName">Name of the VFS.</param>
        /// <param name="data">The data of the VFS</param>
        /// <returns>Server assigned id of the VFS.</returns>
        long AddVFS();

        /// <summary>
        /// Delete a VFS from the user account.
        /// </summary>
        /// <param name="id">ID of the VFS to delete.</param>
        void RemoveVFS();

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
