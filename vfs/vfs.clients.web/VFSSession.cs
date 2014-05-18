using System;
using System.Collections.Generic;
using System.IO;
using vfs.core;
using vfs.common;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using vfs.synchronizer.client;
using vfs.synchronizer.common;
using Microsoft.AspNet.SignalR;

namespace vfs.clients.web
{
    /// <summary>
    /// Class that represents an entry into the VFS, directory or file.
    /// Serves as container between controller and view classes.
    /// </summary>
    public class DirectoryEntry {
        public bool IsFolder { get; private set; }
        public string Name { get; private set; }
        public string Path { get; private set; }
        public ulong Size { get; private set; }

        public string TypeURL {
            get {
                return IsFolder ? "folder.png" : "file.png";
            }
        }
        public DirectoryEntry(string name, string path, bool isFolder, ulong size) {
            this.Name = name;
            this.Path = path;
            this.IsFolder = isFolder;
            this.Size = size;
        }

        public DirectoryEntry(JCDDirEntry entry) {
            this.Name = entry.Name;
            this.Path = "";
            this.IsFolder = entry.IsFolder;
            this.Size = entry.Size;
        }
    }

    /// <summary>
    /// Enum that represents the possible search locations.
    /// </summary>
    public enum SearchLocation { Folder, SubFolder/*, Everywhere*/ };

    /// <summary>
    /// Class that serves as controller class for operations on VFSs.
    /// If a VFS file is mounted, the object is contained in a VFSSession object.
    /// It can only be accessed and modified through the available methods.
    /// </summary>
    public class VFSSession
    {
        /// <summary>
        /// The currently mounted VFS.
        /// If none is mounted, then null.
        /// </summary>
        private JCDVFSSynchronizer mountedVFS;

        /// <summary>
        /// The location of the currently open VFS.
        /// </summary>
        public string mountedVFSpath;

        /// <summary>
        /// Whether the client has an outstanding update request.
        /// </summary>
        public bool updateScheduled = false;

        //The following three are copy-pasted from desktop VFSSession - check them

        /// <summary>
        /// Returns whether the VFSSynchronizer is logged in or not
        /// </summary>
        public bool IsLoggedIn {
            get {
                return (mountedVFS != null && mountedVFS.LoggedIn());
            }
        }

        /// <summary>
        /// The ID of the VFS
        /// </summary>
        public long VFSVersionId {
            get {
                return mountedVFS.GetId();
            }
            set {
                mountedVFS.SetId(value);
            }
        }

        /// <summary>
        /// Gets or sets the userName of the logged in user.
        /// If none is logged in it return null.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The server ID of the session.
        /// </summary>
        private string sessionID;

        /// <summary>
        /// The current directory we are in.
        /// </summary>
        public string CurrentDir
        {
            get
            {
                return mountedVFS != null ? mountedVFS.GetCurrentDirectory() : null;
            }
            private set
            {
                mountedVFS.SetCurrentDirectory(value);
            }
        }

        /// <summary>
        /// The free space of the mounted VFS.
        /// </summary>
        public ulong FreeSpace
        {
            get
            {
                return mountedVFS != null ? mountedVFS.FreeSpace() : 0;
            }
        }

        /// <summary>
        /// The occupied space of the mounted VFS.
        /// </summary>
        public ulong OccupiedSpace
        {
            get
            {
                return mountedVFS != null ? mountedVFS.OccupiedSpace() : 0;
            }
        }

        /// <summary>
        /// The pathes of files and directories that have been chosen to be copied or cut
        /// </summary>
        private string[] clipboardPaths;

        /// <summary>
        /// Boolean that indicates what the copy/cut mode is.
        /// Cut if true, just copy otherwise.
        /// </summary>
        private bool cutNotCopy;

        /// <summary>
        /// The currently set choice of the search location.
        /// </summary>
        public SearchLocation SearchLocation = SearchLocation.SubFolder;

        /// <summary>
        /// Stores the current search results.
        /// </summary>
        public DirectoryEntry[] currentSearchResults;

        /// <summary>
        /// Boolean that represents whether a search is done case sensitive or insensitive.
        /// </summary>
        public bool SearchCaseSensitive = true;

