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
            var tuple = Tuple.Create(0, "test");
            var lst = new List<Tuple<int, string>>();
            lst.Add(tuple);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED,
                                            new object[] { lst });
        }

        public JCDSynchronizerReply AddVFS(string vfsName, byte[] data) {
            Console.WriteLine("Client called AddVFS({0}, [data])", vfsName);
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED,
                                            new object[] { 1 });
        }

        public JCDSynchronizerReply DeleteVFS(int id) {
            Console.WriteLine("Client called DeleteVFS");
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveVFS(int vfsId) {
            Console.WriteLine("Client called RetrieveVFS");
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public void FileAdded(string path, byte[] data) {
            Console.WriteLine("Client called FileAdded");
        }

        public void FileDeleted(string path) {
            Console.WriteLine("Client called FileDeleted");
        }

        public void FileMoved(string oldPath, string newPath) {
            Console.WriteLine("Client called FileMoved");
        }

        public void FileModified(string path, long offset, byte[] data) {
            Console.WriteLine("Client called FileModified");
        }

        public void FileResized(string path, long newSize) {
            Console.WriteLine("Client called FileResized");
        }
    }
}
