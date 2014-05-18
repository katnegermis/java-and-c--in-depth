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

namespace vfs.synchronizer.client
{
    public class JCDVFSSynchronizer : IJCDBasicVFS, IJCDSynchronizedVFS
    {
        private const int NotSynchronizedId = 0;

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

        internal void OnFileAdded(string path, long size, bool isFolder) {
            if (FileAdded != null) {
                FileAdded(path, size, isFolder);
            }
        }

        internal void OnFileDeleted(string path) {
            if (FileDeleted != null) {
                FileDeleted(path);
            }
        }

        internal void OnFileMoved(string oldPath, string newPath) {
            if (FileMoved != null) {
                FileMoved(oldPath, newPath);
            }
        }

        internal void OnFileModified(string path, long startByte, byte[] data) {
            if (FileModified != null) {
                FileModified(path, startByte, data);
            }
        }

        internal void OnFileResized(string path, long newSize) {
            if (FileResized != null) {
                FileResized(path, newSize);
            }
        }
        
        internal void InformServerFileAdded(string path, long size, bool isFolder) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileAdded", path, size, isFolder);
        }

        internal void InformServerFileDeleted(string path) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileDeleted", path);
        }

        internal void InformServerFileMoved(string oldPath, string newPath) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileMoved", oldPath, newPath);
        }

        internal void InformServerFileModified(string path, long offset, byte[] data) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileModified", path, offset, data);
        }

        internal void InformServerFileResized(string path, long newSize) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileResized", path, newSize);
        }

        public void LogIn(string username, string password) {
            if (this.hubConn == null) {
                ConnectToHub(username, password);
            }

            var res = HubInvoke<JCDSynchronizerReply>("LogIn", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
        }

        public static void Register(string username, string password) {
            var conns = ConnectToHubStatic(null);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "Register", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
        }

        public static List<Tuple<long, string>>  ListVFSes(string username, string password) {
            var conns = ConnectToHubStatic(username, password);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "ListVFSes", username, password);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            return (List<Tuple<long, string>>)res.Data[0];
        }

        /// <summary>
        /// Start synchronizing the underlying VFS with the server.
        /// </summary>
        /// <returns></returns>
        public long AddVFS() {
            if (vfs.GetId() != NotSynchronizedId) {
                throw new AlreadySynchronizedVFSException("This VFS is already being synchronized!");
            }

            // Close VFS so that we can read its data.
            vfs.Close();
            //TODO: the whole VFS may not fit in memory
            var data = File.ReadAllBytes(hfsPath);
            var name = Path.GetFileName(hfsPath);
            vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Open", new object[] { hfsPath });            

            var res = HubInvoke<JCDSynchronizerReply>(this.hubProxy, "AddVFS", name, data);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            return (long)res.Data[0];
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

        public static JCDSynchronizerReply RetrieveVFS(string username, string password, int vfsId) {
            var conns = ConnectToHubStatic(username, password);
            var res = HubInvoke<JCDSynchronizerReply>(conns.Item2, "RetrieveVFS", vfsId);
            if (res.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new VFSSynchronizationServerException(res.Message);
            }
            return res;
        }

        public bool LoggedIn() {
            return this.hubConn != null;
        }

        public void LogOut() {
            var result = HubInvoke<JCDSynchronizerReply>("LogOut");
            this.hubConn.Stop();
            this.hubConn = null;
            if (result.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new Exception("Error logging out: " + result.Message);
            }
        }

        private JCDVFSSynchronizer(IJCDBasicVFS vfs, Type vfsType, string path) {
            this.vfs = vfs;
            this.vfsType = vfsType;
            this.hfsPath = path;

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

        /// <summary>
        /// Unmount a mounted VFS.
        /// </summary>
        // - No VFS mounted
        public void Close() {
            lock (this.vfs) {
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
            // TODO: It is going to be a problem that these events will be propagated to the
            // server again. We must do something to stop this from happening.

            // The point of these functions is that they should implement 
            // vfs.synchronizer.common.ISynchronizerClient.
            hub.On<string, bool>("FileAdded", (path, isFolder) => {
                Console.WriteLine("Server called FileAdded");
                if (isFolder) {
                    vfs.CreateDirectory(path, false);
                }
                else {
                    vfs.CreateFile(path, 0, false);
                }
            });

            hub.On<string, long, byte[]>("FileModified", (path, offset, data) => {
                Console.WriteLine("Server called FileModified");
                using(var stream = vfs.GetFileStream(path)) {
                    stream.Seek(offset, System.IO.SeekOrigin.Begin);
                    stream.Write(data, 0, 0);
                }
            });

            hub.On<string, long>("FileResized", (path, size) => {
                Console.WriteLine("Server called FileResized");
                using(var stream = vfs.GetFileStream(path)) {
                    stream.SetLength(size);
                }
            });

            hub.On<string>("FileDeleted", path => {
                Console.WriteLine("Server called FileDeleted");
                var details = vfs.GetFileDetails(path);
                vfs.DeleteFile(path, details.IsFolder);
            });

            hub.On<string, string>("FileMoved", (oldPath, newPath) => {
                Console.WriteLine("Server called FileMoved");
                vfs.MoveFile(oldPath, newPath);
            });
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
    }
}