        private VFSSession(string SessionID, JCDVFSSynchronizer vfs, string VFSpath)
        {
            mountedVFS = vfs;
            mountedVFSpath = VFSpath;
            sessionID = SessionID;

            vfs.FileAdded += (string path, long size, bool isFolder) => {
                checkIfUpdateNeeded(path);
            };
            vfs.FileDeleted += (string path) => {
                checkIfUpdateNeeded(path);
            };
            vfs.FileMoved += (string oldPath, string newPath) => {
                checkIfUpdateNeeded(oldPath);
                checkIfUpdateNeeded(newPath);
            };
            vfs.FileModified += (string path, long offset, byte[] data) => {
                checkIfUpdateNeeded(path);
            };
            vfs.FileResized += (string path, long newSize) => {
                checkIfUpdateNeeded(path);
            };
        }

        #region VFS Methods

        /// <summary>
        /// Makes the call to create the VFS file at the given place and with the given size.
        /// </summary>
        /// <param name="SessionID">The server ID of the session</param>
        /// <param name="fileToCreate">To create and put the VFS inside</param>
        /// <param name="size">Of the new VFS</param>
        /// /// <returns>A new VFSSession object if the VFS was created successfully or null otherwise</returns>
        public static VFSSession CreateVFS(string SessionID, string fileToCreate, ulong size)
        {
            //var vfs = JCDFAT.Create(fileToCreate, size);
            var vfs = JCDVFSSynchronizer.Create(typeof(JCDFAT), fileToCreate, size);
            if(vfs != null)
                return new VFSSession(SessionID, vfs, fileToCreate);
            else
                return null;
        }

        /// <summary>
        /// Delete the currently open VFS
        /// </summary>
        public void DeleteVFS() {
            if(mountedVFS == null)
                throw new Exception("No VFS mounted!");

            string VFSpath = mountedVFSpath;
            Close();
            JCDVFSSynchronizer.Delete(typeof(JCDFAT), VFSpath);
        }

        /// <summary>
        /// Makes the call to open the given VFS file.
        /// Then returns a new VFSSession object.
        /// </summary>
        /// <param name="SessionID">The server ID of the session</param>
        /// <param name="fileToOpen">The file to open</param>
        /// <returns>A new VFSSession object if the VFS was opened successfully or null otherwise</returns>
        public static VFSSession OpenVFS(string SessionID, string fileToOpen)
        {
            //var vfs = JCDFAT.Open(fileToOpen);
            var vfs = JCDVFSSynchronizer.Open(typeof(JCDFAT), fileToOpen);
            if (vfs != null)
                return new VFSSession(SessionID, vfs, fileToOpen);
            else
                return null;
        }

        /// <summary>
        /// Closes the mounted VFS object.
        /// </summary>
        public void Close()
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            mountedVFS.Close();
            mountedVFS = null;
            mountedVFSpath = null;
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Creates a directory with the given name in the current directory of the VFS.
        /// </summary>
        /// <param name="dirName">Name of the new directory</param>
        public void CreateDir(string dirName)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            mountedVFS.CreateDirectory(Helpers.PathCombine(CurrentDir, dirName), false);
        }

        /// <summary>
        /// Renames the file/dir with the given name in the current directory to the new name.
        /// </summary>
        /// <param name="oldName">Name of the file/dir to rename</param>
        /// <param name="newName">New name to set</param>
        public void Rename(string oldName, string newName)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            if(oldName == newName)
                return;

