using System;
using vfs.core;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using vfs.synchronizer.common;

namespace vfs.synchronizer.server
{

    [HubName(JCDSynchronizerSettings.HubName)]
    public class JCDSynchronizerHub : Hub, IJCDSynchronizerServer
    {

        JCDSynchronizerDatabase db = new JCDSynchronizerDatabase();

        public JCDSynchronizerReply Register(string username, string password)
        {
            Console.WriteLine("Client called Register({0}, {1})", username, password);

            var userId = db.Register(username, password);

            //TODO add somewhere if logged in automatically

            if (userId > 0)
                return new JCDSynchronizerReply("Registered successfully", JCDSynchronizerStatusCode.OK);
            else
                return new JCDSynchronizerReply("Registration failed", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply LogIn(string username, string password)
        {
            Console.WriteLine("Client called LogIn({0}, {1})", username, password);

            var userId = db.Login(username, password);

            //TODO add somewhere

            if (userId > 0)
                return new JCDSynchronizerReply("Logged in successfully", JCDSynchronizerStatusCode.OK);
            else
                return new JCDSynchronizerReply("Login failed", JCDSynchronizerStatusCode.FAILED);
        }

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        public JCDSynchronizerReply LogOut()
        {
            Console.WriteLine("Client called LogOut()");

            //TODO remove somewhere

            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply ListVFSes()
        {
            Console.WriteLine("Client called ListVFSes");

            //TODO get the user id from somewhere
            var list = db.ListVFSes(12345L);

            if (list != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, list);
            else
                return new JCDSynchronizerReply("Fail", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply AddVFS(string vfsName, byte[] data)
        {
            Console.WriteLine("Client called AddVFS({0}, [data])", vfsName);

            var vfsId = db.AddVFS(vfsName, 12345L, data);

            if (vfsId != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(vfsId));
            else
                return new JCDSynchronizerReply("Fail", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply DeleteVFS(long vfsId)
        {
            Console.WriteLine("Client called DeleteVFS");

            if (db.DeleteVFS(vfsId))
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveVFS(long vfsId)
        {
            Console.WriteLine("Client called RetrieveVFS");

            var tuple = db.RetrieveVFS(vfsId);

            if (tuple != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, tuple);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveChanges(long vfsId, long lastVersionId)
        {
            Console.WriteLine("Client called RetrieveVFS");

            //TODO find a way to pack and serialize the changes
            var changes = db.RetrieveChanges(vfsId, lastVersionId);

            if (changes != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, changes);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileAdded(long vfsId, string path, byte[] data)
        {
            Console.WriteLine("Client called FileAdded");

            //TODO define event type
            var id = db.AddChange(1, vfsId, path, data);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileDeleted(long vfsId, string path)
        {
            Console.WriteLine("Client called FileDeleted");

            //TODO define event type
            var id = db.AddChange(2, vfsId, path, null);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileMoved(long vfsId, string oldPath, string newPath)
        {
            Console.WriteLine("Client called FileMoved");

            //TODO define event type
            //TODO find way to handle file move. Possible solutions: 1. Delete old and create new. 2. Just change path. 3. Use a deleted flag -> has to be created new
           // var id = db.AddChange(3, vfsId, oldPath, null);

            /*if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);*/
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileModified(long vfsId, string path, long offset, byte[] data)
        {
            Console.WriteLine("Client called FileModified");

            //TODO define event type
            //var id = db.AddChange(4, vfsId, path, new Tuple<long, byte[]>(offset, data)).ToString();

            /*if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);*/
            var currentId = 1;
            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED, currentId);
        }

        public JCDSynchronizerReply FileResized(long vfsId, string path, long newSize)
        {
            Console.WriteLine("Client called FileResized");

            //TODO define event type
            var id = db.AddChange(5, vfsId, path, BitConverter.GetBytes(newSize));

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }
    }
}
