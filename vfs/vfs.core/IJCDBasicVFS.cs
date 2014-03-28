namespace vfs.core
{
    public interface IJCDBasicVFS
    {

        // Create a new VFS-file.
        // Exceptions:
        // - no such path.
        // - too little space available on HFS
        // - invalid path string (file name too long/invalid characters).
        // - no permissions to write on HFS.
        static IJCDBasicVFS Create(string hfsPath, ulong size);

        // Delete an unmounted VFS from HFS.
        // Exceptions:
        // - VFS is mounted.
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        static void Delete(string hfsPath);

        // Mount an existing VFS-file.
        // Return: VFS id.
        // Exceptions:
        // - VFS file already mounted.
        // - file is not VFS type.
        // - invalid path string (file name too long/invalid characters).
        static IJCDBasicVFS Open(string hfsPath);

        // Unmount a mounted VFS.
        // Exceptions:
        void Close();

        // Get total size of a mounted VFS.
        // Return: Size of mounted VFS.
        // Exceptions:
        ulong Size();

        // Get total amount of occupied space on a mounted VFS.
        // Return: Amount of occupied space.
        // Exceptions:
        // Notes:
        // - It should hold that OccupiedSpace + UnoccupiedSpace == Size
        ulong OccupiedSpace();

        // Get total amount of unoccupied space on a mounted VFS.
        // Return: Amount of unoccupied space.
        // Exceptions:
        // Notes:
        // - It should hold that OccupiedSpace + UnoccupiedSpace == Size
        ulong FreeSpace();

        // Create a directory on a mounted VFS.
        // Optionally create parents (like mkdir -p).
        // Exceptions:
        // - no such path (createParents == false).
        // - too little space available on VFS.
        // - invalid path string (file name too long/invalid characters).
        void CreateDirectory(string vfsPath, bool createParents);

        // Import a file or directory from HFS to a mounted VFS.
        // Exceptions:
        // - no such path on HFS.
        // - no such path on VFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ImportFile(string hfsPath, string vfsPath);

        // Export a file or directory from a mounted VFS to HFS.
        // Exceptions:
        // - no such path on VFS.
        // - no such path on HFS.
        // - too little space available on HFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ExportFile(string vfsPath, string hfsPath);

        // Delete a file or directory on a mounted VFS.
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        // - path points to a directory (recursive == false).
        void DeleteFile(string vfsPath, bool recursive);

        // Rename file or directory on a mounted VFS.
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void RenameFile(string vfsPath, string newName);

        // Move file or directory on a mounted VFS.
        // Exceptions:
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void MoveFile(string vfsPath, string newVfsPath);

        // List contents of a directory.
        // Return: List of directories and files contained in vfsPath.
        // Exceptions:
        // - path points to a file (not directory).
        JCDFile[] ListDirectory(string vfsPath);

        // Methods that aren't necessarily needed to be implemented here,
        // and could easily be implemented a layer above.
        void SetCurrentDirectory(string vfsPath);
        string GetCurrentDirectory();
    }
}