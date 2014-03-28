namespace vfs.core
{
    public interface IJCDAdvancedVFS
    {
        // Expand VFS size.
        // Exceptions:
        // - no such path on HFS.
        // - new size < current size.
        void Expand(string hfsPath, ulong newSize);
        
        // Exceptions:
        // - no such path on HFS.
        // - new size >= current size.
        // - new size too small to contain files currently in VFS.
        void Shrink(string hfsPath, ulong newSize);
    }
}