namespace vfs.core
{
    public delegate void MoveFileEventHandler(string oldPath, string newPath);
    public delegate void DeleteFileEventHandler(string path);
    public delegate void AddFileEventHandler(string path);

    public interface IJCDBasicVFS
    {

        event AddFileEventHandler FileAdded;
        event DeleteFileEventHandler FileDeleted;
        event MoveFileEventHandler FileMoved;

        //You can't have static methods in an interface
        /*/// <summary>
        /// Create a new VFS-file.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the hard file system</param>
        /// <param name="size">The size of the new vfs</param>
        /// <returns>An object of an implementation of the IJCDBasicVFS interface</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException"
        // - too little space available on HFS
        // - invalid path string (file name too long/invalid characters).
        // - no permissions to write on HFS.
        static IJCDBasicVFS Create(string hfsPath, ulong size);

        /// <summary>
        /// Delete an unmounted VFS from HFS.
        /// </summary>
        /// <param name="hfsPath">The path of the file on the host file system</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"
        // - VFS is mounted.
        static void Delete(string hfsPath);

        /// <summary>
        /// Mount an existing VFS-file.
        /// </summary>
        /// <param name="hfsPath"> The path of the file on the host file system</param>
        /// <returns>An object of an implementation of the IJCDBasicVFS interface</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        // - VFS file already mounted.
        // - file is not VFS type.
        static IJCDBasicVFS Open(string hfsPath);*/

        /// <summary>
        /// Unmount a mounted VFS.
        /// </summary>
        // - No VFS mounted
        void Close();

        /// <summary>
        /// Get total size of a mounted VFS.
        /// </summary>
        /// <returns>Size of mounted VFS.</returns>
        // - No VFS mounted
        ulong Size();

        /// <summary>
        /// Get total amount of occupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of occupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        // - No VFS mounted
        ulong OccupiedSpace();

        /// <summary>
        /// Get total amount of unoccupied space on a mounted VFS.
        /// </summary>
        /// <returns>Amount of unoccupied space.</returns>
        /// <remarks>It should hold that OccupiedSpace + UnoccupiedSpace == Size</remarks>
        /// - No VFS mounted
        ulong FreeSpace();

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
        void CreateDirectory(string vfsPath, bool createParents);

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
        void CreateFile(string vfsPath, ulong size, bool createParents);

        /// <summary>
        /// Import a file or directory from HFS to a mounted VFS.
        /// </summary>
        // Exceptions:
        // - no such path on HFS.
        // - no such path on VFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ImportFile(string hfsPath, string vfsPath);

        /// <summary>
        /// Export a file or directory from a mounted VFS to HFS.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - no such path on HFS.
        // - too little space available on HFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ExportFile(string vfsPath, string hfsPath);

        /// <summary>
        /// Delete a file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        // - path points to a directory (recursive == false).
        void DeleteFile(string vfsPath, bool recursive);

        ///<summary>
        /// Rename file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void RenameFile(string vfsPath, string newName);

        /// <summary>
        /// Move file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void MoveFile(string vfsPath, string newVfsPath);

        /// <summary>
        /// Copy file or directory on a mounted VFS.
        /// </summary>
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void CopyFile(string vfsPath, string newVfsPath);

        /// <summary>
        /// List contents of a directory.
        /// </summary>
        /// <returns>List of directories and files contained in vfsPath.</returns>
        // Exceptions:
        // - path points to a file (not directory).
        JCDDirEntry[] ListDirectory(string vfsPath);

        // Methods that aren't necessarily needed to be implemented here,
        // and could easily be implemented a layer above.
        void SetCurrentDirectory(string vfsPath);
        string GetCurrentDirectory();
    }
}