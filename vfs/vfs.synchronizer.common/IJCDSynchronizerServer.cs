using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;

namespace vfs.synchronizer.common {
    public interface IJCDSynchronizerServer {
        JCDSynchronizerReply Register(string username, string password);

        JCDSynchronizerReply LogIn(string username, string password);

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/

        JCDSynchronizerReply LogOut();

        /// <summary>
        /// Retrieve a list of tuples of VFS ids and names.
        /// </summary>
        /// <returns>SynchronizerReply with Data set to a list of tuples of VFS ids and names.</returns>
        JCDSynchronizerReply ListVFSes();

        /// <summary>
        /// Add a VFS to the user account.
        /// SynchronizerReply.Data will be set to an int, the new id of the VFS.
        /// </summary>
        /// <param name="vfsName">Name of the VFS.</param>
        /// <param name="data">The data of the VFS</param>
        /// <returns>SynchronizerReply with Data being the id (int) of the VFS.</returns>
        JCDSynchronizerReply AddVFS(string vfsName, byte[] data);

        /// <summary>
        /// Delete a VFS from the user account.
        /// </summary>
        /// <param name="id">ID of the VFS to delete.</param>
        JCDSynchronizerReply DeleteVFS(int id);

        /// <summary>
        /// Retrieve an entire VFS.
        /// SynchronizerReply.Data will be set to a byte array which is the data of the full VFS.
        /// </summary>
        /// <param name="vfsId">ID of the vfs to retrieve. Can be found by calling ListVFSes</param>
        /// <returns>SynchronizerReply with Data set to a byte array which contains the full VFS.</returns>
        JCDSynchronizerReply RetrieveVFS(int vfsId);

        /// <summary>
        /// Inform the other party that a file was added.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="data">Data of the file.</param>
        /// <returns>SynchronizerReply with Data set to the most current change id.</returns>
        JCDSynchronizerReply FileAdded(string path, byte[] data);

        /// <summary>
        /// Inform the other party that a file was deleted.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>SynchronizerReply with Data set to the most current change id.</returns>
        JCDSynchronizerReply FileDeleted(string path);

        /// <summary>
        /// Inform the other party that a file was moved.
        /// </summary>
        /// <param name="oldPath">Old path of the file.</param>
        /// <param name="newPath">New (current) path of the file.</param>
        /// <returns>SynchronizerReply with Data set to the most current change id.</returns>
        JCDSynchronizerReply FileMoved(string oldPath, string newPath);

        /// <summary>
        /// Inform the other party that a file was modified.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="offset">Offset from which the file was modified.</param>
        /// <param name="data">New data to be written, starting from offset.</param>
        /// <returns>SynchronizerReply with Data set to the most current change id.</returns>
        JCDSynchronizerReply FileModified(string path, long offset, byte[] data);

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
        /// <returns>SynchronizerReply with Data set to the most current change id.</returns>
        JCDSynchronizerReply FileResized(string path, long newSize);
    }
}
