using System;
using System.Collections.Generic;
using System.IO;
using vfs.core;
using vfs.synchronizer.client;
using vfs.common;

namespace vfs.clients.desktop
{
    /// <summary>
    /// Class that represents an entry into the VFS, directory or file.
    /// Serves as container between controller and view classes.
    /// </summary>
    public class DirectoryEntry
    {
        public bool IsFolder { get; private set; }
        public string Name { get; private set; }
        public string Path { get; private set; }
        public ulong Size { get; private set; }

        public DirectoryEntry(string name, string path, bool isFolder, ulong size)
        {
            this.Name = name;
            this.Path = path;
            this.IsFolder = isFolder;
            this.Size = size;
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
        /// The currently mounted VFSSynchronizer.
        /// If none is mounted, then null.
        /// </summary>
        private JCDVFSSynchronizer vfsSynchronizer;

        /// <summary>
        /// Returns whether the VFSSynchronizer is logged in or not
        /// </summary>
        public bool IsLoggedIn
        {
            get
            {
                return (vfsSynchronizer != null && vfsSynchronizer.LoggedIn());
            }
        }

        /// <summary>
        /// Returns the userName of the logged in user.
        /// If none is logged in it return null.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The current directory we are in.
        /// </summary>
        public string CurrentDir
        {
            get
            {
                return vfsSynchronizer != null ? vfsSynchronizer.GetCurrentDirectory() : null;
            }
            private set
            {
                vfsSynchronizer.SetCurrentDirectory(value);
            }
        }

        /// <summary>
        /// The free space of the mounted VFS.
        /// </summary>
        public ulong FreeSpace
        {
            get
            {
                return vfsSynchronizer != null ? vfsSynchronizer.FreeSpace() : 0;
            }
        }

        /// <summary>
        /// The occupied space of the mounted VFS.
        /// </summary>
        public ulong OccupiedSpace
        {
            get
            {
                return vfsSynchronizer != null ? vfsSynchronizer.OccupiedSpace() : 0;
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
        /// Boolean that represents whether a search is done case sensitive or insensitive.
        /// </summary>
        public bool SearchCaseSensitive = true;

        private VFSSession(JCDVFSSynchronizer vfsSynchronizer)
        {
            this.vfsSynchronizer = vfsSynchronizer;
        }

        #region VFS Methods

        /// <summary>
        /// Makes the call to create the VFS file at the given place and with the given size.
        /// </summary>
        /// <param name="fileToCreate">To create and put the VFS inside</param>
        /// <param name="size">Of the new VFS</param>
        public static void CreateVFS(string fileToCreate, ulong size)
        {
            JCDVFSSynchronizer.Create(typeof(JCDFAT), fileToCreate, size).Close();
        }

        /// <summary>
        /// Makes the call to delete the given file if it has a VFS in it.
        /// </summary>
        /// <param name="fileToDelete"></param>
        public static void DeleteVFS(string fileToDelete)
        {
            JCDVFSSynchronizer.Delete(typeof(JCDFAT), fileToDelete);
        }

        /// <summary>
        /// Makes the call to open the given VFS file.
        /// </summary>
        /// <param name="fileToOpen">The file to open</param>
        /// <returns>True if opened successfully, false otherwise</returns>
        public static VFSSession OpenVFS(string fileToOpen)
        {
            var vfsSynchronizer = JCDVFSSynchronizer.Open(typeof(JCDFAT), fileToOpen);
            if (vfsSynchronizer != null)
                return new VFSSession(vfsSynchronizer);
            else
                return null;
        }

        /// <summary>
        /// Closes the mounted VFS object.
        /// </summary>
        public void Close()
        {
            vfsSynchronizer.Close();
            vfsSynchronizer = null;
        }

        #endregion



        #region Core Methods

        /// <summary>
        /// Creates a directory with the given name in the current directory of the VFS.
        /// </summary>
        /// <param name="dirName">Name of the new directory</param>
        public void CreateDir(string dirName)
        {
            vfsSynchronizer.CreateDirectory(Helpers.PathCombine(CurrentDir, dirName), false);
        }

        /// <summary>
        /// Creates a fiel with the given name and size in the current directory of the VFS.
        /// </summary>
        /// <param name="fileName">Name of the new file</param>
        /// <param name="size">Size of the new file</param>
        public void CreateFile(string fileName, ulong size)
        {
            vfsSynchronizer.CreateFile(Helpers.PathCombine(CurrentDir, fileName), size, false);
        }

        /// <summary>
        /// Renames the file/dir with the given name in the current directory to the new name.
        /// </summary>
        /// <param name="oldName">Name of the file/dir to rename</param>
        /// <param name="newName">New name to set</param>
        public void Rename(string oldName, string newName)
        {
            vfsSynchronizer.RenameFile(Helpers.PathCombine(CurrentDir, oldName), newName);
        }

        /// <summary>
        /// Copies the files/dirs with the given names in the current directory into the clipboard.
        /// </summary>
        /// <param name="names">Names of the files/dirs</param>
        public void Copy(string[] names)
        {
            cutNotCopy = false;
            putIntoClipboard(names);
        }

        /// <summary>
        /// Cuts the files/dirs with the given names in the current directory into the clipboard.
        /// </summary>
        /// <param name="names">Names of the files/dirs</param>
        public void Cut(string[] names)
        {
            cutNotCopy = true;
            putIntoClipboard(names);
        }

        /// <summary>
        /// Takes the files/dirs from the clipboard and puts them into the current directory.
        /// </summary>
        /// <returns>An integer with the number of top level files/dirs that have been pasted.</returns>
        public int Paste()
        {
            int count = 0;
            foreach (string path in clipboardPaths)
            {
                try
                {
                    var name = Helpers.PathGetFileName(path);

                    if (cutNotCopy)
                        vfsSynchronizer.MoveFile(path, Helpers.PathCombine(CurrentDir, name));
                    else
                        vfsSynchronizer.CopyFile(path, Helpers.PathCombine(CurrentDir, name));
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
            int count = 0;
            foreach (string name in names)
            {
                try
                {
                    vfsSynchronizer.DeleteFile(Helpers.PathCombine(CurrentDir, name), true);
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
        /// Imports the given files/dirs from the host file system into the current directory.
        /// </summary>
        /// <param name="files">File/dir paths to import</param>
        /// <param name="targetDir">Dir to import the files/dirs to.</param>
        /// <returns>The number of top level files/dirs that have been imported</returns>
        public int Import(string[] files, string targetDir)
        {
            int count = 0;
            foreach (string file in files)
            {
                var name = new FileInfo(file).Name;
                vfsSynchronizer.ImportFile(file, Helpers.PathCombine(targetDir, name));
                count++;
            }
            return count;
        }

        /// <summary>
        /// Exports the files/dirs with the given names from the current directory to the target path in the host file system.
        /// </summary>
        /// <param name="names">Names of the files/dirs to export</param>
        /// <param name="targetPath">Path to export the files/dirs to</param>
        /// <returns>The number of top level files/dirs that have been exported</returns>
        public int Export(string[] names, string targetPath)
        {
            int count = 0;
            foreach (string name in names)
            {
                try
                {
                    var file = Helpers.PathCombine(CurrentDir, name);
                    vfsSynchronizer.ExportFile(file, targetPath);
                }
                catch (Exception)
                {
                    //log or just ignore
                }
                count++;
            }
            return count;
        }

        /// <summary>
        /// Makes the drag and drop operation with the given files/dirs from the current directory to the target.
        /// If set so, the given files are removed afterwards.
        /// </summary>
        /// <param name="names">Files/dirs to drag/drop.</param>
        /// <param name="removeAfterwards">If set to true, a Move operation is done, otherwise a Copy operation.</param>
        /// <returns>The number of moved files/dirs.</returns>
        public int DragDrop(string[] names, string dir, bool removeAfterwards)
        {
            int count = 0;
            foreach (string name in names)
            {
                try
                {
                    var path = Helpers.PathCombine(CurrentDir, name);
                    var targetDir = Helpers.PathCombine(CurrentDir, dir + "/");
                    if (removeAfterwards)
                        vfsSynchronizer.MoveFile(path, Helpers.PathCombine(targetDir, name));
                    else
                        vfsSynchronizer.CopyFile(path, Helpers.PathCombine(targetDir, name));

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
        /// Searches for the given string and returns the found files.
        /// </summary>
        /// <param name="searchString">String to search for.</param>
        /// <returns>DirectoryEntry Array with the found files.</returns>
        public DirectoryEntry[] Search(string searchString)
        {
            switch (SearchLocation)
            {
                case SearchLocation.Everywhere:
                    return getDirEntryDetails(vfsSynchronizer.Search(searchString, SearchCaseSensitive));
                case SearchLocation.SubFolder:
                    return getDirEntryDetails(vfsSynchronizer.Search(CurrentDir, searchString, SearchCaseSensitive, true));
                case SearchLocation.Folder:
                    return getDirEntryDetails(vfsSynchronizer.Search(CurrentDir, searchString, SearchCaseSensitive, false));
                default:
                    throw new Exception("Invalid \"SearchLocation\" enum value in your Session.");
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Tries to move the current directory one directory up/back. 
        /// </summary>
        /// <returns>True if moved, false otherwise</returns>
        public bool MoveBack()
        {
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
            var dirList = vfsSynchronizer.ListDirectory(CurrentDir);
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


        public bool Register(string userName, string password)
        {
            //var result = vfsSynchronizer.Register(userName, password);
            //TODO check the statusCode or so
            this.UserName = userName;
            return true;
        }

        public bool LogIn(string userName, string password)
        {
            var result = vfsSynchronizer.LogIn(userName, password);
            //TODO check the statuscode or so
            this.UserName = userName;
            return true;
        }

        public void LogOut()
        {
            vfsSynchronizer.LogOut();
            this.UserName = null;
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
                    var dirEntry = vfsSynchronizer.GetFileDetails(path);
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
