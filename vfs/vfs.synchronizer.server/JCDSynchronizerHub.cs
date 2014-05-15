using System;
using vfs.core;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using vfs.synchronizer.common;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace vfs.synchronizer.server
{

    [HubName(JCDSynchronizerSettings.HubName)]
    public class JCDSynchronizerHub : Hub, IJCDSynchronizerServer
    {
        // Dictionary for storing user ids.
        // Usable to identify a user across connection sessions.
        private static readonly ConcurrentDictionary<string, User> users = new ConcurrentDictionary<string, User>();

        private const long notLoggedInId = -1;

        private class User {
            public string Name;
            public HashSet<string> ConnectionIds;
            public long Id = notLoggedInId;
        }

        JCDSynchronizerDatabase db = new JCDSynchronizerDatabase();

        /// <summary>
        /// Called every time a user connects. We want to store user's session across
        /// TCP sessions. This method lets us just that.
        /// 
        /// This method was found on 
        /// http://stackoverflow.com/questions/20520874/signalr-how-to-survive-accidental-page-refresh/20521466#20521466
        /// </summary>
        public override Task OnConnected() {
            // This line doesn't work because Context.User isn't set, because
            // signalr Authentication is not used.
            // See http://www.asp.net/signalr/overview/signalr-20/hubs-api/mapping-users-to-connections#inmemory
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;

            var user = users.GetOrAdd(username, _ => new User {
                Name = username,
                ConnectionIds = new HashSet<string>(),
            });

            lock (user.ConnectionIds) {
                user.ConnectionIds.Add(connectionId);
            }

            return base.OnConnected();
        }

        public JCDSynchronizerReply Register(string username, string password)
        {
            Console.WriteLine("Client called Register({0}, {1})", username, password);

            var userId = db.Register(username, password);

            //TODO add somewhere if logged in automatically
            // LogIn(Context, userId);

            if (userId > 0)
                return new JCDSynchronizerReply("Registered successfully", JCDSynchronizerStatusCode.OK);
            else
                return new JCDSynchronizerReply("Registration failed", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply LogIn(string username, string password)
        {
            Console.WriteLine("Client called LogIn({0}, {1})", username, password);

            if (UserIsLoggedIn(Context)) {
                return new JCDSynchronizerReply("You are already logged in!", JCDSynchronizerStatusCode.FAILED);
            }

            var userId = 500L;
            //var userId = db.Login(username, password);

            if (userId > 0) {
                return LogIn(Context, userId);
            }
            else {
                return new JCDSynchronizerReply("Login failed", JCDSynchronizerStatusCode.FAILED);
            }
        }

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        public JCDSynchronizerReply LogOut()
        {
            Console.WriteLine("Client called LogOut()");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }

            var user = GetUserFromContext(Context);
            lock (user) {
                user.Id = notLoggedInId;
            }

            return new JCDSynchronizerReply("NOT YET IMPLEMENTED", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply ListVFSes()
        {
            Console.WriteLine("Client called ListVFSes");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var userId = GetUserIdFromContext(Context);
            var list = db.ListVFSes(userId);

            if (list != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, list);
            else
                return new JCDSynchronizerReply("Fail", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply AddVFS(string vfsName, byte[] data)
        {
            Console.WriteLine("Client called AddVFS({0}, [data])", vfsName);

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var userId = GetUserIdFromContext(Context);

            var vfsId = db.AddVFS(vfsName, userId, data);

            if (vfsId != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(vfsId));
            else
                return new JCDSynchronizerReply("Fail", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply DeleteVFS(long vfsId)
        {
            Console.WriteLine("Client called DeleteVFS");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }

            if (db.DeleteVFS(vfsId))
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveVFS(long vfsId)
        {
            Console.WriteLine("Client called RetrieveVFS");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }

            var tuple = db.RetrieveVFS(vfsId);

            if (tuple != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, tuple);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply RetrieveChanges(long vfsId, long lastVersionId)
        {
            Console.WriteLine("Client called RetrieveVFS");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }

            var changes = db.RetrieveChanges(vfsId, lastVersionId);

            if (changes != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, changes);
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileAdded(long vfsId, string path, long size, bool isFolder)
        {
            Console.WriteLine("Client called FileAdded");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var id = db.AddFile(vfsId, path, size, isFolder);
            
            // Inform other clients.
            var user = GetUserFromContext(Context);
            SendToAllConnectedUsers(user, JCDSynchronizationEventType.Added, path, size, isFolder);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileDeleted(long vfsId, string path)
        {
            Console.WriteLine("Client called FileDeleted");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var id = db.DeleteFile(vfsId, path);

            // Inform other clients.
            var user = GetUserFromContext(Context);
            SendToAllConnectedUsers(user, JCDSynchronizationEventType.Deleted, path);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileMoved(long vfsId, string oldPath, string newPath)
        {
            Console.WriteLine("Client called FileMoved");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var id = db.MoveFile(vfsId, oldPath, newPath);
            // Inform other clients.
            var user = GetUserFromContext(Context);
            SendToAllConnectedUsers(user, JCDSynchronizationEventType.Moved, oldPath, newPath);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileModified(long vfsId, string path, long offset, byte[] data)
        {
            Console.WriteLine("Client called FileModified");

            if (!UserIsLoggedIn(Context)) {
                return NotLoggedInMessage();
            }
            var id = db.ModifyFile(vfsId, path, offset, data);

            // Inform other clients.
            var user = GetUserFromContext(Context);
            SendToAllConnectedUsers(user, JCDSynchronizationEventType.Modified, path, offset, data);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        public JCDSynchronizerReply FileResized(long vfsId, string path, long newSize)
        {
            Console.WriteLine("Client called FileResized");

            if (!UserIsLoggedIn(Context)) {
                return new JCDSynchronizerReply("You are not logged in!", JCDSynchronizerStatusCode.FAILED);
            }
            var id = db.ResizeFile(vfsId, path, newSize);

            // Inform other clients.
            var user = GetUserFromContext(Context);
            SendToAllConnectedUsers(user, JCDSynchronizationEventType.Resized, path, newSize);

            if (id != null)
                return new JCDSynchronizerReply("OK", JCDSynchronizerStatusCode.OK, Convert.ToUInt64(id));
            else
                return new JCDSynchronizerReply("FAIL", JCDSynchronizerStatusCode.FAILED);
        }

        private User GetUserFromContext(HubCallerContext context) {
            string username = context.User.Identity.Name;
            User user;
            if (users.TryGetValue(username, out user)) {
                return user;
            }
            return null;
        }

        private JCDSynchronizerReply LogIn(HubCallerContext context, long userId) {
            var user = GetUserFromContext(context);
            lock (user) {
                user.Id = userId;
            }
            return new JCDSynchronizerReply("Logged in successfully", JCDSynchronizerStatusCode.OK);
        }

        private JCDSynchronizerReply NotLoggedInMessage() {
            return new JCDSynchronizerReply("You are not logged in!", JCDSynchronizerStatusCode.FAILED);
        }

        private bool UserIsLoggedIn(HubCallerContext context) {
            return GetUserFromContext(context).Id != notLoggedInId;
        }

        private long GetUserIdFromContext(HubCallerContext context) {
            return GetUserFromContext(context).Id;
        }

        private void SendToAllConnectedUsers(User user, JCDSynchronizationEventType type, params object[] args) {
            lock (user) {
                foreach (string userId in user.ConnectionIds) {
                    SendMessage(userId, type, args);
                }
            }
        }

        private void SendMessage(string userId, JCDSynchronizationEventType type, params object[] args) {
            switch (type) {
                case JCDSynchronizationEventType.Added:
                    ClientFileAdded(userId, args);
                    break;
                case JCDSynchronizationEventType.Deleted:
                    ClientFileDeleted(userId, args);
                    break;
                case JCDSynchronizationEventType.Moved:
                    ClientFileMoved(userId, args);
                    break;
                case JCDSynchronizationEventType.Modified:
                    ClientFileModified(userId, args);
                    break;
                case JCDSynchronizationEventType.Resized:
                    ClientFileResized(userId, args);
                    break;
                default:
                    Console.WriteLine(String.Format("Execution of a change of type {0} failed", type));
                    break;
            }
        }

        private void ClientFileAdded(string userId, object[] args) {
            var path = (string)args[0];
            var size = (long)args[1];
            var isFolder = (bool)args[2];
            Clients.User(userId).FileAdded(path, size, isFolder);
        }

        private void ClientFileModified(string userId, object[] args) {
            string path = (string)args[0];
            long offset = (long)args[1];
            byte[] data = (byte[])args[2];
            Clients.User(userId).FileModified(path, offset, data);
        }

        private void ClientFileResized(string userId, object[] args) {
            string path = (string)args[0];
            long newSize = (long)args[1];
            Clients.User(userId).FileResized(path, newSize);
        }

        private void ClientFileMoved(string userId, object[] args) {
            string oldPath = (string)args[0];
            string newPath = (string)args[1];
            Clients.User(userId).FileMoved(oldPath, newPath);
        }

        private void ClientFileDeleted(string userId, object[] args) {
            string path = (string)args[0];
            Clients.User(userId).FileDeleted(path);
        }
    }
}
