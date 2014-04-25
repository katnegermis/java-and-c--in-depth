using System;
using System.IO;
using BplusDotNet;
using System.Linq;

namespace vfs.core.indexing {
    public class FileIndex : IDisposable {
        const int KEY_LENGTH = 256;
        private SerializedTree stree;

        /// <summary>
        /// Create new FileIndex from a BplusTreeBytes object.
        /// </summary>
        /// <param name="tree"></param>
        public FileIndex(BplusTreeBytes tree) {
            this.stree = new SerializedTree(tree);
        }

        public static FileIndex Initialize(Stream treeFile, Stream dataFile) {
            var tree = BplusTreeBytes.Initialize(treeFile, dataFile, KEY_LENGTH);
            tree.Commit(); // Make sure to write initial data to disk.
            return new FileIndex(tree);
        }

        public static FileIndex Open(Stream treeFile, Stream dataFile) {
            var tree = BplusTreeBytes.ReOpen(treeFile, dataFile);
            return new FileIndex(tree);
        }

        /// <summary>
        /// Initialize a FileIndex from files on the host file system.
        /// </summary>
        /// <param name="treeFileName">Path to the file containing the tree structure.</param>
        /// <param name="dataFileName">Path to the file containing data.</param>
        /// <returns></returns>
        public static FileIndex Initialize(string treeFileName, string dataFileName) {
            Stream treeFile;
            Stream dataFile;
            var filesExisted = File.Exists(treeFileName) && File.Exists(dataFileName);
            // We hope that the files weren't deleted in between the above and the two following lines of code.
            treeFile = File.Open(treeFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            dataFile = File.Open(dataFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            if (!filesExisted) {
                return Initialize(treeFile, dataFile);
            }
            else {
                return Open(treeFile, dataFile);
            }
        }

        /// <summary>
        /// Retrieve stored values from `fileName`.
        /// </summary>
        /// <param name="fileName">Key of key-value pair.</param>
        /// <returns>Value of key-value pair.</returns>
        public IndexedFile[] Get(string fileName, bool caseSensitive) {
            var val = stree.Get(fileName, null, caseSensitive);

            if (val == null) {
                return null;
            }

            if (val is IndexedFile[]) {
                return val as IndexedFile[];
            }

            // Shut up the compiler.
            return null;
        }

        public void Put(string fileName, string path) {
            Put(new IndexedFile(fileName, path));
        }

        public void Put(IndexedFile f) {
            var arr = Get(f.Name, true);

            // Key didn't already exist, insert new array.
            if (arr == null) {
                stree.Set(f.Name, new IndexedFile[] { f });
                return;
            }

            // Key already exists, check whether value already exists
            foreach (var val in arr) {
                if (val == f) {
                    throw new FileAlreadyIndexedException();
                }
            }

            // Value didn't already exist, add it.
            var newArr = new IndexedFile[arr.Length + 1];
            for (int i = 0; i < arr.Length; i += 1) {
                newArr[i] = arr[i];
            }
            newArr[arr.Length] = f;
            stree.Set(f.Name, newArr);
        }

        public void Rename(string fileName, string path, string newName, string newPath) {
            Rename(new IndexedFile(fileName, path), newName, newPath);
        }

        public void Rename(IndexedFile f, string newName, string newPath) {
            Remove(f);
            Put(newName, newPath);
        }

        public void Remove(string fileName, string path) {
            Remove(new IndexedFile(fileName, path));
        }

        public void Remove(IndexedFile f) {
            var vals = Get(f.Name, true);
            if (vals == null) {
                // TODO: possible throw exception here instead.
                return;
            }

            // If there's only one file, remove it completely from tree
            if (vals.Length == 1) {
                stree.RemoveKey(f.Name);
                return;
            }

            // There are multiple files with the same name.
            // Remove only the file the user asked us to remove.
            var newVals = vals.Where(val => { return val != f; }).ToArray();
            stree.Set(f.Name, newVals);
        }

        public void Dispose() {
            Close();
        }

        /// <summary>
        /// Commit, flush data to disk, and close files.
        /// </summary>
        public void Close() {
            this.stree.Commit();
            this.stree.Shutdown();
        }
    }
}
