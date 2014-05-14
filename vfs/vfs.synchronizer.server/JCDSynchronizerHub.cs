using System;
using vfs.core;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using vfs.synchronizer.common;

namespace vfs.synchronizer.server {

    [HubName(JCDSynchronizerSettings.HubName)]
    public class JCDSynchronizerHub : Hub, IJCDSynchronizerServer {

        public JCDSynchronizerReply Register(string username, string password) {
            Console.WriteLine("Client called Register({0}, {1})", username, password);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply LogIn(string username, string password) {
            Console.WriteLine("Client called LogIn({0}, {1})", username, password);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        public JCDSynchronizerReply LogOut() {
            Console.WriteLine("Client called LogOut()");
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply ListVFSes() {
            Console.WriteLine("Client called ListVFSes");
            var tuple = Tuple.Create(1, 1);
            var lst = new List<Tuple<int, int>>();
            lst.Add(tuple);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, lst);
        }

        public JCDSynchronizerReply AddVFS(string vfsName, byte[] data) {
            Console.WriteLine("Client called AddVFS({0}, [data])", vfsName);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, 1);
        }

        public JCDSynchronizerReply DeleteVFS(int id) {
            Console.WriteLine("Client called DeleteVFS");
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveVFS(int vfsId) {
            Console.WriteLine("Client called RetrieveVFS");
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileAdded(string path, byte[] data) {
            Console.WriteLine("Client called FileAdded");
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileDeleted(string path) {
            Console.WriteLine("Client called FileDeleted");
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileMoved(string oldPath, string newPath) {
            Console.WriteLine("Client called FileMoved");
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileModified(string path, long offset, byte[] data) {
            Console.WriteLine("Client called FileModified");
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileResized(string path, long newSize) {
            Console.WriteLine("Client called FileResized");
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }
    }
}
