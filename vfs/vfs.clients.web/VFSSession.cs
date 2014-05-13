using System;
using System.Collections.Generic;
using System.IO;
using vfs.core;
using vfs.common;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

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
    public enum SearchLocation { Folder, SubFolder, Everywhere };

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
        private JCDFAT mountedVFS;

        /// <summary>
        /// The location of the currently open VFS.
        /// </summary>
        public string mountedVFSpath;

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

        private VFSSession(JCDFAT vfs, string path)
        {
            mountedVFS = vfs;
            mountedVFSpath = path;
        }

        #region VFS Methods

        /// <summary>
        /// Makes the call to create the VFS file at the given place and with the given size.
        /// </summary>
        /// <param name="fileToCreate">To create and put the VFS inside</param>
        /// <param name="size">Of the new VFS</param>
        /// /// <returns>A new VFSSession object if the VFS was created successfully or null otherwise</returns>
        public static VFSSession CreateVFS(string fileToCreate, ulong size)
        {
            var vfs = JCDFAT.Create(fileToCreate, size);
            if(vfs != null)
                return new VFSSession(vfs, fileToCreate);
            else
                return null;
        }

        /*/// <summary>
        /// Makes the call to delete the given file if it has a VFS in it.
        /// </summary>
        /// <param name="fileToDelete"></param>
        public static void DeleteVFS(string fileToDelete)
        {
            JCDFAT.Delete(fileToDelete);
        }*/

        /// <summary>
        /// Delete the currently open VFS
        /// </summary>
        public void DeleteVFS() {
            if(mountedVFS == null)
                throw new Exception("No VFS mounted!");

            string VFSpath = mountedVFSpath;
            Close();
            JCDFAT.Delete(VFSpath);
        }

        /// <summary>
        /// Makes the call to open the given VFS file.
        /// Then returns a new VFSSession object.
        /// </summary>
        /// <param name="fileToOpen">The file to open</param>
        /// <returns>A new VFSSession object if the VFS was opened successfully or null otherwise</returns>
        public static VFSSession OpenVFS(string fileToOpen)
        {
            var vfs = JCDFAT.Open(fileToOpen);
            if (vfs != null)
                return new VFSSession(vfs, fileToOpen);
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

        /*/// <summary>
        /// Creates a fiel with the given name and size in the current directory of the VFS.
        /// </summary>
        /// <param name="fileName">Name of the new file</param>
        /// <param name="size">Size of the new file</param>
        public void CreateFile(string fileName, ulong size)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            mountedVFS.CreateFile(Helpers.PathCombine(CurrentDir, fileName), size, false);
        }*/


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
                //try
                //{
                    var name = Helpers.PathGetFileName(path);

                    if (cutNotCopy)
                        mountedVFS.MoveFile(path, Helpers.PathCombine(CurrentDir, name));
                    else
                        mountedVFS.CopyFile(path, Helpers.PathCombine(CurrentDir, name));
                    count++;
                //}
                //catch (Exception)
                //{
                    //Log or just ignore
                //}
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

        public int Import(string[] files, string targetDir)
        {
            int count = 0;
            foreach (string file in files)
            {
                var name = new FileInfo(file).Name;
                mountedVFS.ImportFile(file, Helpers.PathCombine(targetDir, name));
                count++;
            }
            return count;
        }

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

        /*/// <summary>
        /// Makes the drag and drop operation with the given files/dirs from the current directory to the target.
        /// If set so, the given files are removed afterwards.
        /// </summary>
        /// <param name="names">Files/dirs to drag/drop.</param>
        /// <param name="removeAfterwards">If set to true, a Move operation is done, otherwise a Copy operation.</param>
        /// <returns>The number of moved files/dirs.</returns>
        public int DragDrop(string[] names, string dir, bool removeAfterwards)
        {
            if (mountedVFS == null)
                throw new Exception("No VFS mounted!");

            int count = 0;
            foreach (string name in names)
            {
                try
                {
                    var path = Helpers.PathCombine(CurrentDir, name);
                    var targetDir = Helpers.PathCombine(CurrentDir, dir + "/");
                    if (removeAfterwards)
                        mountedVFS.MoveFile(path, Helpers.PathCombine(targetDir, name));
                    else
                        mountedVFS.CopyFile(path, Helpers.PathCombine(targetDir, name));

                    count++;
                }
                catch (Exception)
                {
                    //Log or just ignore
                }
            }
            return count;
        }*/

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
                case SearchLocation.Everywhere:
                    currentSearchResults = getDirEntryDetails(mountedVFS.Search(searchString, SearchCaseSensitive));
                    break;
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
