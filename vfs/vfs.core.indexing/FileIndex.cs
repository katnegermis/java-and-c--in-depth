using System;
using System.IO;
using BplusDotNet;
using System.Linq;

namespace vfs.core.indexing {
    public class FileIndex : IDisposable {
        const int KEY_LENGTH = 256;
        string treeFileName = @"C:/treefile.txt";
        string dataFileName = @"C:/blockfile.txt";
        BplusTreeBytes tree;
        SerializedTree stree;

        public FileIndex(string treeFileName, string dataFileName) {
            this.treeFileName = treeFileName;
            this.dataFileName = dataFileName;
            Initialize();
        }

        public FileIndex() {
            Initialize();
        }

        private void Initialize() {
            Stream treeFile;
            Stream blockFile;
            var filesExisted = File.Exists(treeFileName) && File.Exists(dataFileName);
            // We hope that the files weren't deleted in between the above and the two following lines of code.
            treeFile = File.Open(treeFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            blockFile = File.Open(dataFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            
            if (filesExisted) {
                this.tree = BplusTreeBytes.ReOpen(treeFile, blockFile);
            } else {
                this.tree = BplusTreeBytes.Initialize(treeFile, blockFile, KEY_LENGTH);
                // Make sure to write initialized data structure to file.
                this.tree.Commit();
            }
            this.stree = new SerializedTree((IByteTree)tree);
        }

        public IndexedFile[] Get(string fileName) {
            var val = stree.Get(fileName, null);

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
            var arr = Get(f.Name);

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
            //stree.Commit();
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
            var vals = Get(f.Name);
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
