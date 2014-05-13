using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;
using vfs.synchronizer.server;
using Microsoft.AspNet.SignalR.Client;
using vfs.synchronizer.common;

namespace vfs.synchronizer.client
{
    public class JCDVFSSynchronizer : IJCDBasicVFS, IJCDSynchronizedVFS
    {
        private IJCDBasicVFS vfs;
        // Temporary field representing a connection.
        private HubConnection hubConn;
        private IHubProxy hubProxy;

        public event AddFileEventHandler FileAdded;
        public event DeleteFileEventHandler FileDeleted;
        public event MoveFileEventHandler FileMoved;
        public event ModifyFileEventHandler FileModified;
        public event ResizeFileEventHandler FileResized;

        /// <summary>
        /// Event to be called every time a new file is added to the file system.
        /// </summary>
        /// <param name="path">Path of the newly added file.</param>
        internal void OnFileAdded(string path) {
            if (FileAdded != null) {
                FileAdded(path);
            }
        }

        /// <summary>
        /// Event to be called every time a file is deleted from the file system.
        /// </summary>
        /// <param name="path">Path of the deleted file.</param>
        internal void OnFileDeleted(string path) {
            if (FileDeleted != null) {
                FileDeleted(path);
            }
        }

        /// <summary>
        /// Event to be called every time a file is moved or renamed on the file system.
        /// </summary>
        /// <param name="oldPath">File's previous (old) path.</param>
        /// <param name="newPath">File's new (current) path.</param>
        internal void OnFileMoved(string oldPath, string newPath) {
            if (FileMoved != null) {
                FileMoved(oldPath, newPath);
            }
        }

        /// <summary>
        /// Event to be called every time a file is modified.
        /// This does NOT include file resizing!
        /// </summary>
        /// <param name="path">File's path.</param>
        /// <param name="offset">Offset in to file where the data was written.</param>
        /// <param name="data">Data that was written.</param>
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
        
