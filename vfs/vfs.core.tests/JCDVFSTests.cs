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
namespace vfs.core.tests
{
    public class TestVariables {
        public const string TEST_DIRECTORY = @"testDirectory\";
        public const string TEST_FILE = @"test.vfs";
        public const ulong SIZE_MAX = UInt64.MaxValue;
        public const ulong SIZE_STANDARD = 50 * 1024 * 1024;
        public const long SIZE_SMALL = 5 * 1024 * 1024;

        public static string FilePath() {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + TestVariables.TEST_FILE);
        }

        public static string SourcePath() {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + @"source.txt");
        }

        public static string TargetPath() {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + @"target.txt");
        }
    }

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
            var testVFS = JCDFAT.Create(testFile, TestVariables.SIZE_STANDARD);
        }

        #endregion

        #region Open Tests

        [TestMethod()]
        public void OpenNormalTest()
        {
            var testVFS = JCDFAT.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            testVFS.Close();
            testVFS = JCDFAT.Open(TestVariables.FilePath());
            Assert.Inconclusive("No real direct way to verify the result.");
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


    [TestClass()]
    public class MountedJCFVFSTests
    {
        JCDFAT vfs = null;

        public void MountedInitializer()
        {
          /*  if (File.Exists(TestVariables.FilePath()))
                File.Delete(TestVariables.FilePath());*/

            if (Directory.Exists(TestVariables.TEST_DIRECTORY))
                Directory.Delete(TestVariables.TEST_DIRECTORY, true);

            Directory.CreateDirectory(TestVariables.TEST_DIRECTORY);


            /*if (File.Exists(TestVariables.SourcePath()))
                File.Delete(TestVariables.SourcePath());

            if (File.Exists(TestVariables.TargetPath()))
                File.Delete(TestVariables.TargetPath());*/

            vfs = JCDFAT.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            vfs.Close();
            vfs = JCDFAT.Open(TestVariables.FilePath());
        }

        [TestCleanup()]
        public void MountedCleanup()
        {
            if (vfs != null)
            {
                try
                {
                    // testVFS.Close();
                }
                finally
                {
                    vfs = null;
                }
            }
        }

        /// <summary>
        /// Helper method that creates a file with the given size at the given location
        /// </summary>
        /// <param name="path">To create the file at</param>
        /// <param name="size">Size of the file to create</param>
        public void createFile(string path, long size)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(size);
            }
        }

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
            Assert.AreEqual(TestVariables.SIZE_STANDARD, vfs.Size());
        }

        #endregion

        #region OccupiedSpace Tests

        [TestMethod()]
        public void OccupiedSpaceEmptyTest()
        {
            Assert.Inconclusive("No way to verify the result.");
            //Assert.AreEqual(0, testVFS.OccupiedSpace());
        }

        [TestMethod()]
        public void OccupiedSpaceNormalTest()
        {
            var before = vfs.OccupiedSpace();
            //testVFS.
            Assert.Inconclusive("No way to verify the result.");
        }

        [TestMethod()]
        public void OccupiedSpaceFullTest()
        {
            Assert.Inconclusive("No way to verify the result.");
        }

        #endregion

        #region FreeSpace Tests

        [TestMethod()]
        public void FreeSpaceAllTest()
        {
            Assert.Inconclusive("No way to verify the result.");
        }

        [TestMethod()]
        public void FreeSpaceNoneTest()
        {
            Assert.Inconclusive("No way to verify the result.");
        }

        #endregion

        #region Size, Free and Occupied combined

        [TestMethod()]
        public void SizeFreeOccupiedCombinedTest()
        {
            Assert.AreEqual(vfs.Size(), vfs.OccupiedSpace() + vfs.FreeSpace());
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
            string currentDir = vfs.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            createFile(TestVariables.TEST_DIRECTORY + @"source.txt", TestVariables.SIZE_SMALL);

            vfs.ImportFile(TestVariables.TEST_DIRECTORY + @"source.txt", currentDir + name);
            var list = vfs.ListDirectory(currentDir);
            bool found = false;
            foreach (var entry in list)
                if (entry.Name == name)
                {
                    found = true;
                    break;
                }
            Assert.IsTrue(found);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "The fact that the file is too big for the VFS was discovered.")]
        public void ImportFileTooBigTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            InternalHelpers.CreateJCDFAT(testName);
            string sourceFile = TestVariables.TEST_DIRECTORY + @"source.txt";
            createFile(sourceFile, (long)TestVariables.SIZE_STANDARD + 8);
            vfs.ImportFile(sourceFile, @"vfsSrc.txt");
        }

        #endregion

        #region ExportFile Tests

        [TestMethod()]
        public void ExportFileNormalTest()
        {
            createFile(TestVariables.SourcePath(), TestVariables.SIZE_SMALL);
            vfs.ImportFile(TestVariables.SourcePath(), @"vfsSrc.txt");
            vfs.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.IsTrue(TestHelpers.HostFileCompare(TestVariables.SourcePath(), TestVariables.TargetPath()));
        }

        [TestMethod()]
        public void ExportFileRecursive()
        {
            string source = Path.Combine(TestVariables.TEST_DIRECTORY, "source");
            string target = Path.Combine(TestVariables.TEST_DIRECTORY, "target");

            Directory.CreateDirectory(source);
            createFile(Path.Combine(source, "file.txt"), TestVariables.SIZE_SMALL);
            vfs.ImportFile(source, @"/dir");

            vfs.ExportFile(@"/dir", target);

            Assert.IsTrue(TestHelpers.HostFileCompare(Path.Combine(source, "file.txt"), Path.Combine(target, "file.txt")));
        }


        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to export does not exist was discovered.")]
        public void ExportFileNotExistingTest()
        {
            vfs.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.Inconclusive("No way to verify the result.");
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
            string currentDir = vfs.GetCurrentDirectory();
            vfs.CreateDirectory(Path.Combine(currentDir, @"dir"), false);

            vfs.RenameFile(Path.Combine(currentDir, @"dir"), @"new");
            var list = vfs.ListDirectory(currentDir);

            bool foundNew = false;
            bool foundOld = false;
            foreach (var entry in list)
            {
                if (entry.Name == @"new")
                    foundNew = true;
                if (entry.Name == @"dir")
                    foundOld = false;
            }
            Assert.IsTrue(foundNew);
            Assert.IsFalse(foundOld);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to rename does not exist was discovered.")]
        public void RenameFileNotExistingTest()
        {
            vfs.RenameFile("file", "newName");
            Assert.Inconclusive("No way to verify the result");
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the file name to change to does already exist was discovered.")]
        public void RenameFileToExistingNameTest()
        {
            string currentDir = vfs.GetCurrentDirectory();
            vfs.CreateDirectory(Path.Combine(currentDir, @"old"), false);
            vfs.CreateDirectory(Path.Combine(currentDir, @"new"), false);

            vfs.RenameFile(Path.Combine(currentDir, @"old"), @"new");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region MoveFile Tests

        [TestMethod()]
        public void MoveFileNormalTest()
        {
            string currentDir = vfs.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            string targetDir = "target";
            createFile(TestVariables.TEST_DIRECTORY + @"file.txt", TestVariables.SIZE_SMALL);

            vfs.CreateDirectory(Path.Combine(currentDir, targetDir), false);
            vfs.ImportFile(TestVariables.TEST_DIRECTORY + @"file.txt", Path.Combine(currentDir, name));
            vfs.MoveFile(Path.Combine(currentDir, name), Path.Combine(currentDir, targetDir, name));
            var targetList = vfs.ListDirectory(Path.Combine(currentDir, targetDir));
            bool found = false;
            foreach (var entry in targetList)
                if (entry.Name == name)
                {
                    found = true;
                    break;
                }
            var sourceList = vfs.ListDirectory(Path.Combine(currentDir, name));
            bool stillThere = false;
            foreach (var entry in sourceList)
                if (entry.Name == name)
                {
                    stillThere = true;
                    break;
                }
            Assert.IsTrue(found && !stillThere);
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to move does not exist was discovered.")]
        public void MoveFileNotExistingTest()
        {
            var testName = MethodBase.GetCurrentMethod().Name;
            var vfs = InternalHelpers.CreateJCDFAT(testName);
            vfs.CreateDirectory("dir", false);
            vfs.MoveFile("file", "dir");
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
            string currentDir = vfs.GetCurrentDirectory();
            string name = "dir";

            vfs.CreateDirectory(Path.Combine(currentDir, name), false);
            var list = vfs.ListDirectory(currentDir);
            bool found = false;
            foreach (var entry in list)
                if (entry.Name == name)
                {
                    found = true;
                    break;
                }
            Assert.IsTrue(found);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the directory to list does not exist was discovered.")]
        public void ListDirectoryNotExistingTest()
        {
            string currentDir = vfs.GetCurrentDirectory();
            string name = "dir";

            var list = vfs.ListDirectory(Path.Combine(currentDir, name));
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region SetCurrentDirectory Tests

        [TestMethod()]
        public void SetCurrentDirectoryNormalTest()
        {
            string currentDir = vfs.GetCurrentDirectory();
            string name = "dir";

            vfs.CreateDirectory(Path.Combine(currentDir, name), false);
            vfs.SetCurrentDirectory(Path.Combine(currentDir, name));

            Assert.AreEqual(Path.Combine(currentDir, name), vfs.GetCurrentDirectory());
        }

        [TestMethod()]
        public void SetCurrentDirectoryUpwardsTest()
        {
            string currentDir = vfs.GetCurrentDirectory();
            string name = @"dir\inner";

            vfs.CreateDirectory(Path.Combine(currentDir, name), true);
            vfs.SetCurrentDirectory(Path.Combine(currentDir, name));
            vfs.SetCurrentDirectory("..");

            Assert.AreEqual(Path.Combine(currentDir, "dir"), vfs.GetCurrentDirectory());
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the directory to set cannot exist was discovered.")]
        public void SetCurrentDirectoryUpwardsAtRootTest()
        {
            vfs.SetCurrentDirectory(@"\");
            vfs.SetCurrentDirectory("..");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region GetCurrentDirectory Tests

        [TestMethod()]
        public void GetCurrentDirectoryNormalTest()
        {
            string name = @"\dir";

            vfs.CreateDirectory(name, false);
            vfs.SetCurrentDirectory(name);

            Assert.AreEqual(name, vfs.GetCurrentDirectory());
        }

        #endregion

    }

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
            Assert.AreEqual(0, vfs.GetId());
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