            mountedVFS.RenameFile(Helpers.PathCombine(CurrentDir, oldName), newName);
        }

        /// <summary>
        /// Returns whether there are files in the clipboard.
        /// </summary>
        public bool clipBoardNonEmpty() {
            return (clipboardPaths != null && clipboardPaths.Length > 0);
        }

        /// <summary>
        /// Copies the files/dirs with the given names in the current directory into the clipboard.
        /// </summary>
        /// <param name="names">Names of the files/dirs</param>
        public void Copy(string[] names)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            cutNotCopy = false;
            putIntoClipboard(names);

        }

        /// <summary>
        /// Cuts the files/dirs with the given names in the current directory into the clipboard.
        /// </summary>
        /// <param name="names">Names of the files/dirs</param>
        public void Cut(string[] names)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            cutNotCopy = true;
            putIntoClipboard(names);
        }

        /// <summary>
        /// Takes the files/dirs from the clipboard and puts them into the current directory.
        /// </summary>
        /// <returns>An integer with the number of top level files/dirs that have been pasted.</returns>
        public int Paste()
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            int count = 0;
            foreach (string path in clipboardPaths)
            {
                try
                {
                    var name = Helpers.PathGetFileName(path);

                    if (cutNotCopy)
                        mountedVFS.MoveFile(path, Helpers.PathCombine(CurrentDir, name));
                    else
                        mountedVFS.CopyFile(path, Helpers.PathCombine(CurrentDir, name));
                    count++;
                }
                catch (Exception)
                {
                    //Log or just ignore
                }
            }
            return count;
        }

        /// <summary>
        /// Deletes the files/dirs with the given names in the current directory.
        /// </summary>
        /// <param name="names">Names of the files/dirs</param>
        /// <returns>An integer with the number of top level files/dirs that have been deleted</returns>
        public int Delete(string[] names)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            int count = 0;
            foreach (string name in names)
            {
                try
                {
                    mountedVFS.DeleteFile(Helpers.PathCombine(CurrentDir, name), true);
                    count++;
                }
                catch (Exception)
                {
                    //Log or just ignore
                }
            }
            return count;
        }

        /// <summary>
        /// Stores the uploaded files in the current directory.
        /// </summary>
        public void Upload(IList<HttpPostedFile> files)
        {
            foreach(HttpPostedFile file in files)
            {
                var path = Helpers.PathCombine(CurrentDir, file.FileName);
                mountedVFS.ImportFile(file.InputStream, path);
            }
        }

        /// <summary>
        /// Sends a file from the current directory to the user,
        /// invoking a Save dialog in the browser.
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <param name="name">The size of the file</param>
        /// <param name="name">The HTTP response object from the server</param>
        public void Download(string name, string size, HttpResponse response)
        {
            try {
                var file = Helpers.PathCombine(CurrentDir, name);
                response.Clear();
                response.ClearHeaders();
                response.ClearContent();
                response.AppendHeader("Content-Disposition", "attachment; filename=\"" + name + "\"");
                response.AppendHeader("Content-Length", size);
                response.AppendHeader("Cache-Control", "private, max-age=0, no-cache");
                response.ContentType = "application/octet-stream";
                response.Flush();
                mountedVFS.ExportFile(file, response.OutputStream);
                response.End();
            }
            catch(Exception) {
                //log or just ignore
            }
        }

        /// <summary>
        /// Searches for the given string and returns the found files.
        /// </summary>
        /// <param name="searchString">String to search for.</param>
        /// <returns>DirectoryEntry Array with the found files.</returns>
        public DirectoryEntry[] Search(string searchString)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            switch (SearchLocation)
            {
                //case SearchLocation.Everywhere:
                //    currentSearchResults = getDirEntryDetails(mountedVFS.Search(searchString, SearchCaseSensitive));
                //    break;
                case SearchLocation.SubFolder:
                    currentSearchResults = getDirEntryDetails(mountedVFS.Search(CurrentDir, searchString, SearchCaseSensitive, true));
                    break;
                case SearchLocation.Folder:
                    currentSearchResults = getDirEntryDetails(mountedVFS.Search(CurrentDir, searchString, SearchCaseSensitive, false));
                    break;
                default:
                    throw new Exception("Invalid \"SearchLocation\" enum value in your Session.");
            }

            return currentSearchResults;
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Tries to move the current directory one directory up/back. 
        /// </summary>
        /// <returns>True if moved, false otherwise</returns>
        public bool MoveBack()
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            var path = ParentDirOf(CurrentDir);
            if (path == CurrentDir)
                return false;

            moveIntoDirectory(path);
            return true;
        }

        /// <summary>
        /// Moves from the current directory into the given directory.
        /// </summary>
        /// <param name="directory">Directory to move into</param>
        /// <param name="completePath">If true, the given path is taken to move into. If false, the current directory is prepended.</param>
        /// <returns>True if moved, false otherwise</returns>
        public bool MoveInto(string directory, bool completePath)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            if (completePath)
                moveIntoDirectory(directory);
            else
                moveIntoDirectory((Helpers.PathCombine(CurrentDir, directory) + '/'));

            //TODO return the value of the actual move operation when changed in JCDFAT
            return true;
        }

        /// <summary>
        /// Returns an array with the files/dirs in the current directory.
        /// </summary>
        /// <returns>The entries in the current directory</returns>
        public DirectoryEntry[] ListCurrentDirectory()
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            var dirList = mountedVFS.ListDirectory(CurrentDir);
            var entries = new DirectoryEntry[dirList.Length];
            for (int i = 0; i < dirList.Length; i++)
            {
                string path = CurrentDir + dirList[i].Name;
                if (dirList[i].IsFolder)
                    path += @"/";
                entries[i] = new DirectoryEntry(dirList[i].Name, path, dirList[i].IsFolder, dirList[i].Size);
            }
            return entries;
        }


        #endregion

        #region Synchro Methods

        //TODO: What if the client closes the page after getting the notificaiton, but before updating page?
        //      Is the session reset anyway then?
        /// <summary>
        /// Checks if the client needs to refresh the page on a VFS event
        /// </summary>
        /// <param name="vfsPath">The path of the affected file</param>
        private void checkIfUpdateNeeded(string vfsPath) {
            if(!updateScheduled) {
                //if vfsPath is directly in mountedVFSpath, pushUpdate()
                string current = Helpers.TrimLastSlash(mountedVFSpath);
                if(vfsPath.StartsWith(current)) {
                    string subPath = vfsPath.Substring(current.Length).TrimStart(new char[] { '/' });
                    if(subPath.IndexOf('/') < 0) {
                        //The affected file is the current directory or lies directly inside of it
                        pushUpdate();
                    }
                }
            }
        }

        /// <summary>
        /// Tells the client that the current view should be updated.
        /// </summary>
        private void pushUpdate() {
            string connectionID;
            Updates.sessionToConnection.TryGetValue(sessionID, out connectionID);
            if(connectionID != null) {
                updateScheduled = true;
                GlobalHost.ConnectionManager.GetHubContext<Updates>().Clients.Client(connectionID).update();
            }
            //else {
                //SignalR could not establish connection, fail silently
            //}
        }

        //The following three are copy-pasted from desktop VFSSession - check them

        public bool LogIn(string userName, string password) {
            try {
                mountedVFS.LogIn(userName, password);
            }
            catch(vfs.exceptions.VFSSynchronizationServerException e) {
                //TODO check the statuscode or so
                throw e;
            }
            this.UserName = userName;
            return true;
        }

        public void LogOut() {
            mountedVFS.LogOut();
            this.UserName = null;
        }

        public bool AddVFS() {
            try {
                mountedVFS.AddVFS();
            }
            catch(vfs.exceptions.VFSSynchronizationServerException e) {
                throw e;
            }
            catch(vfs.exceptions.AlreadySynchronizedVFSException e) {
                throw e;
            }
            return true;
        }

        public bool RemoveVFS() {
            try {
                mountedVFS.RemoveVFS();
            }
            catch(vfs.exceptions.VFSSynchronizationServerException e) {
                throw e;
            }
            return true;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Tries to set the given directory as current directory.
        /// </summary>
        /// <param name="dir">Directory to move into</param>
        private void moveIntoDirectory(string dir)
        {
            CurrentDir = dir;
        }

        /// <summary>
        /// Returns the parent directory of the given path.
        /// </summary>
        /// <param name="path">To calculate the parent directory from</param>
        /// <returns></returns>
        public string ParentDirOf(string path)
        {
            string result;

            if (Helpers.TrimLastSlash(path) == "")
                return path;

            result = Helpers.PathGetDirectoryName(Helpers.TrimLastSlash(path));

            return result;
        }

        /// <summary>
        /// Puts the given file/dir names appended to the current directory into the clipboard.
        /// </summary>
        /// <param name="names">File/dir names to put into the clipboard</param>
        private void putIntoClipboard(string[] names)
        {
            clipboardPaths = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
                clipboardPaths[i] = Helpers.PathCombine(CurrentDir, names[i]);
        }

        /// <summary>
        /// Returns the JCDDirEntries for the files at the given pathes.
        /// </summary>
        /// <param name="pathes">Pahtes to the files to get the details from.</param>
        /// <returns>Array with the details.</returns>
        private DirectoryEntry[] getDirEntryDetails(string[] pathes)
        {
            var dirEntryList = new List<DirectoryEntry>();
            foreach (var path in pathes)
            {
                try
                {
                    var dirEntry = mountedVFS.GetFileDetails(path);
                    dirEntryList.Add(new DirectoryEntry(dirEntry.Name, path, dirEntry.IsFolder, dirEntry.Size));
                }
                catch (Exception)
                {
                    //Log or just ignore
                }
            }
            return dirEntryList.ToArray();
        }

        #endregion

    }
}
