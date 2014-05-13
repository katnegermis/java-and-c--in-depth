﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core {
    public struct JCDSynchronizerReply {
        private string message;
        public string Message { get { return message; } }

        private SynchronizerStatusCode statusCode;
        public SynchronizerStatusCode StatusCode { get { return statusCode; } }

        private object data;
        public object Data { get { return data; } }

        public JCDSynchronizerReply(string msg, SynchronizerStatusCode status) {
            this.message = msg;
            this.statusCode = status;
            this.data = null;
        }

        public JCDSynchronizerReply(string msg, SynchronizerStatusCode status, object data) {
            this.message = msg;
            this.statusCode = status;
            this.data = data;
        }
    }

    public enum SynchronizerStatusCode {
        OK,
        FAILED,
    }

    public interface IJCDSynchronizedVFS : IJCDBasicVFS {
        JCDSynchronizerReply Register(string username, string password);

        JCDSynchronizerReply LogIn(string username, string password);

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        /// <summary>
        /// Retrieve a list of tuples of VFS ids and names.
        /// </summary>
        /// <returns>SynchronizerReply with Data set to a list of tuples of VFS ids and names.</returns>
        JCDSynchronizerReply ListVFSes();

        /// <summary>
        /// Add a VFS to the user account.
        /// SynchronizerReply.Data will be set to an int, the new id of the VFS.
        /// </summary>
        /// <param name="vfsName">Name of the VFS.</param>
        /// <param name="data">The data of the VFS</param>
        /// <returns>SynchronizerReply with Data being the id (int) of the VFS.</returns>
        JCDSynchronizerReply AddVFS(string vfsName, byte[] data);

        /// <summary>
        /// Delete a VFS from the user account.
        /// </summary>
        /// <param name="id">ID of the VFS to delete.</param>
        JCDSynchronizerReply DeleteVFS(int id);

        /// <summary>
        /// Retrieve an entire VFS.
        /// SynchronizerReply.Data will be set to a byte array which is the data of the full VFS.
        /// </summary>
        /// <param name="vfsId">ID of the vfs to retrieve. Can be found by calling ListVFSes</param>
        /// <returns>SynchronizerReply with Data set to a byte array which contains the full VFS.</returns>
        JCDSynchronizerReply RetrieveVFS(int vfsId);
    }
}
