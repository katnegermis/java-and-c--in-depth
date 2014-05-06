using System;

namespace vfs.exceptions
{
    public class FileAlreadyExistsException : Exception { }

    public class FileNotFoundException : Exception { }

    public class FileTooSmallException : Exception { }

    public class BufferTooSmallException : Exception { }

    public class ParentNotFoundException : FileNotFoundException { }

    public class NotAFolderException : FileNotFoundException { }

    public class FileExistsException : Exception { }

    public class MoveToSelfException : Exception { }

    public class FileSystemNotOpenException : Exception { }

    public class InvalidFileException : Exception { }

    public class InvalidFileNameException : Exception { }

    public class InvalidSizeException : Exception { }

    public class NotEnoughSpaceException : Exception { }

    public class RootDeletionException : Exception { }

    public class NonRecursiveDeletionException : Exception { }

    public class InvalidFATIndexException : Exception { }

    public class ReachedEndOfFileException : Exception { }
}
