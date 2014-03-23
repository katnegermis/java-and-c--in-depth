namespace vfs.core
{
    public interface IAdvancedVFS
    {
        // Expand VFS size.
        // Exceptions:
        // - no such path on HFS.
        // - new size < current size.
        public void Expand(string hfsPath, ulong newSize);

        // Exceptions:
        // - no such path on HFS.
        // - new size >= current size.
        // - new size too small to contain files currently in VFS.
        public void Shrink(string hfsPath, ulong newSize);
    }
}