        internal void InformServerFileAdded(string path, byte[] data) {
            if (!(LoggedIn())) {
                // Log to disk
                return;
            }
            var reply = HubInvoke<JCDSynchronizerReply>("FileAdded", path, data);
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

        public JCDSynchronizerReply LogIn(string username, string password) {
            // Implement properly.
            if (this.hubConn == null) {
                ConnectToHub();
            }
            return HubInvoke<JCDSynchronizerReply>("LogIn", username, password);
        }

        public static JCDSynchronizerReply Register(string username, string password) {
            // Implement properly.
            var conns = ConnectToHubStatic();
            return HubInvoke<JCDSynchronizerReply>(conns.Item2, "Register", username, password);
        }

        public static JCDSynchronizerReply ListVFSes(string username, string password) {
            var conns = ConnectToHubStatic();
            return HubInvoke<JCDSynchronizerReply>(conns.Item2, "ListVFSes", username, password);
        }

        /// <summary>
        /// Start synchronizing the underlying VFS with the server.
        /// </summary>
        /// <returns></returns>
        public JCDSynchronizerReply AddVFS() {
            // We should probably use the hfsPath instead of vfsName here, since we don't track
            // vfs names in this file.
            byte[] data = new byte[1]; // Get vfs data.
            return HubInvoke<JCDSynchronizerReply>(this.hubProxy, "AddVFS", data);
        }


        /// <summary>
        /// Stop synchronizing the underlying VFS with the server.
        /// </summary>
        /// <returns></returns>
        public JCDSynchronizerReply RemoveVFS() {
            // TODO: Implement properly.
            int vfsId = 0; // Get real VFS id, this.vfs.GetId();
            var conns = ConnectToHubStatic();
            return HubInvoke<JCDSynchronizerReply>(this.hubProxy, "DeleteVFS", vfsId);
        }

        public JCDSynchronizerReply RetrieveVFS(int vfsId) {
            return HubInvoke<JCDSynchronizerReply>(this.hubProxy, "RetrieveVFS", vfsId);
        }

        public bool LoggedIn() {
            // Implement properly.
            return this.hubConn != null;
        }

        public void LogOut() {
            var result = HubInvoke<JCDSynchronizerReply>("LogOut");
            if (result.StatusCode != JCDSynchronizerStatusCode.OK) {
                throw new Exception("Error logging out: " + result.Message);
            }
        }

        private JCDVFSSynchronizer(IJCDBasicVFS vfs) {
            this.vfs = vfs;

            // Subscribe to events with functions that propagate vfs events to subscribers
            // of this class.
            vfs.FileModified += OnFileModified;
            vfs.FileAdded += OnFileAdded;
            vfs.FileDeleted += OnFileDeleted;
            vfs.FileMoved += OnFileMoved;
            vfs.FileResized += OnFileResized;

            // Subscribe to vfs events
            //vfs.FileModified += InformServerFileModified;
            //vfs.FileAdded += InformServerFileAdded;
            //vfs.FileDeleted += InformServerFileDeleted;
            //vfs.FileMoved += InformServerFileMoved;
            //vfs.FileResized += InformServerFileResized;
        }

        /// <summary>
        /// Create a new VFS-file.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the hard file system</param>
        /// <param name="size">The size of the new vfs</param>
        /// <returns>True if the vfs has been created successfully, false otherwise</returns>
        public static JCDVFSSynchronizer Create(Type vfsType, string hfsPath, ulong size) {
            var vfs = (IJCDBasicVFS)IJCDBasicTypeCallStaticMethod(vfsType, "Create", new object[] { hfsPath, size });
            return new JCDVFSSynchronizer(vfs);
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
            return new JCDVFSSynchronizer(vfs);

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
        public void CreateFile(string vfsPath, ulong size, bool createParents) {
            lock (this.vfs) {
                vfs.CreateFile(vfsPath, size, createParents);
            }
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

        ///<summary>
        /// Rename file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void RenameFile(string vfsPath, string newName) {
            lock (this.vfs) {
                vfs.RenameFile(vfsPath, newName);
            }
        }

        /// <summary>
        /// Move file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void MoveFile(string vfsPath, string newVfsPath) {
            lock (this.vfs) {
                vfs.MoveFile(vfsPath, newVfsPath);
            }
        }

        /// <summary>
        /// Copy file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void CopyFile(string vfsPath, string newVfsPath) {
            lock (this.vfs) {
                vfs.CopyFile(vfsPath, newVfsPath);
            }
        }

        /// <summary>
        /// List contents of a directory.
        /// </summary>
        /// <returns>List of directories and files contained in vfsPath.</returns>
        // Exceptions:
        // - path points to a file (not directory).
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

        internal static object IJCDBasicTypeCallStaticMethod(Type type, string methodName, object[] args) {
            if (type.GetInterface("IJCDBasicVFS") == null) {
                throw new Exception("Can only be used with objects that implement IJCDBasicVFS");
            }

            var method = type.GetMethod(methodName);
            return (IJCDBasicVFS)method.Invoke(null, args);
        }

        private void ConnectToHub() {
            var conns = ConnectToHubStatic();
            this.hubConn = conns.Item1;
            this.hubProxy = conns.Item2;
        }

        private static Tuple<HubConnection, IHubProxy> ConnectToHubStatic() {
            var hubConn = new HubConnection(JCDSynchronizerSettings.PublicAddress);
            var hubProxy = hubConn.CreateHubProxy(JCDSynchronizerSettings.HubName);
            hubConn.Start().Wait();
            return Tuple.Create(hubConn, hubProxy);
        }
        
        private T HubInvoke<T>(string methodName, params object[] args) {
            return HubInvoke<T>(this.hubProxy, methodName, args);
        }

        private static T HubInvoke<T>(IHubProxy hubProxy, string methodName, params object[] args) {
            return hubProxy.Invoke<T>(methodName, args).Result;
        }
    }
}
