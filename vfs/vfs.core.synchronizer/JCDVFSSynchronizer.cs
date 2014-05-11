﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;

namespace vfs.core.synchronizer
{
    public class JCDVFSSynchronizer : IJCDBasicVFS
    {
        private IJCDBasicVFS vfs;

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

        
        internal void InformServerFileAdded(string path) {
            throw new NotImplementedException();
        }

        internal void InformServerFileDeleted(string path) {
            throw new NotImplementedException();
        }

        internal void InformServerFileMoved(string oldPath, string newPath) {
            throw new NotImplementedException();
        }

        internal void InformServerFileModified(string path, long offset, byte[] data) {
            throw new NotImplementedException();
        }

        internal void InformServerFileResized(string path, long newSize) {
            throw new NotImplementedException();
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
        /// <returns>An object of an implementation of the IJCDBasicVFS interface</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException"
        // - too little space available on HFS
        // - invalid path string (file name too long/invalid characters).
        // - no permissions to write on HFS.
        public static JCDVFSSynchronizer Create(Type vfsType, string hfsPath, ulong size) {
            var vfs = (IJCDBasicVFS)IJCDBasicTypeCallMethod(vfsType, "Create", new object[] { hfsPath, size });
            return new JCDVFSSynchronizer(vfs);
        }

        /// <summary>
        /// Delete an unmounted VFS from HFS.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the host file system</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"
        // - VFS is mounted.
        public static void Delete(Type vfsType, string hfsPath) {
            IJCDBasicTypeCallMethod(vfsType, "Delete", new object[] { hfsPath });
        }

        /// <summary>
        /// Mount an existing VFS-file.
        /// </summary>
        /// <param name="hfsPath"> The path of the file on the host file system</param>
        /// <returns>An object of an implementation of the IJCDBasicVFS interface</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        // - VFS file already mounted.
        // - file is not VFS type.
        public static JCDVFSSynchronizer Open(Type vfsType, string hfsPath) {
            var vfs = (IJCDBasicVFS)IJCDBasicTypeCallMethod(vfsType, "Open", new object[] { hfsPath });
            return new JCDVFSSynchronizer(vfs);

        }

        /// <summary>
        /// Unmount a mounted VFS.
        /// </summary>
        // - No VFS mounted
        public void Close() {
            vfs.Close();
        }

        /// <summary>
        /// Get total size of a mounted VFS.
        /// </summary>
        /// <returns>Size of mounted VFS.</returns>
        // - No VFS mounted
        public ulong Size() {
            return vfs.Size();
        }

        /// <summary>
        /// Get total amount of occupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of occupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        // - No VFS mounted
        public ulong OccupiedSpace() {
            return vfs.OccupiedSpace();
        }

        /// <summary>
        /// Get total amount of unoccupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of unoccupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        /// - No VFS mounted
        public ulong FreeSpace() {
            return vfs.FreeSpace();
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
            vfs.CreateDirectory(vfsPath, createParents);
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
            vfs.CreateFile(vfsPath, size, createParents);
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
            vfs.ImportFile(hfsPath, vfsPath);
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
            vfs.ExportFile(vfsPath, hfsPath);
        }

        /// <summary>
        /// Delete a file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        // - path points to a directory (recursive == false).
        public void DeleteFile(string vfsPath, bool recursive) {
            vfs.DeleteFile(vfsPath, recursive);
        }

        ///<summary>
        /// Rename file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void RenameFile(string vfsPath, string newName) {
            vfs.RenameFile(vfsPath, newName);
        }

        /// <summary>
        /// Move file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void MoveFile(string vfsPath, string newVfsPath) {
            vfs.MoveFile(vfsPath, newVfsPath);
        }

        /// <summary>
        /// Copy file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        public void CopyFile(string vfsPath, string newVfsPath) {
            vfs.CopyFile(vfsPath, newVfsPath);
        }

        /// <summary>
        /// List contents of a directory.
        /// </summary>
        /// <returns>List of directories and files contained in vfsPath.</returns>
        // Exceptions:
        // - path points to a file (not directory).
        public JCDDirEntry[] ListDirectory(string vfsPath) {
            return vfs.ListDirectory(vfsPath);
        }

        public void SetCurrentDirectory(string vfsPath) {
            vfs.SetCurrentDirectory(vfsPath);
        }

        public string GetCurrentDirectory() {
            return vfs.GetCurrentDirectory();
        }

        public JCDFileStream GetFileStream(string vfsPath) {
            return vfs.GetFileStream(vfsPath);
        }

        internal static object IJCDBasicTypeCallMethod(Type type, string methodName, object[] args) {
            if (type.GetInterface("IJCDBasicVFS") == null) {
                throw new Exception("Can only be used with objects that implement IJCDBasicVFS");
            }

            var method = type.GetMethod(methodName);
            return (IJCDBasicVFS)method.Invoke(null, args);
        }
    }
}