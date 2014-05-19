using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using vfs.common;

namespace vfs.core.tests {

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class JCDVFSIndexingTests {
        private const uint MB1 = 1000000;
        private const uint MB50 = 50 * MB1;

        [TestMethod]
        public void TestFindSingleFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            // Create initial file.
            var filePath = "/dir/file";
            var fileName = Helpers.PathGetFileName(filePath);
            vfs.CreateFile(filePath, MB1, true);

            // Test
            var files = vfs.Search(fileName, true);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(filePath, files[0]);
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestDeleteSingleFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            // Create initial file.
            var filePath = "/dir/file";
            var fileName = Helpers.PathGetFileName(filePath);
            vfs.CreateFile(filePath, MB1, true);
            vfs.DeleteFile(filePath, false);

            // Test
            // Make sure that file can't be found
            var noFile = vfs.Search(fileName, true);
            Assert.AreEqual(0, noFile.Length);
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestDeleteMultipleFiles() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            var fileName = "file";
            int numFiles = 10;
            var files = new string[numFiles];
            // Create `numFiles` in different directories.
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = string.Format("/dir{0}/{1}", i, fileName);
                vfs.CreateFile(files[i], 1, true);
                vfs.DeleteFile(files[i], false);
            }

            // Test
            var noFiles = vfs.Search(fileName, true);
            Assert.AreEqual(0, noFiles.Length);
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestDeleteSingleDirectory() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            // Create initial file.
            var dirPath = "/dir/directory";
            var dirName = Helpers.PathGetFileName(dirPath);
            vfs.CreateDirectory(dirPath, true);
            vfs.DeleteFile(dirPath, true);

            // Test
            // Make sure that file can't be found
            var noFile = vfs.Search(dirName, true);
            Assert.AreEqual(0, noFile.Length);
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestDeleteDirectoryTree() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            // Create initial directory
            var dirPath = "/dir/";
            var fileName = "file";
            int numFiles = 10;
            var files = new string[numFiles];
            vfs.CreateDirectory(dirPath, false);
            // Create `numFiles` in `dirPath`.
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = Helpers.PathCombine(dirPath, fileName + i);
                vfs.CreateFile(files[i], 1, false);
            }
            vfs.DeleteFile(dirPath, true);

            // Test
            // Make sure that directory can't be found.
            var noFile = vfs.Search(dirPath, true);
            Assert.AreEqual(0, noFile.Length);

            // Make sure that subfiles can't be found.
            for (int i = 0; i < numFiles; i += 1) {
                Assert.AreEqual(0, vfs.Search(files[i], true).Length);
            }
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestFindMultipleFiles() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
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
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestFindMovedFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
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
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestFindMovedToNewDirectory() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
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
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestMoveDirectoryTree() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            var rootDir = "/root/dir/";
            vfs.CreateDirectory(rootDir, true);

            // Create initial files.
            var numFiles = 20;
            var files = new string[numFiles];
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = Helpers.PathCombine(rootDir, "file" + i);
                vfs.CreateFile(files[i], 1, false);
            }

            // Move old root dir.
            var newRootDir = "/new/root/dir/";
            vfs.CreateDirectory(Helpers.PathGetDirectoryName(newRootDir), true);
            vfs.MoveFile(rootDir, newRootDir);

            // Create list of new file names.
            var newFiles = new string[numFiles];
            for (int i = 0; i < numFiles; i += 1) {
                newFiles[i] = Helpers.PathCombine(newRootDir, "file" + i);
            }

            // Test
            // Make sure that the old files can't be found.
            for (int i = 0; i < numFiles; i += 1) {
                var tmpFileName = Helpers.PathGetFileName(files[i]);
                var foundFilesPaths = vfs.Search(tmpFileName, true);

                // Make sure that only one file is found.
                Assert.AreEqual(1, foundFilesPaths.Length);

                // Make sure that file has the new path.
                Assert.AreNotEqual(files[i], foundFilesPaths[0]);
            }
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestRenamedFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
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
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestRenameDirectoryTree() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            var rootDir = "/root/dir/";
            vfs.CreateDirectory(rootDir, true);

            // Create initial files.
            var numFiles = 20;
            var fileName = "file";
            var files = new string[numFiles];
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = Helpers.PathCombine(rootDir, fileName + i);
                vfs.CreateFile(files[i], 1, false);
            }

            // Move old root dir.
            var newDirName = "newdir";
            var newRootDir = Path.Combine("/root/", newDirName);
            vfs.RenameFile(rootDir, newDirName);

            // Create list of new file names.
            var newFiles = new string[numFiles];
            for (int i = 0; i < numFiles; i += 1) {
                newFiles[i] = Helpers.PathCombine(newRootDir, fileName + i);
            }

            // Test
            for (int i = 0; i < numFiles; i += 1) {
                var tmpFileName = Helpers.PathGetFileName(files[i]);
                var foundFilesPaths = vfs.Search(tmpFileName, true);

                // Make sure that only one file is found.
                Assert.AreEqual(1, foundFilesPaths.Length);

                // Make sure that file has the new path.
                Assert.AreNotEqual(files[i], foundFilesPaths[0]);
            }
            CloseJCDFAT(vfs, testName);
        }

        [TestMethod]
        public void TestSearchNonRecursive() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = CreateVFS(testName);
            var fileName = "file";
            int numFiles = 10;
            var files = new string[numFiles];
            // Create `numFiles` in different directories.
            for (int i = 0; i < numFiles; i += 1) {
                files[i] = string.Format("/dir{0}/{1}", i, fileName);
                vfs.CreateFile(files[i], 1, true);
            }

            // Test
            // Verify that files can be found recursively.
            var allFiles = vfs.Search("/", fileName, true, true);
            Assert.AreEqual(numFiles, allFiles.Length);
            
            // Verify that files aren't found nonrecursively.
            var noFiles = vfs.Search("/", fileName, true, false);
            Assert.AreEqual(0, noFiles.Length);
            CloseJCDFAT(vfs, testName);
        }

        private JCDFAT CreateJCDFAT(string testName, uint size) {
            TestHelpers.DeleteFiles(new string[] { testName });
            return JCDFAT.Create(testName, size);
        }

        private JCDFAT CreateVFS(string testName) {
            return CreateJCDFAT(testName, MB50);
        }

        private void CloseJCDFAT(JCDFAT vfs, string testName) {
            vfs.Close();
           TestHelpers.DeleteFiles(new string[] { testName });
        }
    }
}
