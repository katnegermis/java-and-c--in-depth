using System;
using System.Runtime.Serialization;

namespace vfs.exceptions {
    [Serializable]
    public class FileAlreadyExistsException : Exception {
        public FileAlreadyExistsException() : base() { }
        public FileAlreadyExistsException(string msg) : base(msg) { }
        public FileAlreadyExistsException(string msg, Exception e) : base(msg, e) { }
        protected FileAlreadyExistsException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class FileNotFoundException : Exception {
        public FileNotFoundException() : base() { }
        public FileNotFoundException(string msg) : base(msg) { }
        public FileNotFoundException(string msg, Exception e) : base(msg, e) { }
        protected FileNotFoundException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class FileTooSmallException : Exception {
        public FileTooSmallException() : base() { }
        public FileTooSmallException(string msg) : base(msg) { }
        public FileTooSmallException(string msg, Exception e) : base(msg, e) { }
        protected FileTooSmallException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class BufferTooSmallException : Exception {
        public BufferTooSmallException() : base() { }
        public BufferTooSmallException(string msg) : base(msg) { }
        public BufferTooSmallException(string msg, Exception e) : base(msg, e) { }
        protected BufferTooSmallException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class ParentNotFoundException : FileNotFoundException {
        public ParentNotFoundException() : base() { }
        public ParentNotFoundException(string msg) : base(msg) { }
        public ParentNotFoundException(string msg, Exception e) : base(msg, e) { }
        protected ParentNotFoundException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class NotAFolderException : FileNotFoundException {
        public NotAFolderException() : base() { }
        public NotAFolderException(string msg) : base(msg) { }
        public NotAFolderException(string msg, Exception e) : base(msg, e) { }
        protected NotAFolderException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class FileExistsException : Exception {
        public FileExistsException() : base() { }
        public FileExistsException(string msg) : base(msg) { }
        public FileExistsException(string msg, Exception e) : base(msg, e) { }
        protected FileExistsException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class MoveToSelfException : Exception {
        public MoveToSelfException() : base() { }
        public MoveToSelfException(string msg) : base(msg) { }
        public MoveToSelfException(string msg, Exception e) : base(msg, e) { }
        protected MoveToSelfException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class FileSystemNotOpenException : Exception {
        public FileSystemNotOpenException() : base() { }
        public FileSystemNotOpenException(string msg) : base(msg) { }
        public FileSystemNotOpenException(string msg, Exception e) : base(msg, e) { }
        protected FileSystemNotOpenException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class InvalidFileException : Exception {
        public InvalidFileException() : base() { }
        public InvalidFileException(string msg) : base(msg) { }
        public InvalidFileException(string msg, Exception e) : base(msg, e) { }
        protected InvalidFileException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class InvalidFileNameException : Exception {
        public InvalidFileNameException() : base() { }
        public InvalidFileNameException(string msg) : base(msg) { }
        public InvalidFileNameException(string msg, Exception e) : base(msg, e) { }
        protected InvalidFileNameException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class InvalidSizeException : Exception {
        public InvalidSizeException() : base() { }
        public InvalidSizeException(string msg) : base(msg) { }
        public InvalidSizeException(string msg, Exception e) : base(msg, e) { }
        protected InvalidSizeException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class NotEnoughSpaceException : Exception {
        public NotEnoughSpaceException() : base() { }
        public NotEnoughSpaceException(string msg) : base(msg) { }
        public NotEnoughSpaceException(string msg, Exception e) : base(msg, e) { }
        protected NotEnoughSpaceException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class RootDeletionException : Exception {
        public RootDeletionException() : base() { }
        public RootDeletionException(string msg) : base(msg) { }
        public RootDeletionException(string msg, Exception e) : base(msg, e) { }
        protected RootDeletionException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class NonRecursiveDeletionException : Exception {
        public NonRecursiveDeletionException() : base() { }
        public NonRecursiveDeletionException(string msg) : base(msg) { }
        public NonRecursiveDeletionException(string msg, Exception e) : base(msg, e) { }
        protected NonRecursiveDeletionException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class InvalidFATIndexException : Exception {
        public InvalidFATIndexException() : base() { }
        public InvalidFATIndexException(string msg) : base(msg) { }
        public InvalidFATIndexException(string msg, Exception e) : base(msg, e) { }
        protected InvalidFATIndexException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [Serializable]
    public class ReachedEndOfFileException : Exception {
        public ReachedEndOfFileException() : base() { }
        public ReachedEndOfFileException(string msg) : base(msg) { }
        public ReachedEndOfFileException(string msg, Exception e) : base(msg, e) { }
        protected ReachedEndOfFileException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }
}
