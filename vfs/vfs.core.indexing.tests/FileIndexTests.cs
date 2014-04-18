using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using vfs.core.indexing;

namespace vfs.core.indexing.tests {
    [TestClass]
    public class FileIndexTests {
        private delegate void CorrectFileFound(MyFile x, FileIndex fi);

        [TestMethod]
        public void TestSingleFileInsertion() {
            /// Set up
            var fileIndex = GetIndex("single_file_insertion");
            var file = new MyFile("file", "/path/file");
            fileIndex.Put(file);

            /// Test
            Assert.AreEqual(file, fileIndex.Get(file.Name)[0]); // Make sure file is there.
        }

        [TestMethod]
        [ExpectedException(typeof(FileAlreadyIndexedException), "A file can't be added twice.")]
        public void TestDuplicateFileInsertion() {
            /// Set up
            var fileIndex = GetIndex("duplicate_file_insertion");
            var file = new MyFile("file", "/path/file");
            fileIndex.Put(file);
            fileIndex.Put(file);

            /// Test
            Assert.AreEqual(file, fileIndex.Get(file.Name)[0]); // Make sure file is there.
        }

        [TestMethod]
        public void TestMultipleFileInsertionOneFileName() {
            /// Set up
            var fileIndex = GetIndex("multiple_file_insertion_one_file_name");
            string fileName = "file";
            var files = GenerateFilesArray(5, fileName, true);
            foreach (var file in files) {
                fileIndex.Put(file);
            }

            /// Test
            var vals = fileIndex.Get(fileName);
            Assert.IsTrue(FilesAreAllThere(files, vals));
        }

        [TestMethod]
        public void TestMultipleFileInsertionMultipleFileNames() {
            /// Set up
            var fileIndex = GetIndex("multiple_file_insertion_multiple_file_names");
            var files = GenerateFilesArray(5, "file", false);
            foreach (var file in files) {
                fileIndex.Put(file);
            }

            CorrectFileFound correctFile = (MyFile f, FileIndex fIndex) => {
                var val = fIndex.Get(f.Name);
                Assert.AreEqual(val.Length, 1); // Make sure that only one file is found.
                Assert.AreEqual(f, val[0]); // Make sure that the file is the correct file.
            };

            /// Test
            // Verify that all files can be correctly retrieved.
            for (int i = 0; i < files.Length; i += 1) {
                correctFile(files[i], fileIndex);
            }
        }

        [TestMethod]
        public void TestSingleFileRetrieval() {
            var fileIndex = GetIndex("single_file_retrieval");
            fileIndex.Put(new MyFile("file", "/var/file"));
            var vals = fileIndex.Get("file");
            Assert.AreEqual(1, vals.Length);
        }

        [TestMethod]
         public void TestNonExistentFile() {
            /// Set up
            var fileIndex = GetIndex("non_existent_file");

            /// Test
            var vals = fileIndex.Get("non existent file");
            Assert.IsNull(vals); // Make sure file isn't there.
        }

        [TestMethod]
        public void TestMultipleFileRetrieval() {
            /// Set up
            var fileIndex = GetIndex("multiple_file_retrieval");
            var fileName = "file";
            var files = GenerateFilesArray(5, fileName, true);
            foreach (var file in files) {
                fileIndex.Put(file);
            }

            /// Test
            var vals = fileIndex.Get("file");
            Assert.IsTrue(FilesAreAllThere(files, vals));
        }

        [TestMethod]
        public void TestRemoveNonExistentFile() {
            /// Set up
            var fileIndex = GetIndex("non_existent_file_removal");
            var file = new MyFile("file", "/path/file");
            var file2 = new MyFile("file2", "/path/file2");
            fileIndex.Put(file);

            /// Test
            Assert.AreEqual(file, fileIndex.Get(file.Name)[0]); // Make sure `file` is there.
            fileIndex.Remove(file2); // Try to remove file that wasn't added.
            Assert.AreEqual(file, fileIndex.Get(file.Name)[0]); // Make sure `file` is still there.
        }

        [TestMethod]
        public void TestSingleFileSingleRemoval() {
            /// Set up
            var fileIndex = GetIndex("single_file_removal");
            var file = new MyFile("file", "/path/file");
            fileIndex.Put(file);

            /// Test
            Assert.AreEqual(file, fileIndex.Get(file.Name)[0]); // Make sure file is there.
            fileIndex.Remove(file);
            Assert.IsNull(fileIndex.Get(file.Name)); // Make sure file has been removed.
        }

