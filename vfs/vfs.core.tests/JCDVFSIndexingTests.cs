using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.tests {

    [TestClass]
    public class JCDVFSIndexingTests {
        private const uint MB1 = 1000000;
        private const uint MB50 = 50 * MB1;

        [TestMethod]
        public void TestFindSingleFile() {
            // Set up
            var vfs = CreateVFS("find_single_file");
            // Create initial file.
            var filePath = "/dir/file";
            var fileName = Helpers.PathGetFileName(filePath);
            vfs.CreateFile(filePath, MB1, true);

            // Test
            var files = vfs.Search(fileName, true);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(filePath, files[0]);
        }

        [TestMethod]
        public void TestFindMultipleFiles() {
            // Set up
            var vfs = CreateVFS("find_multiple_files");
            var fileName = "file";
            int numFiles = 10;
            var files = new string[numFiles];
            // Create `numFiles` in different directories.
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = string.Format("/dir{0}/{1}", i, fileName);
                vfs.CreateFile(files[i], 1, true);
            }

            // Test
            var foundFiles = vfs.Search(fileName, true);
            Assert.AreEqual(numFiles, foundFiles.Length);
            for (int i = 0; i < numFiles; i += 1) {
                Assert.AreEqual(files[i], foundFiles[i]);
            }
        }

        [TestMethod]
        public void TestFindMovedFile() {
            // Set up
            var vfs = CreateVFS("find_moved_file");
            // Create initial file.
            var dir = "/dir/";
            var fileName = "file";
            var filePath = Helpers.PathCombine(dir, fileName);
            vfs.CreateFile(filePath, MB1, true);

            // Move file in to same directory, with different name.
            var newFileName = "newfile";
            var newFilePath = Helpers.PathCombine(dir, newFileName);
            vfs.MoveFile(filePath, newFilePath);

            // Test
            // Make sure we don't find the old file.
            var noFiles = vfs.Search(fileName, true);
            Assert.AreEqual(0, noFiles.Length);
            
            // Make sure we find new file.
            var files = vfs.Search(newFileName, true);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(newFilePath, files[0]);
        }

        [TestMethod]
        public void TestFindMovedToNewDirectoryFile() {
            // Set up
            var vfs = CreateVFS("find_moved_to_new_directory_file");
            // Create initial file.
            var filePath = "/dir/file";
            var fileName = Helpers.PathGetFileName(filePath);
            vfs.CreateFile(filePath, MB1, true);

            // Create new directory for file and move it there.
            var newFilePath = "/dir2/newfile";
            var newFileName = Helpers.PathGetFileName(newFilePath);
            var newDirectoryName = Helpers.PathGetDirectoryName(newFilePath);
            vfs.CreateDirectory(newDirectoryName, true);
            vfs.MoveFile(filePath, newFilePath);

            // Test
            // Make sure we don't find the old file.
            var noFiles = vfs.Search(fileName, true);
            Assert.AreEqual(0, noFiles.Length);

            // Make sure we find new file.
            var files = vfs.Search(newFileName, true);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(newFilePath, files[0]);
        }

        [TestMethod]
        public void TestRenamedFile() {
            // Set up
            var vfs = CreateVFS("find_renamed_file");
            // Create initial file.
            var filePath = "/dir/file";
            var fileName = Helpers.PathGetFileName(filePath);
            vfs.CreateFile(filePath, MB1, true);

            // Move rename file.
            var newFilePath = "/dir/newfile";
            var newFileName = Helpers.PathGetFileName(newFilePath);
            vfs.RenameFile(filePath, newFileName);

            // Test
            // Make sure the old file can't be found.
            var noFiles = vfs.Search(fileName, true);
            Assert.AreEqual(0, noFiles.Length);

            // Make sure we find new file.
            var files = vfs.Search(newFileName, true);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(newFilePath, files[0]);
        }

        private JCDFAT CreateVFS(string testName, uint size) {
            DeleteFiles(new string[] { testName });
            return JCDFAT.Create(testName, size);
        }

        private JCDFAT CreateVFS(string testName) {
            return CreateVFS(testName, MB50);
        }

        private void DeleteFiles(string[] files) {
            foreach (var file in files) {
                try {
                    File.Delete(file);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }
    }
}
