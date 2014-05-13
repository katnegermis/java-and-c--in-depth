using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.synchronizer.common {
    public interface IJCDSynchronizerCommon {
        /// <summary>
        /// Inform the other party that a file was added.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="data">Data of the file.</param>
        void FileAdded(string path, byte[] data);

        /// <summary>
        /// Inform the other party that a file was deleted.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        void FileDeleted(string path);

        /// <summary>
        /// Inform the other party that a file was moved.
        /// </summary>
        /// <param name="oldPath">Old path of the file.</param>
        /// <param name="newPath">New (current) path of the file.</param>
        void FileMoved(string oldPath, string newPath);

        /// <summary>
        /// Inform the other party that a file was modified.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="offset">Offset from which the file was modified.</param>
        /// <param name="data">New data to be written, starting from offset.</param>
        void FileModified(string path, long offset, byte[] data);

        /// <summary>
        /// Inform the other party that a file was resized.
        /// 
        /// If the file increased in size, the callee of this function should zero out
        /// the unused space.
        /// If the file decreased in size, whatever was beyond the new size should be
        /// discarded.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="newSize">New size of the file.</param>
        void FileResized(string path, long newSize);
    }
}