        [TestMethod]
        public void TestMultipleFilesSingleRemoval() {
            /// Set up
            var fileIndex = GetIndex("multiple_file_single_removal");
            var fileName = "file";
            var files = GenerateFilesArray(5, fileName, true);
            // Insert `files` in to index.
            foreach (var f in files) {
                fileIndex.Put(f);
            }

            var index = 2;
            var file = files[index];
            // Make sure file has been inserted.
            Assert.AreEqual(file, fileIndex.Get(fileName)[index]);

            // Remove `file` from `index`.
            fileIndex.Remove(file);

            // Remove `file` from `files`.
            files = files.Where(x => { return x != file; }).ToArray();

            /// Test
            var vals = fileIndex.Get(fileName);
            // Make sure that only files that weren't deleted are still there.
            Assert.IsTrue(FilesAreAllThere(files, vals));
        }

        [TestMethod]
        public void TestMultipleFilesMultipleRemoval() {
            /// Set up
            var fileIndex = GetIndex("multiple_file_multiple_removal");
            var fileName = "file";
            var files = GenerateFilesArray(5, fileName, true);
            // Insert `files` in to index.
            foreach (var f in files) {
                fileIndex.Put(f);
            }

            // Remove files from `index` and `files`.
            // The files removed will be those with paths `/var/file{1,3,5}`
            // because indexes in `files` are "refreshed" in each run.
            var indexes = new int[] { 0, 1, 2 };
            foreach (var index in indexes) {
                var file = files[index];
                // Make sure file has been inserted.
                Assert.AreEqual(file, fileIndex.Get(fileName)[index]);

                // Remove `file` from `index`.
                fileIndex.Remove(file);

                // Remove `file` from `files`.
                // This is where the indexes are "refreshed".
                files = files.Where(x => { return x != file; }).ToArray();
            }

            /// Test
            var vals = fileIndex.Get(fileName);
            // Make sure that only files that weren't deleted are still there.
            Assert.IsTrue(FilesAreAllThere(files, vals));
        }

        [TestMethod]
        public void TestMassiveMultipleFilesMultipleRemoval() {
            /// Set up
            var fileIndex = GetIndex("massive_multiple_file_multiple_removal");
            var fileName = "file";
            int numFiles = 10000;
            var files = GenerateFilesArray(numFiles, fileName, false);
            // Insert `files` in to index.
            foreach (var f in files) {
                fileIndex.Put(f);
            }

            // Remove every other file from `index` and `files`.
            for (var i = 0; i < numFiles; i += 2) {
                fileIndex.Remove(files[i]);
            }

            // Create an array with every other file from `files` in it.
            // This should yield an array with the files that weren't deleted
            // in the loop above.
            var newFiles = new MyFile[numFiles / 2];
            for (int i = 0; i < newFiles.Length; i += 1) {
                newFiles[i] = files[i * 2 + 1];
            }

            /// Test
            // Make sure that only files that weren't deleted are still there.
            for (int i = 0; i < newFiles.Length; i += 1) {
                var file = newFiles[i];
                var vals = fileIndex.Get(file.Name);
                Assert.AreEqual(file, vals[0]);
            }
        }

        private bool FilesAreAllThere(MyFile[] arr1, MyFile[] arr2) {
            if (arr1.Length != arr2.Length) {
                return false;
            }

            // Make sure that all of the files are retrived back (in same order).
            for (int i = 0; i < arr1.Length; i += 1) {
                if (!(arr1[i] == arr2[i])) {
                    return false;
                }
            }
            return true;
        }

        private FileIndex GetIndex(string testName) {
            // Expects test names to be unique, so that they will operate on different files.
            // This is necessary because the tests are run in parallel.
            var files = new string[] {String.Format("{0}_tree", testName),
                                      String.Format("{0}_data", testName),};
            DeleteFiles(files);
            return new FileIndex(files[0], files[1]);
        }

        private void DeleteFiles(string[] files) {
            foreach (var file in files) {
                try {
                    File.Delete(file);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }

        private MyFile[] GenerateFilesArray(int numElements, string fileName, bool sameName) {
            var files = new MyFile[numElements];
            for (var i = 0; i < numElements; i += 1) {
                var name = sameName ? fileName : fileName + i;
                // Make sure that files aren't exactly the same, i.e. same name AND path.
                var file = new MyFile(name, String.Format("/path/{0}/{1}", i, name));
                files[i] = file;
            }
            return files;
        }
    }
}

