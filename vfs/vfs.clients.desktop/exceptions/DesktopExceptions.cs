using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace vfs.clients.desktop.exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class NoSessionException : Exception
    {
        public NoSessionException() : base() { }
        public NoSessionException(string msg) : base(msg) { }
        public NoSessionException(string msg, Exception e) : base(msg, e) { }
        protected NoSessionException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class SessionException : Exception
    {
        public SessionException() : base() { }
        public SessionException(string msg) : base(msg) { }
        public SessionException(string msg, Exception e) : base(msg, e) { }
        protected SessionException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class InvalidPathException : Exception
    {
        public InvalidPathException() : base() { }
        public InvalidPathException(string msg) : base(msg) { }
        public InvalidPathException(string msg, Exception e) : base(msg, e) { }
        protected InvalidPathException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException() : base() { }
        public FileAlreadyExistsException(string msg) : base(msg) { }
        public FileAlreadyExistsException(string msg, Exception e) : base(msg, e) { }
        protected FileAlreadyExistsException(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }
}
