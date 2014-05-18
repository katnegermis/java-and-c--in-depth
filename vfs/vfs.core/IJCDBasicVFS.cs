using System.IO;

namespace vfs.core
{
    public delegate void MoveFileEventHandler(string oldPath, string newPath);
    public delegate void DeleteFileEventHandler(string path);
    public delegate void AddFileEventHandler(string path, long size, bool isFolder);
    public delegate void ModifyFileEventHandler(string path, long offset, byte[] data);
    public delegate void ResizeFileEventHandler(string path, long newSize);

    public interface IJCDBasicVFS
    {

        /// <summary>
        /// Event to be called every time a new file is added to the file system.
        /// </summary>
        /// <param name="path">Path of the newly added file.</param>
        event AddFileEventHandler FileAdded;

        /// <summary>
        /// Event to be called every time a file is deleted from the file system.
        /// </summary>
        /// <param name="path">Path of the deleted file.</param>
        event DeleteFileEventHandler FileDeleted;

        /// <summary>
        /// Event to be called every time a file is moved or renamed on the file system.
        /// </summary>
        /// <param name="oldPath">File's previous (old) path.</param>
        /// <param name="newPath">File's new (current) path.</param>
        event MoveFileEventHandler FileMoved;

        /// <summary>
        /// Event to be called every time a file is modified.
        /// This does NOT include file resizing!
        /// </summary>
        /// <param name="path">File's path.</param>
        /// <param name="offset">Offset in to file where the data was written.</param>
        /// <param name="data">Data that was written.</param>
        event ModifyFileEventHandler FileModified;

        /// <summary>
        /// Event to be called every time a file (NOT folder) is resized.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="newSize">New size of the file.</param>
        event ResizeFileEventHandler FileResized;

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
        JCDFileStream CreateFile(string vfsPath, ulong size, bool createParents);

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
        /// Import a file or directory from a stream to a mounted VFS.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - invalid VFS path string (file name too long/invalid characters).
        void ImportFile(Stream file, string vfsPath);

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
        /// Export a file from a mounted VFS to a stream.
        /// </summary>
        // Exceptions:
        // - no such path on VFS.
        // - the VFS path points to a directory
        // - invalid VFS path string (file name too long/invalid characters).
        void ExportFile(string vfsPath, Stream output);

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

        /// <summary>
        /// Get a file stream for manipulating a file on the vfs.
        /// </summary>
        /// <param name="vfsPath">Path to the file.</param>
        /// <returns>JCDFileStream</returns>
        JCDFileStream GetFileStream(string vfsPath);

        void SetCurrentDirectory(string vfsPath);

        string GetCurrentDirectory();

        string[] Search(string fileName, bool caseSensitive);

        string[] Search(string searchDir, string fileName, bool caseSensitive, bool recursive);

        JCDDirEntry GetFileDetails(string vfsPath);

        long GetId();
        void SetId(long id);
    }
}