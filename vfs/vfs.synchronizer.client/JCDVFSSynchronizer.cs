using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;
using Microsoft.AspNet.SignalR.Client;
using vfs.synchronizer.common;
using System.Reflection;
using vfs.exceptions;
using System.IO;
using System.Net;
using System.Web.Security;
using Newtonsoft.Json.Linq;
using System.Data.SQLite;

namespace vfs.synchronizer.client
{
    public class JCDVFSSynchronizer : IJCDBasicVFS, IJCDSynchronizedVFS
    {
        private const long NotSynchronizedId = -1;
        private string dbName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jcdOffline.db");

        // Path to the underlying VFS.
        private string hfsPath;

        private IJCDBasicVFS vfs;
        private Type vfsType;
        private HubConnection hubConn;
        private IHubProxy hubProxy;

        public event AddFileEventHandler FileAdded;
        public event DeleteFileEventHandler FileDeleted;
        public event MoveFileEventHandler FileMoved;
        public event ModifyFileEventHandler FileModified;
        public event ResizeFileEventHandler FileResized;

        private bool PropagateToServer = true;

        private SQLiteConnection offlineStorage = null;

        internal void createTableIfNotExisting() {
            try {
                using(var command = new SQLiteCommand(offlineStorage)) {
                    // Users
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Changes ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, vfsId INTEGER NOT NULL, eventType INTEGER NOT NULL, event BLOB NOT NULL );";
                    command.ExecuteNonQuery();
                }
            }
            catch(Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private void connectOfflineStorage() {
            if(offlineStorage == null) {
                offlineStorage = new SQLiteConnection();
                offlineStorage.ConnectionString = "Data Source=" + dbName;
                offlineStorage.Open();

                createTableIfNotExisting();
            }
        }

        private void disconnectOfflineStorage() {
            if(offlineStorage != null) {
                offlineStorage.Close();
                offlineStorage.Dispose();
                offlineStorage = null;
            }
        }

        private void storeEventOffline(JCDSynchronizationEventType type, byte[] serializedEvent) {
            using(var command = new SQLiteCommand(offlineStorage)) {
                command.CommandText = "INSERT INTO Changes (vfsId, eventType, event) VALUES(@vfsId, @eventType, @event);";
                command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfs.GetId();
                command.Parameters.Add("@eventType", System.Data.DbType.Int32).Value = (int) type;
                command.Parameters.Add("@event", System.Data.DbType.Binary).Value = serializedEvent;
                command.ExecuteNonQuery();
            }
        }

        private void commitOfflinesEvents() {
            List<long> eventIds = new List<long>();

            using(var command = new SQLiteCommand(offlineStorage)) {
                command.CommandText = "SELECT id, eventType, event FROM Changes WHERE vfsId = @vfsId;";
                command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfs.GetId();

                using(var reader = command.ExecuteReader()) {
                    string vfsPath;
                    string newPath;
                    long offset;
                    long newSize;
                    long size;
                    byte[] data;
                    bool isFolder;

                    int type;

                    while(reader.Read()) {
                        eventIds.Add(Convert.ToInt64(reader["id"]));

                        type = Convert.ToInt32(reader["eventType"]);
                        switch(type) {
                            case (int) JCDSynchronizationEventType.Added:
                                JCDSynchronizerSerialization.Deserialize<string, long, bool>(JCDSynchronizationEventType.Added, (byte[]) reader["event"], out vfsPath, out size, out isFolder);
                                InformServerFileAdded(vfsPath, size, isFolder);
                                break;
                            case (int) JCDSynchronizationEventType.Deleted:
                                JCDSynchronizerSerialization.Deserialize<string>(JCDSynchronizationEventType.Deleted, (byte[]) reader["event"], out vfsPath);
                                InformServerFileDeleted(vfsPath);
                                break;
                            case (int) JCDSynchronizationEventType.Moved:
                                JCDSynchronizerSerialization.Deserialize<string, string>(JCDSynchronizationEventType.Moved, (byte[]) reader["event"], out vfsPath, out newPath);
                                InformServerFileMoved(vfsPath, newPath);
                                break;
                            case (int) JCDSynchronizationEventType.Modified:
                                JCDSynchronizerSerialization.Deserialize<string, long, byte[]>(JCDSynchronizationEventType.Modified, (byte[]) reader["event"], out vfsPath, out offset, out data);
                                InformServerFileModified(vfsPath, offset, data);
                                break;
                            case (int) JCDSynchronizationEventType.Resized:
                                JCDSynchronizerSerialization.Deserialize<string, long>(JCDSynchronizationEventType.Resized, (byte[]) reader["event"], out vfsPath, out newSize);
                                InformServerFileResized(vfsPath, newSize);
                                break;
                            default:
                                Console.WriteLine(String.Format("Execution of a change of type {0} failed", type));
                                break;
                        }
                    }
                }
            }

            foreach(long id in eventIds) {
                using(var command = new SQLiteCommand(offlineStorage)) {
                    command.CommandText = "DELETE FROM Changes WHERE id = @id;";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = id;
                    command.ExecuteNonQuery();
                }
            }
        }

        internal void OnFileAdded(string path, long size, bool isFolder) {
            var handler = FileAdded;
            if (handler != null) {
                handler(path, size, isFolder);
            }
        }

        internal void OnFileDeleted(string path) {
            var handler = FileDeleted;
            if (handler != null) {
                handler(path);
            }
        }

        internal void OnFileMoved(string oldPath, string newPath) {
            var handler = FileMoved;
            if (handler != null) {
                handler(oldPath, newPath);
            }
        }


        internal void OnFileModified(string path, long offset, byte[] data) {
            var handler = FileModified;
            if (handler != null) {
                handler(path, offset, data);
            }
        }

        internal void OnFileResized(string path, long newSize) {
            var handler = FileResized;
            if (handler != null) {
                handler(path, newSize);
            }
        }
        
        internal void InformServerFileAdded(string path, long size, bool isFolder) {
            if(!IsSynchronized() || !PropagateToServer) {
                return;
            }

            if (!LoggedIn()) {
                byte[] logEvent = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Added,
                    path, size, isFolder);
                storeEventOffline(JCDSynchronizationEventType.Added, logEvent);
            }
            else {
                var reply = HubInvoke<JCDSynchronizerReply>("FileAdded", vfs.GetId(), path, size, isFolder);
                SetCurrentVersionId((long)reply.Data[0]);
            }
        }

