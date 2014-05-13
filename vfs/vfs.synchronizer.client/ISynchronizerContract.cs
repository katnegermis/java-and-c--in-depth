using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.synchronizer {

    [ServiceContract(SessionMode=SessionMode.Required,
                     CallbackContract=typeof(ISynchronizerCallback))]
    public interface ISynchronizerContract : ISynchronizerBase {
        // Maybe create an class which holds an error code and message, instead of
        // using just bools here. This way we could pass on a message to the user,
        // informing of what went wrong.

        [OperationContract(IsOneWay = false)]
        bool Register(string username, string password);

        [OperationContract(IsOneWay = false)]
        bool Login(string username, string password);

        /************************************************************************
         * The following functions assume that the user is logged in, and that  *
         * the server knows which VFSes belong to a particular user.            *
         ************************************************************************/
        
        /// <summary>
        /// Retrieve a list of tuples of VFS ids and names.
        /// </summary>
        /// <returns>List of tuples of VFS ids and names.</returns>
        [OperationContract(IsOneWay = false)]
        List<Tuple<int, string>> ListVFSes();
        
        /// <summary>
        /// Add a VFS to the user account.
        /// </summary>
        /// <param name="vfsName">Name of the VFS.</param>
        /// <param name="data">The data of the VFS</param>
        /// <returns>The ID which the VFS will be known as in the future.</returns>
        [OperationContract(IsOneWay = false)]
        int AddVFS(string vfsName, byte[] data);

        /// <summary>
        /// Delete a VFS from the user account.
        /// </summary>
        /// <param name="id">ID of the VFS to delete.</param>
        [OperationContract(IsOneWay = false)]
        void DeleteVFS(int id);

        /// <summary>
        /// Retrieve an entire VFS.
        /// </summary>
        /// <param name="vfsId">ID of the vfs to retrieve. Can be found by calling ListVFSes</param>
        /// <returns>Data of the VFS.</returns>
        [OperationContract(IsOneWay = false)]
        byte[] RetrieveVFS(int vfsId);
    }
}
