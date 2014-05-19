using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using vfs.exceptions;
using vfs.common;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
namespace vfs.core.tests
{
    [ExcludeFromCodeCoverage]
    [TestClass()]
    public class UnmountedJCDFATTests
    {

        #region Create Tests

        [TestMethod()]
        public void CreateNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFileName = TestHelpers.GetTestFileName(testName);

            // Test
            var testVFS = JCDFAT.Create(testFileName, InternalHelpers.MB50);
            Assert.IsNotNull(testVFS);
            Assert.IsTrue(File.Exists(testFileName));

            InternalHelpers.CloseJCDFAT(testVFS, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(DirectoryNotFoundException),
         "A path to an invalid file(in this case a directory) was discovered.")]
        public void CreateWithInvalidPathTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            var nonExistentFolder = "non_existent_folder";

            // Test
            var testVFS = JCDFAT.Create(Path.Combine(nonExistentFolder, testFile), InternalHelpers.MB5);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidSizeException),
         "A size of UInt64.MaxValue was rejected.")]
        public void CreateWithSizeTooBigTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);

            // Test
            JCDFAT.Create(testFile, UInt64.MaxValue);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidSizeException),
         "A size of 0 was rejected with an unspecified exception.")]
        public void CreateWithSizeTooSmallTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            TestHelpers.DeleteFiles(testFile);

            // Test
            var testVFS = JCDFAT.Create(testFile, 0);
        }

        [TestMethod()]
        [ExpectedException(typeof(FileAlreadyExistsException),
         "An existing file was discovered.")]
        public void CreateWithFileExistingTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            var stream = File.Create(testFile);
            stream.Flush();
            stream.Close();

            // Test
            var testVFS = JCDFAT.Create(testFile, InternalHelpers.MB50);
        }

        #endregion

        #region Open Tests

        [TestMethod()]
        public void OpenNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfsFileName = TestHelpers.GetTestFileName(testName);
            var vfs = JCDFAT.Create(vfsFileName, InternalHelpers.MB50);
            vfs.Close();

            // Test
            vfs = JCDFAT.Open(vfsFileName);

            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
       "The invalid (empty) file was discovered.")]
        public void OpenInvalidFileTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            var stream = File.CreateText(testFile);
            stream.Write("This is not a valid VFS file!");
            stream.Flush();
            stream.Close();

            // Test
            var testVFS = JCDFAT.Open(testFile);
            Assert.IsNull(testVFS);
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "A path to an invalid (non-existent) file was discovered.")]
        public void OpenWithInvalidPathTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);

            // Test
            var testVFS = JCDFAT.Open(testFile);
            Assert.IsNull(testVFS);
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        #endregion

        #region Delete Tests

        [TestMethod()]
        public void DeleteNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFileName = TestHelpers.GetTestFileName(testName);
            InternalHelpers.CreateJCDFAT(testName).Close();

            // Test
            JCDFAT.Delete(testFileName);
            Assert.IsFalse(File.Exists(testName));
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "The fact that the file is no VFS was discovered.")]
        public void DeleteNoVFSFileTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            var stream = File.CreateText(testFile);
            stream.Write("This is not a valid VFS file!");
            stream.Flush();
            stream.Close();

            // Test
            JCDFAT.Delete(testFile);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file is not existing was discovered.")]
        public void DeleteNotExistingFileTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = TestHelpers.GetTestFileName(testName);
            
            // Test
            JCDFAT.Delete(testFile);
        }


        #endregion

    }

    [ExcludeFromCodeCoverage]
    [TestClass()]
    public class MountedJCFVFSTests
    {

        #region Close Tests

        [TestMethod()]
        public void CloseNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            
            // Test
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        #endregion

        #region Size Tests

        [TestMethod()]
        public void SizeTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;

            // JCDFAT is created in multiples of blockSize + x * (blockSize + 4MB)
            // The first block is for meta data, x * blockSize is used for FAT tables
            // and x * 4MB is used for data.
            var blockSize = 1UL << 12; // block size of 4K.

            // Test
            var vfs1 = InternalHelpers.CreateJCDFAT(testName, JCDFAT.globalMinFSSize);
            Assert.AreEqual(blockSize + JCDFAT.globalMinFSSize + blockSize, vfs1.Size());
            InternalHelpers.CloseJCDFAT(vfs1, testName);


            // This vfs is 4MB + 2 blocks + 1, and should therefore be size ~8MB
            var vfs2 = InternalHelpers.CreateJCDFAT(testName, JCDFAT.globalMinFSSize + 2 * blockSize + 1);
            Assert.AreEqual(blockSize + (JCDFAT.globalMinFSSize + blockSize) * 2, vfs2.Size());
            InternalHelpers.CloseJCDFAT(vfs2, testName);

            // This vfs is 8MB + 3 blocks + 1, and should therefore be size ~12MB
            var vfs3 = InternalHelpers.CreateJCDFAT(testName, 2 * JCDFAT.globalMinFSSize + 3 * blockSize + 1);
            Assert.AreEqual(blockSize + (JCDFAT.globalMinFSSize + blockSize) * 3, vfs3.Size());
            InternalHelpers.CloseJCDFAT(vfs3, testName);

            // This vfs is 12MB + 4 blocks + 1, and should therefore be size ~16MB
            var vfs4 = InternalHelpers.CreateJCDFAT(testName, 3 * JCDFAT.globalMinFSSize + 4 * blockSize + 1);
            Assert.AreEqual(blockSize + (JCDFAT.globalMinFSSize + blockSize) * 4, vfs4.Size());
            InternalHelpers.CloseJCDFAT(vfs4, testName);
        }

        #endregion

        #region OccupiedSpace Tests

        [TestMethod()]
        public void OccupiedSpaceNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var sizeBefore = vfs.OccupiedSpace();
            var fileSize = InternalHelpers.MB1;
            vfs.CreateFile("file", fileSize, false);
            var blockSize = 1UL << 12; // 4KB

            // Test
            ulong fileBlocks = Helpers.ruid(fileSize, blockSize);
            ulong fileRealSize = (fileBlocks + 4) * blockSize;
            Assert.AreEqual(sizeBefore + fileRealSize, vfs.OccupiedSpace());
        }

        #endregion

        #region Size, Free and Occupied combined

        [TestMethod()]
        public void SizeFreeOccupiedCombinedTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            Assert.AreEqual(vfs.Size(), vfs.OccupiedSpace() + vfs.FreeSpace());
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        #endregion

        #region CreateDirectory Tests

        [TestMethod()]
        public void CreateDirectoryNormalParentFalseTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirName = "dir";
            var dirPath = Helpers.PathCombine("/", dirName);
            vfs.CreateDirectory(dirPath, false);

            // Test
            vfs.SetCurrentDirectory(dirPath);

            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        public void CreateDirectoryNormalParentTrueTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var parentDir = "/parent";
            var dirName = "dir";
            var dirPath = Helpers.PathCombine(parentDir, dirName);
            vfs.CreateDirectory(dirPath, true);

            // Test
            vfs.SetCurrentDirectory(dirPath);

            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(ParentNotFoundException), "Expected parent dir not to exist")]
        public void CreateDirectoryParentMissingTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var parentDir = "/parent";
            var dirName = "dir";
            var dirPath = Helpers.PathCombine(parentDir, dirName);

            // Test
            vfs.CreateDirectory(dirPath, false);
        }

        #endregion

        #region ImportFile Tests

        [TestMethod()]
        public void ImportFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            // Create file to import.
            var hfsFileName = TestHelpers.GetTestFileName(testName, "stream");
            TestHelpers.DeleteFiles(hfsFileName);
            var fileSize = (int)InternalHelpers.MB1;
            var exportFileData = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = new FileStream(hfsFileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(exportFileData, 0, fileSize);
                stream.Flush();
            }
            var vfsImportName = "vfsTarget";

            // Test
            vfs.ImportFile(hfsFileName, vfsImportName);
            using (var vfsImportStream = vfs.GetFileStream(vfsImportName)) {
                using (var hfsExportStream = new FileStream(hfsFileName, FileMode.Open)) {
                    var streamEquality = TestHelpers.StreamCompare(vfsImportStream, hfsExportStream);
                    Assert.IsTrue(streamEquality);
                }
            }

            // Clean up
            TestHelpers.DeleteFiles(new string[] { hfsFileName });
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(NotEnoughSpaceException),
        "The fact that the file is too big for the VFS was discovered.")]
        public void ImportFileTooBigTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfsSize = InternalHelpers.MB5;
            var vfs = InternalHelpers.CreateJCDFAT(testName, vfsSize);
            // Create file to import.
            var hfsFileName = TestHelpers.GetTestFileName(testName, "stream");
            TestHelpers.DeleteFiles(hfsFileName);
            var fileSize = (int)vfsSize * 2;
            var exportFileData = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = new FileStream(hfsFileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(exportFileData, 0, fileSize);
                stream.Flush();
            }
            var vfsImportName = "vfsTarget";

            // Test
            vfs.ImportFile(hfsFileName, vfsImportName);
        }

        #endregion

        #region ExportFile Tests

        [TestMethod()]
        public void ExportFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            // Create file to import.
            var hfsFileName = TestHelpers.GetTestFileName(testName, "stream");
            TestHelpers.DeleteFiles(hfsFileName);
            var fileSize = (int)InternalHelpers.MB1;
            var exportFileData = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = new FileStream(hfsFileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(exportFileData, 0, fileSize);
                stream.Flush();
            }
            var vfsImportName = "vfsTarget";
            var hfsExportName = "hfsTarget";
            TestHelpers.DeleteFiles(hfsExportName);


            // Test
            vfs.ImportFile(hfsFileName, vfsImportName);
            vfs.ExportFile(vfsImportName, hfsExportName);
            using (var vfsImportStream = vfs.GetFileStream(vfsImportName)) {
                using (var hfsExportStream = new FileStream(hfsExportName, FileMode.Open)) {
                    var streamEquality = TestHelpers.StreamCompare(vfsImportStream, hfsExportStream);
                    Assert.IsTrue(streamEquality);
                }
            }

            // Clean up
            TestHelpers.DeleteFiles(new string[] { hfsFileName, hfsExportName });
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        public void ExportFileRecursive()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirName = "dir";
            var fileName = "file";
            var filePath = Helpers.PathCombine(dirName, fileName);
            var fileSize = (int)InternalHelpers.MB1;
            var data = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = vfs.CreateFile(filePath, (uint)fileSize, true)) {
                stream.Write(data, 0, fileSize);
                stream.Flush();
            }
            var exportDirName = TestHelpers.GetTestFileName(testName, dirName);
            var exportFileName = Helpers.PathCombine(exportDirName, fileName);

            // Test
            vfs.ExportFile(dirName, exportDirName);
            using (var vfsImportStream = vfs.GetFileStream(filePath)) {
                using (var hfsExportStream = new FileStream(exportFileName, FileMode.Open)) {
                    var streamEquality = TestHelpers.StreamCompare(vfsImportStream, hfsExportStream);
                    Assert.IsTrue(streamEquality);
                }
            }

            // Cleanup
            InternalHelpers.CloseJCDFAT(vfs, testName);
            TestHelpers.DeleteFolders(exportDirName, true);
        }


        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to export does not exist was discovered.")]
        public void ExportFileNotExistingTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);

            vfs.ExportFile("/non_existent_file", "output_file");
        }

        [TestMethod()]
        public void ExportFileEmptyTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            // Create empty file to import.
            var hfsFileName = TestHelpers.GetTestFileName(testName, "stream");
            TestHelpers.DeleteFiles(hfsFileName);
            using (var stream = new FileStream(hfsFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.SetLength(0);
                stream.Flush();
            }
            var vfsImportName = "vfsTarget";
            var hfsExportName = "hfsTarget";
            
            // Test
            vfs.ImportFile(hfsFileName, vfsImportName);
            vfs.ExportFile(vfsImportName, hfsExportName);
            using (var vfsImportStream = vfs.GetFileStream(vfsImportName)) {
                using (var hfsExportStream = new FileStream(hfsExportName, FileMode.Open)) {
                    var streamEquality = TestHelpers.StreamCompare(vfsImportStream, hfsExportStream);
                    Assert.IsTrue(streamEquality);
                }
            }

            // Clean up
            TestHelpers.DeleteFiles(new string[] { hfsFileName, hfsExportName });
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        #endregion

        #region DeleteFile Tests

        [TestMethod()]
        public void DeleteFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var filePath = "/vfsSrc.txt";
            var fileSize = InternalHelpers.MB1;
            vfs.CreateFile(filePath, fileSize, false);
            
            // Test
            vfs.DeleteFile(filePath, false);
            var dirList = vfs.ListDirectory("/");
            Assert.AreEqual(0, dirList.Length);

            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        public void DeleteFileRecursiveTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirPath = "/dir";
            vfs.CreateDirectory(dirPath, false);
            // Insert files in to dir.
            var fileSize = InternalHelpers.MB1;
            var numFiles = 10;
            for (int i = 0; i < numFiles; i += 1) {
                var filePath = Helpers.PathCombine(dirPath, i.ToString());
                vfs.CreateFile(filePath, fileSize, false);
            }

            // Test
            vfs.DeleteFile(dirPath, true);
            var dirList = vfs.ListDirectory("/");
            Assert.AreEqual(0, dirList.Length);

            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to delete does not exist was discovered.")]
        public void DeleteFileNotExistingTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);

            // Test
            vfs.DeleteFile("non_existent_file", false);
        }

        #endregion

        #region RenameFile Tests

        [TestMethod()]
        public void RenameFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var fileSize = (int)InternalHelpers.MB1;
            var fileName = "file";
            var fileData = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = vfs.CreateFile(fileName, (uint)fileSize, false)) {
                stream.Write(fileData, 0, fileSize);
                stream.Flush();
            }
            var newFile = "newFile";

            // Test
            vfs.RenameFile(fileName, newFile);
            var files = vfs.ListDirectory("/").Select(file => file.Name);
            // Old file no longer shows
            Assert.IsFalse(files.Contains(fileName));
            // New file shows
            Assert.IsTrue(files.Contains(newFile));

            // Make sure file contents are equal
            var newFileData = new byte[fileSize];
            using (var stream = vfs.GetFileStream(newFile)) {
                stream.Read(newFileData, 0, fileSize);
            }
            TestHelpers.AreEqual(fileData, newFileData);

            // Clean up
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to rename does not exist was discovered.")]
        public void RenameFileNotExistingTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            vfs.RenameFile("non_existent_file", "dir");
        }

        [TestMethod()]
        [ExpectedException(typeof(FileExistsException),
        "The fact that the file name to change to does already exist was discovered.")]
        public void RenameFileToExistingNameTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var fileSize = InternalHelpers.MB1;
            var fileName = "file";
            vfs.CreateFile(fileName, fileSize, false);

            // Test
            vfs.RenameFile(fileName, fileName);
        }

        #endregion

        #region MoveFile Tests

        [TestMethod()]
        public void MoveFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var fileSize = (int)InternalHelpers.MB1;
            var fileName = "file";
            var fileData = TestHelpers.GenerateRandomData(fileSize);
            using (var stream = vfs.CreateFile(fileName, (uint)fileSize, false)) {
                stream.Write(fileData, 0, fileSize);
                stream.Flush();
            }
            var newFileDir = "dir";
            vfs.CreateDirectory(newFileDir, false);
            var newFileName = "newFile";
            var newFilePath = Helpers.PathCombine(newFileDir, newFileName);

            // Test
            vfs.MoveFile(fileName, newFilePath);
            
            // fileName no longer shows in root (only newFileDir does).
            Assert.AreEqual(1, vfs.ListDirectory("/").Length);
            Assert.AreEqual(newFileDir, vfs.ListDirectory("/")[0].Name);
            
            // New file shows when listing newFileDir.
            Assert.AreEqual(newFileName, vfs.ListDirectory(newFileDir)[0].Name);

            // Make sure file contents are equal
            var newFileData = new byte[fileSize];
            using (var stream = vfs.GetFileStream(newFilePath)) {
                stream.Read(newFileData, 0, fileSize);
            }
            TestHelpers.AreEqual(fileData, newFileData);

            // Clean up
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to move does not exist was discovered.")]
        public void MoveFileNotExistingTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            vfs.MoveFile("non_existent_filefile", "dir");
        }

        #endregion

        #region CopyFile Tests

        [TestMethod()]
        public void CopyFileNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);

            string srcFile = "source.txt";
            int srcFileSize = (int)InternalHelpers.MB1;
            vfs.CreateFile(srcFile, (uint)srcFileSize, false);
            var srcFileData = TestHelpers.GenerateRandomData(srcFileSize);
            using (var stream = vfs.GetFileStream(srcFile)) {
                stream.Write(srcFileData, 0, srcFileSize);
            }

            // Test
            string targetFile = "target.txt";
            vfs.CopyFile(srcFile, targetFile);
            var targetDetails = vfs.GetFileDetails(targetFile);
            // Ensure that sizes are equal.
            Assert.AreEqual((ulong)srcFileSize, targetDetails.Size);

            // Ensure that contents are the same.
            using (var stream = vfs.GetFileStream(targetFile)) {
                var targetFileData = new byte[srcFileSize];
                stream.Read(targetFileData, 0, srcFileSize);
                TestHelpers.AreEqual(srcFileData, targetFileData);
            }
            InternalHelpers.CloseJCDFAT(vfs, testName);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to copy does not exist was discovered.")]
        public void CopyFileNotExistingTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirName = "dir";
            vfs.CreateDirectory(dirName, false);

            // Test
            vfs.CopyFile("non_existing_file", dirName);
        }

        #endregion

        #region ListDirectory Tests

        [TestMethod()]
        public void ListDirectoryNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var files = new string[] {"1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                                      "11", "12", "13", "14", "15", "16", "17", "18"};
            var dirPath = "/dir/";
            foreach (var fileName in files) {
                var filePath = Helpers.PathCombine(dirPath, fileName);
                vfs.CreateFile(filePath, 1, true);
            }

            // Test
            var fileList = vfs.ListDirectory(dirPath);
            var fileNameList = fileList.Select(file => file.Name);
            foreach (var fileName in files) {
                Assert.IsTrue(fileNameList.Contains(fileName));
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the directory to list does not exist was discovered.")]
        public void ListDirectoryNotExistingTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);

            vfs.ListDirectory("non_existent_directory");
        }

        #endregion

        #region SetCurrentDirectory Tests

        [TestMethod()]
        public void SetCurrentDirectoryNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirPath = "/dir/";
            vfs.CreateDirectory(dirPath, false);

            // Test
            vfs.SetCurrentDirectory(dirPath);

            Assert.AreEqual(dirPath, vfs.GetCurrentDirectory());
        }

        [TestMethod()]
        public void SetCurrentDirectoryUpwardsTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            var dirPath = "/dir/";
            vfs.CreateDirectory(dirPath, false);
            vfs.SetCurrentDirectory(dirPath);
            Assert.AreEqual(dirPath, vfs.GetCurrentDirectory());

            // Test
            vfs.SetCurrentDirectory("..");
            Assert.AreEqual("/", vfs.GetCurrentDirectory());
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the directory to set cannot exist was discovered.")]
        public void SetCurrentDirectoryUpwardsAtRootTest()
        {

            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);

            // Test
            Assert.AreEqual("/", vfs.GetCurrentDirectory());
            vfs.SetCurrentDirectory("..");
        }

        #endregion

        #region GetCurrentDirectory Tests

        [TestMethod()]
        public void GetCurrentDirectoryNormalTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            
            // Test
            Assert.AreEqual("/", vfs.GetCurrentDirectory());

            var dirName = "/dir/";
            vfs.CreateDirectory(dirName, false);
            vfs.SetCurrentDirectory(dirName);

            Assert.AreEqual(dirName, vfs.GetCurrentDirectory());
        }

        #endregion

    }

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class MountedTestsWithoutInitializer {
        #region Create Parents Tests
        [TestMethod]
        public void TestCreateParents() {
            // Set up
            var vfs = InternalHelpers.CreateJCDFAT("create_parents");
            var dirName = "/1/2/3/4/5/";
            vfs.CreateDirectory(dirName, true);

            // Test
            vfs.SetCurrentDirectory(dirName);
            Assert.AreEqual(dirName, vfs.GetCurrentDirectory());
        }

        [TestMethod]
        public void TestCreateParentsAlreadyExist() {
            // Set up
            var vfs = InternalHelpers.CreateJCDFAT("create_parents_already_exist");
            var dirName1 = "/1/2/3/";
            var dirName2 = "/1/2/3/4/5/";
            vfs.CreateDirectory(dirName1, true);
            vfs.CreateDirectory(dirName2, true);

            // Test
            vfs.SetCurrentDirectory(dirName2);
            Assert.AreEqual(dirName2, vfs.GetCurrentDirectory());
        }
        #endregion

        #region VFS id Tests
        [TestMethod]
        public void TestVFSInitialId() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            
            // Test
            Assert.AreEqual(-1, vfs.GetId());
        }

        [TestMethod]
        public void TestVFSSetId() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            long id = 1337;

            // Test
            vfs.SetId(id);
            Assert.AreEqual(id, vfs.GetId());
        }

        [TestMethod]
        public void TestVFSSetIdCloseGetId() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            long id = 1337;
            vfs.SetId(id);
            vfs.Close();
            vfs = InternalHelpers.OpenJCDFAT(testName);
            
            // Test
            Assert.AreEqual(id, vfs.GetId());
        }

        [TestMethod]
        public void TestVFSSetIdCloseGetIdNegativeNumber() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            long id = -1337;
            vfs.SetId(id);
            vfs.Close();
            vfs = InternalHelpers.OpenJCDFAT(testName);

            // Test
            Assert.AreEqual(id, vfs.GetId());
        }
        #endregion
    }

    [ExcludeFromCodeCoverage]
    internal class InternalHelpers {
        public const uint MB1 = 1000000;
        public const uint MB5 = 5 * MB1;
        public const uint MB50 = 10 * MB5;

        public static JCDFAT CreateJCDFAT(string testName, ulong size) {
            testName = TestHelpers.GetTestFileName(testName);
            TestHelpers.DeleteFiles(testName);
            return JCDFAT.Create(testName, size);
        }

        public static JCDFAT CreateJCDFAT(string testName) {
            return CreateJCDFAT(testName, MB50);
        }

        public static JCDFAT OpenJCDFAT(string testName) {
            return JCDFAT.Open(TestHelpers.GetTestFileName(testName));
        }

        public static void CloseJCDFAT(JCDFAT vfs, string testName) {
            vfs.Close();
            TestHelpers.DeleteFiles(TestHelpers.GetTestFileName(testName));
        }
    }
}