        internal void InformServerFileDeleted(string path) {
            if(!IsSynchronized()) {
                return;
            }

            if(!LoggedIn()) {
                byte[] logEvent = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Deleted,
                    path);
                storeEventOffline(JCDSynchronizationEventType.Deleted, logEvent);
            }
            else {
                var reply = HubInvoke<JCDSynchronizerReply>("FileDeleted", vfs.GetId(), path);
                SetCurrentVersionId((long)reply.Data[0]);
            }
        }

        internal void InformServerFileMoved(string oldPath, string newPath) {
            if(!IsSynchronized()) {
                return;
            }

            if(!LoggedIn()) {
                byte[] logEvent = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Moved,
                    oldPath, newPath);
                storeEventOffline(JCDSynchronizationEventType.Moved, logEvent);
            }
            else {
                var reply = HubInvoke<JCDSynchronizerReply>("FileMoved", vfs.GetId(), oldPath, newPath);
                SetCurrentVersionId((long)reply.Data[0]);
            }
        }

        internal void InformServerFileModified(string path, long offset, byte[] data) {
            if(!IsSynchronized()) {
                return;
            }

            if(!LoggedIn()) {
                byte[] logEvent = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Modified,
                    path, offset, data);
                storeEventOffline(JCDSynchronizationEventType.Modified, logEvent);
            }
            else {
                var reply = HubInvoke<JCDSynchronizerReply>("FileModified", vfs.GetId(), path, offset, data);
                SetCurrentVersionId((long)reply.Data[0]);
            }
        }

        internal void InformServerFileResized(string path, long newSize) {
            if(!IsSynchronized()) {
                return;
            }

            if(!LoggedIn()) {
                byte[] logEvent = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Resized,
                    path, newSize);
                storeEventOffline(JCDSynchronizationEventType.Resized, logEvent);
            }
            else {
                var reply = HubInvoke<JCDSynchronizerReply>("FileResized", vfs.GetId(), path, newSize);
                SetCurrentVersionId((long)reply.Data[0]);
            }
        }

        public void LogIn(string username, string password) {
            if (this.hubConn == null) {
                ConnectToHub(username, password);
            }

            var res = HubInvoke<JCDSynchronizerReply>("LogIn", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }

            res = HubInvoke<JCDSynchronizerReply>("RetrieveChanges", vfs.GetId(), vfs.GetCurrentVersionId());
            if(res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }

            var jarr = (JObject) res.Data[0];
            // No changes since last log in.
            if(jarr != null) {
                var changes = jarr.ToObject<Tuple<long, List<Tuple<int, byte[]>>>>();
                if(changes != null && changes.Item2.Count > 0) {
                    lock(this.vfs) {
                        vfs.Close();
                        try {
                            JCDSynchronizerChangeExecutor.Execute(hfsPath, changes.Item2);
                        }
                        catch(FileAlreadyExistsException e) {
                            throw new VFSSynchronizationServerException("Failed to fetch files: " + e.Message, e);
                        }
                        catch(vfs.exceptions.FileNotFoundException e) {
                            throw new VFSSynchronizationServerException("Failed to fetch files: " + e.Message, e);
                        }
                        finally {
                            vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Open", new object[] { hfsPath });
                            SubscribeToEvents(vfs);
                            vfs.SetCurrentVersionId(changes.Item1);
                        }
                    }
                }

                commitOfflinesEvents();

                disconnectOfflineStorage();
            }
        }

        public static void Register(string username, string password) {
            var conns = ConnectToHubStatic(null);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "Register", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
        }

        public static List<Tuple<long, string>> ListVFSes(string username, string password) {
            var conns = ConnectToHubStatic(username, password);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "ListVFSes", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            var lst = (JArray)res.Data[0];
            return lst.ToObject<List<Tuple<long, string>>>();
        }

        /// <summary>
        /// Start synchronizing the underlying VFS with the server.
        /// </summary>
        /// <returns></returns>
        public long AddVFS() {
            if ( IsSynchronized()) {
                throw new AlreadySynchronizedVFSException("This VFS is already being synchronized!");
            }

            // Close VFS so that we can read its data.
            vfs.Close();
            //TODO: the whole VFS may not fit in memory
            var data = File.ReadAllBytes(hfsPath);
            var name = Path.GetFileName(hfsPath);
            vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Open", new object[] { hfsPath });
            SubscribeToEvents(vfs);

            var res = HubInvoke<JCDSynchronizerReply>(this.hubProxy, "AddVFS", name, data);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            var vfsId = (long)res.Data[0];
            vfs.SetId(vfsId);
            return vfsId;
        }


        /// <summary>
        /// Stop synchronizing the underlying VFS with the server.
        /// </summary>
        /// <returns></returns>
        public void RemoveVFS() {
            long vfsId = GetId();
            SetId(NotSynchronizedId);
            var res = HubInvoke<JCDSynchronizerReply>(this.hubProxy, "DeleteVFS", vfsId);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
        }

        public static Tuple<long, byte[]> RetrieveVFS(string username, string password, long vfsId) {
            var conns = ConnectToHubStatic(username, password);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "RetrieveVFS", vfsId);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            var data = (JObject)res.Data[0];
            return data.ToObject<Tuple<long, byte[]>>();
        }

        public bool LoggedIn() {
            return this.hubConn != null;
        }

        public void LogOut() {
            if (!LoggedIn()) {
                return;
            }
            var result = HubInvoke<JCDSynchronizerReply>("LogOut");
            this.hubConn.Stop();
            this.hubConn = null;
            if (result.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new Exception("Error logging out: " + result.Message);
            }

            connectOfflineStorage();
        }

        private JCDVFSSynchronizer(IJCDBasicVFS vfs, Type vfsType, string path) {
            this.vfs = vfs;
            this.vfsType = vfsType;
            this.hfsPath = path;

            connectOfflineStorage();
            SubscribeToEvents(vfs);
        }

        /// <summary>
        /// Create a new VFS-file.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the hard file system</param>
        /// <param name="size">The size of the new vfs</param>
        /// <returns>True if the vfs has been created successfully, false otherwise</returns>
        public static JCDVFSSynchronizer Create(Type vfsType, string hfsPath, ulong size) {
            var vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Create", new object[] { hfsPath, size });
            return new JCDVFSSynchronizer(vfs, vfsType, hfsPath);
        }

        /// <summary>
        /// Delete an unmounted VFS from HFS.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the host file system</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"
        public static void Delete(Type vfsType, string hfsPath) {
            IJCDBasicTypeCallStaticMethod(vfsType, "Delete", new object[] { hfsPath });
        }

        /// <summary>
        /// Mount an existing VFS-file.
        /// </summary>
        /// <param name="hfsPath"> The path of the file on the host file system</param>
        /// <returns>An object of an implementation of the IJCDBasicVFS interface</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        public static JCDVFSSynchronizer Open(Type vfsType, string hfsPath) {
            var vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Open", new object[] { hfsPath });
            return new JCDVFSSynchronizer(vfs, vfsType, hfsPath);

        }

        public void Dispose() {
            Close();
        }

        /// <summary>
        /// Unmount a mounted VFS.
        /// </summary>
        // - No VFS mounted
        public void Close() {
            lock (this.vfs) {
                LogOut();
                disconnectOfflineStorage();
                vfs.Close();
            }
        }

        /// <summary>
        /// Get total size of a mounted VFS.
        /// </summary>
        /// <returns>Size of mounted VFS.</returns>
        // - No VFS mounted
        public ulong Size() {
            lock (this.vfs) {
                return vfs.Size();
            }
        }

        /// <summary>
        /// Get total amount of occupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of occupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        // - No VFS mounted
        public ulong OccupiedSpace() {
            lock (this.vfs) {
                return vfs.OccupiedSpace();
            }
        }

        /// <summary>
        /// Get total amount of unoccupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of unoccupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        /// - No VFS mounted
        public ulong FreeSpace() {
            lock (this.vfs) {
                return vfs.FreeSpace();
            }
        }

        /// <summary>
        /// Create a directory on a mounted VFS.
        /// Optionally create parents (like mkdir -p).
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <param name="createParents"></param>
        // Exceptions:
        // - no such path (createParents == false).
        // - too little space available on VFS.
        // - invalid path string (file name too long/invalid characters).
        public void CreateDirectory(string vfsPath, bool createParents) {
            lock (this.vfs) {
                vfs.CreateDirectory(vfsPath, createParents);
            }
        }

        /// <summary>
        /// Create a file on a mounted VFS of the given size.
        /// Optionally create parents (like mkdir -p).
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <param name="createParents"></param>
        // Exceptions:
        // - no such path (createParents == false).
        // - too little space available on VFS.
        // - invalid path string (file name too long/invalid characters).
        public JCDFileStream CreateFile(string vfsPath, ulong size, bool createParents) {
            JCDFileStream stream;
            lock (this.vfs) {
                stream = vfs.CreateFile(vfsPath, size, createParents);
            }
            return stream;
        }

        /// <summary>
        /// Import a file or directory from HFS to a mounted VFS.
        /// </summary>
        // Exceptions:
        // - no such path on HFS.
        // - no such path on VFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        public void ImportFile(string hfsPath, string vfsPath) {
            lock (this.vfs) {
                vfs.ImportFile(hfsPath, vfsPath);
            }
        }

        /// <summary>
        /// Import a file or directory from a stream to a mounted VFS.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - invalid VFS path string (file name too long/invalid characters).
        public void ImportFile(Stream file, string vfsPath) {
            lock(this.vfs) {
                vfs.ImportFile(file, vfsPath);
            }
        }

        /// <summary>
        /// Export a file or directory from a mounted VFS to HFS.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - no such path on HFS.
        // - too little space available on HFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        public void ExportFile(string vfsPath, string hfsPath) {
            lock (this.vfs) {
                vfs.ExportFile(vfsPath, hfsPath);
            }
        }

        /// <summary>
        /// Export a file from a mounted VFS to a stream.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - the VFS path points to a directory
        // - invalid VFS path string (file name too long/invalid characters).
        public void ExportFile(string vfsPath, Stream output) {
            lock(this.vfs) {
                vfs.ExportFile(vfsPath, output);
            }
        }

        /// <summary>
        /// Delete a file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        // - path points to a directory (recursive == false).
        public void DeleteFile(string vfsPath, bool recursive) {
            lock (this.vfs) {
                vfs.DeleteFile(vfsPath, recursive);
            }
        }

        public void RenameFile(string vfsPath, string newName) {
            lock (this.vfs) {
                vfs.RenameFile(vfsPath, newName);
            }
        }

        public void MoveFile(string vfsPath, string newVfsPath) {
            lock (this.vfs) {
                vfs.MoveFile(vfsPath, newVfsPath);
            }
        }

        public void CopyFile(string vfsPath, string newVfsPath) {
            lock (this.vfs) {
                vfs.CopyFile(vfsPath, newVfsPath);
            }
        }

        public JCDDirEntry[] ListDirectory(string vfsPath) {
            lock (this.vfs) {
                return vfs.ListDirectory(vfsPath);
            }
        }

        public void SetCurrentDirectory(string vfsPath) {
            lock (this.vfs) {
                vfs.SetCurrentDirectory(vfsPath);
            }
        }

        public string GetCurrentDirectory() {
            lock (this.vfs) {
                return vfs.GetCurrentDirectory();
            }
        }

        public JCDFileStream GetFileStream(string vfsPath) {
            lock (this.vfs) {
                return vfs.GetFileStream(vfsPath);
            }
        }

        public string[] Search(string fileName, bool caseSensitive) {
            lock (this.vfs) {
                return vfs.Search(fileName, caseSensitive);
            }
        }

        public string[] Search(string searchDir, string fileName, bool caseSensitive, bool recursive) {
            lock (this.vfs) {
                return vfs.Search(searchDir, fileName, caseSensitive, recursive);
            }
        }

        public JCDDirEntry GetFileDetails(string path) {
            lock (this.vfs) {
                return vfs.GetFileDetails(path);
            }
        }

        public void SetId(long id) {
            vfs.SetId(id);
        }

        public long GetId() {
            return vfs.GetId();
        }

        public bool IsSynchronized() {
            return vfs.GetId() != NotSynchronizedId;
        }

        internal static object IJCDBasicTypeCallStaticMethod(Type type, string methodName, object[] args) {
            if (type.GetInterface("IJCDBasicVFS") == null) {
                throw new Exception("Can only be used with objects that implement IJCDBasicVFS");
            }

            var method = type.GetMethod(methodName);
            return (IJCDBasicVFS)method.Invoke(null, args);
        }

        private void ConnectToHub(string username, string password) {
            var conns = ConnectToHubStatic(username, password);
            this.hubConn = conns.Item1;
            this.hubProxy = conns.Item2;
            SetHubEvents(this.hubProxy);
        }

        private static Tuple<HubConnection, IHubProxy> ConnectToHubStatic(string username, string password) {
            var authCookie = AuthenticateUser(username, password);
            return ConnectToHubStatic(authCookie);
        }

        private static Tuple<HubConnection, IHubProxy> ConnectToHubStatic(Cookie authCookie) {
            var hubConn = new HubConnection(JCDSynchronizerSettings.PublicAddress);
            if (authCookie != null) {
                hubConn.CookieContainer = new CookieContainer();
                hubConn.CookieContainer.Add(authCookie);
            }
            var hubProxy = hubConn.CreateHubProxy(JCDSynchronizerSettings.HubName);
            try {
                hubConn.Start().Wait();
            }
            catch (AggregateException e) {
                throw new VFSSynchronizationServerException("Error in communication with server: " + e.Message, e);
            }
            return Tuple.Create(hubConn, hubProxy);
        }

        private static Cookie AuthenticateUser(string user, string password) {
            Cookie authCookie;
            var request = WebRequest.Create(JCDSynchronizerSettings.PublicLoginAddress) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();

            var authCredentials = "UserName=" + user + "&Password=" + password;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(authCredentials);
            request.ContentLength = bytes.Length;
            try {
                using (var requestStream = request.GetRequestStream()) {
                    requestStream.Write(bytes, 0, bytes.Length);
                }
            }
            catch {
                throw new VFSSynchronizationServerException("Couldn't connect to server!");
            }

            HttpWebResponse response = null;
            try {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e) {
                throw new VFSSynchronizationServerException(e.Message);
            }

            authCookie = response.Cookies[JCDSynchronizerSettings.LoginCookieName];
            if (authCookie == null) {
                throw new VFSSynchronizationServerException("Incorrect username and/or password");
            }

            return authCookie;
        }

        private void SetHubEvents(IHubProxy hub) {
            // The point of these functions is that they should implement 
            // vfs.synchronizer.common.ISynchronizerClient.
            hub.On<long, string, long, bool>("FileAdded", (versionId, path, size, isFolder) => {
                Console.WriteLine("Server called FileAdded");
                // Current conflict resolution tactic: throw away changes previous
                // to current newest change.
                if (versionId <= GetCurrentVersionId()) {
                    return;
                }
                PropagateToServer = false;
                if (isFolder) {
                    vfs.CreateDirectory(path, false);
                }
                else {
                    vfs.CreateFile(path, (ulong)size, false);
                }
                PropagateToServer = true;
            });

            hub.On<long, string, long, byte[]>("FileModified", (versionId, path, offset, data) => {
                Console.WriteLine("Server called FileModified");
                // Current conflict resolution tactic: throw away changes previous
                // to current newest change.
                if (versionId <= GetCurrentVersionId()) {
                    return;
                }
                PropagateToServer = false;
                using(var stream = vfs.GetFileStream(path)) {
                    stream.Seek(offset, System.IO.SeekOrigin.Begin);
                    stream.Write(data, 0, data.Length);
                }
                PropagateToServer = true;
            });

            hub.On<long, string, long>("FileResized", (versionId, path, size) => {
                Console.WriteLine("Server called FileResized");
                // Current conflict resolution tactic: throw away changes previous
                // to current newest change.
                if (versionId <= GetCurrentVersionId()) {
                    return;
                }
                PropagateToServer = false;
                using(var stream = vfs.GetFileStream(path)) {
                    stream.SetLength(size);
                }
                PropagateToServer = true;
            });

            hub.On<long, string>("FileDeleted", (versionId, path) => {
                Console.WriteLine("Server called FileDeleted");
                // Current conflict resolution tactic: throw away changes previous
                // to current newest change.
                if (versionId <= GetCurrentVersionId()) {
                    return;
                }
                PropagateToServer = false;
                var details = vfs.GetFileDetails(path);
                vfs.DeleteFile(path, details.IsFolder);
                PropagateToServer = true;
            });

            hub.On<long, string, string>("FileMoved", (versionId, oldPath, newPath) => {
                Console.WriteLine("Server called FileMoved");
                // Current conflict resolution tactic: throw away changes previous
                // to current newest change.
                if (versionId <= GetCurrentVersionId()) {
                    return;
                }
                PropagateToServer = false;
                vfs.MoveFile(oldPath, newPath);
                PropagateToServer = true;
            });
        }

        private void SubscribeToEvents(IJCDBasicVFS vfs) {
            // Subscribe to events with functions that propagate vfs events to subscribers
            // of this class.
            ((JCDFAT)vfs).FileModified += OnFileModified;
            ((JCDFAT)vfs).FileAdded += OnFileAdded;
            ((JCDFAT)vfs).FileDeleted += OnFileDeleted;
            ((JCDFAT)vfs).FileMoved += OnFileMoved;
            ((JCDFAT)vfs).FileResized += OnFileResized;

            // Subscribe to vfs events
            ((JCDFAT)vfs).FileModified += InformServerFileModified;
            ((JCDFAT)vfs).FileAdded += InformServerFileAdded;
            ((JCDFAT)vfs).FileDeleted += InformServerFileDeleted;
            ((JCDFAT)vfs).FileMoved += InformServerFileMoved;
            ((JCDFAT)vfs).FileResized += InformServerFileResized;
        }
        
        private T HubInvoke<T>(string methodName, params object[] args) {
            return HubInvoke<T>(this.hubProxy, methodName, args);
        }

        private static T HubInvoke<T>(IHubProxy hubProxy, string methodName, params object[] args) {
            try {
                return hubProxy.Invoke<T>(methodName, args).Result;
            }
            catch (AggregateException e) {
                throw new VFSSynchronizationServerException(e.ToString());
            }
        }

        public void SetCurrentVersionId(long versionId) {
            vfs.SetCurrentVersionId(versionId);
        }

        public long GetCurrentVersionId() {
            return vfs.GetCurrentVersionId();
        }
    }
}
