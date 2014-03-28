namespace vfs.core
{
    public interface IBasicVFS
    {

        // Create a new VFS-file.
        // Exceptions:
        // - no such path.
        // - too little space available on HFS
        // - invalid path string (file name too long/invalid characters).
        // - no permissions to write on HFS.
        void Create(string hfsPath, ulong size);

        // Delete an unmounted VFS from HFS.
        // Exceptions:
        // - VFS is mounted.
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        void Delete(string hfsPath);

        // Mount an existing VFS-file.
        // Return: VFS id.
        // Exceptions:
        // - VFS file already mounted.
        // - file is not VFS type.
        // - invalid path string (file name too long/invalid characters).
        int Mount(string hfsPath);

        // Unmount a mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        int Unmount(int vfsId);

        // Get total size of a mounted VFS.
        // Return: Size of mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        ulong Size(int vfsId);

        // Get total amount of occupied space on a mounted VFS.
        // Return: Amount of occupied space.
        // Exceptions:
        // - no open VFS with given id.
        // Notes:
        // - It should hold that OccupiedSpace + UnoccupiedSpace == Size
        ulong OccupiedSpace(int vfsId);

        // Get total amount of unoccupied space on a mounted VFS.
        // Return: Amount of unoccupied space.
        // Exceptions:
        // - no open VFS with given id.
        // Notes:
        // - It should hold that OccupiedSpace + UnoccupiedSpace == Size
        ulong UnoccupiedSpace(int vfsId);

        // Create a directory on a mounted VFS.
        // Optionally create parents (like mkdir -p).
        // Exceptions:
        // - no open VFS with given id.
        // - no such path (createParents == false).
        // - too little space available on VFS.
        // - invalid path string (file name too long/invalid characters).
        void CreateDirectory(int vfsId, string vfsPath, bool createParents);

        // Import a file or directory from HFS to a mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        // - no such path on HFS.
        // - no such path on VFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ImportFile(int vfsId, string hfsPath, string vfsPath);

        // Export a file or directory from a mounted VFS to HFS.
        // Exceptions:
        // - no open VFS with given id.
        // - no such path on VFS.
        // - no such path on HFS.
        // - too little space available on HFS.
        // - invalid HFS path string (file name too long/invalid characters).
        // - invalid VFS path string (file name too long/invalid characters).
        void ExportFile(int vfsId, string vfsPath, string hfsPath);

        // Delete a file or directory on a mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        // - invalid path string (file name too long/invalid characters).
        // - no such path.
        // - path points to a directory (recursive == false).
        void DeleteFile(int vfsId, string vfsPath, bool recursive);

        // Rename file or directory on a mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void RenameFile(int vfsId, string vfsPath, string newName);

        // Move file or directory on a mounted VFS.
        // Exceptions:
        // - no open VFS with given id.
        // - invalid path string (file name too long/invalid characters).
        // - invalid file name (too long/invalid characters).
        // - no such file on VFS.
        void MoveFile(int vfsId, string vfsPath, string newVfsPath);

        // List contents of a directory.
        // Return: List of directories and files contained in vfsPath.
        // Exceptions:
        // - no open VFS with given id.
        // - path points to a file (not directory).
        File[] ListDirectory(int vfsId, string vfsPath);

        // Methods that aren't necessarily needed to be implemented here,
        // and could easily be implemented a layer above.
        // In fact, implementing them on this layer would mean that all paths
        // used in other methods should be interpreted as being relative.
        void SetCurrentDirectory(int vfsId, string vfsPath);
        string GetCurrentDirectory(int vfsId);
    }
}