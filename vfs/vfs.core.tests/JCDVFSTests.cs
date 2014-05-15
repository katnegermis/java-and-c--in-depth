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
            var testFileName = InternalHelpers.GetTestFileName(testName);

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
            var testFile = InternalHelpers.GetTestFileName(testName);
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
            var testFile = InternalHelpers.GetTestFileName(testName);

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
            var testFile = InternalHelpers.GetTestFileName(testName);
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
            var testFile = InternalHelpers.GetTestFileName(testName);
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
            var testFile = InternalHelpers.GetTestFileName(testName);
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
            var testFile = InternalHelpers.GetTestFileName(testName);

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
            InternalHelpers.CreateJCDFAT(testName).Close();

            // Test
            JCDFAT.Delete(testName);
            Assert.IsFalse(File.Exists(testName));
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "The fact that the file is no VFS was discovered.")]
        public void DeleteNoVFSFileTest()
        {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFile = InternalHelpers.GetTestFileName(testName);
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
            var testFile = InternalHelpers.GetTestFileName(testName);
            
            // Test
            JCDFAT.Delete(testFile);
        }


        #endregion

    }


    [TestClass()]
    public class MountedJCFVFSTests
    {
        JCDFAT testVFS = null;

        [TestInitialize()]
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

            testVFS = JCDFAT.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            testVFS.Close();
            testVFS = JCDFAT.Open(TestVariables.FilePath());
        }

        [TestCleanup()]
        public void MountedCleanup()
        {
            if (testVFS != null)
            {
                try
                {
                    // testVFS.Close();
                }
                finally
                {
                    testVFS = null;
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

        /// <summary>
        /// Helper method to compare 2 files (byte by byte)
        /// </summary>
        /// <param name="file1">First file path</param>
        /// <param name="file2">Second file path</param>
        /// <returns>true if equal, false otherwise</returns>
        private bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        #region Close Tests

        [TestMethod()]
        public void CloseNormalTest()
        {
            testVFS.Close();
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        #endregion

        #region Size Tests

        [TestMethod()]
        public void SizeTest()
        {
            Assert.AreEqual(TestVariables.SIZE_STANDARD, testVFS.Size());
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
            var before = testVFS.OccupiedSpace();
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
            Assert.AreEqual(testVFS.Size(), testVFS.OccupiedSpace() + testVFS.FreeSpace());
        }

        #endregion

        #region CreateDirectory Tests

        [TestMethod()]
        public void CreateDirectoryNormalParentFalseTest()
        {
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"dir", false);
            var list = testVFS.ListDirectory(testVFS.GetCurrentDirectory());
            Assert.AreEqual(1, list.Length);
        }

        [TestMethod()]
        public void CreateDirectoryNormalParentTrueTest()
        {
            string dir = testVFS.GetCurrentDirectory();
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"dir\another", true);
            var list = testVFS.ListDirectory(testVFS.GetCurrentDirectory() + @"dir");
            Assert.AreEqual(1, list.Length);
        }

        [TestMethod()]
        public void CreateDirectoryParentMissingTest()
        {
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"\dir\another", false);
            Assert.Inconclusive("No way to verify the result. Some exception might have to be thrown.");
        }

        #endregion

        #region ImportFile Tests

        [TestMethod()]
        public void ImportFileNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            createFile(TestVariables.TEST_DIRECTORY + @"source.txt", TestVariables.SIZE_SMALL);

            testVFS.ImportFile(TestVariables.TEST_DIRECTORY + @"source.txt", currentDir + name);
            var list = testVFS.ListDirectory(currentDir);
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
            testVFS.ImportFile(sourceFile, @"vfsSrc.txt");
        }

        #endregion

        #region ExportFile Tests

        [TestMethod()]
        public void ExportFileNormalTest()
        {
            createFile(TestVariables.SourcePath(), TestVariables.SIZE_SMALL);
            testVFS.ImportFile(TestVariables.SourcePath(), @"vfsSrc.txt");
            testVFS.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.IsTrue(FileCompare(TestVariables.SourcePath(), TestVariables.TargetPath()));
        }

        [TestMethod()]
        public void ExportFileRecursive()
        {
            string source = Path.Combine(TestVariables.TEST_DIRECTORY, "source");
            string target = Path.Combine(TestVariables.TEST_DIRECTORY, "target");

            Directory.CreateDirectory(source);
            createFile(Path.Combine(source, "file.txt"), TestVariables.SIZE_SMALL);
            testVFS.ImportFile(source, @"/dir");

            testVFS.ExportFile(@"/dir", target);

            Assert.IsTrue(FileCompare(Path.Combine(source, "file.txt"), Path.Combine(target, "file.txt")));
        }


        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to export does not exist was discovered.")]
        public void ExportFileNotExistingTest()
        {
            testVFS.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.Inconclusive("No way to verify the result.");
        }

        [TestMethod()]
        public void ExportFileEmptyTest()
        {
            createFile(TestVariables.SourcePath(), 0);
            testVFS.ImportFile(TestVariables.SourcePath(), @"vfsSrc.txt");
            testVFS.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.IsTrue(FileCompare(TestVariables.SourcePath(), TestVariables.TargetPath()));
        }

        #endregion

        #region DeleteFile Tests

        [TestMethod()]
        public void DeleteFileNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            createFile(TestVariables.SourcePath(), TestVariables.SIZE_SMALL);

            testVFS.ImportFile(TestVariables.SourcePath(), Path.Combine(currentDir, name));
            testVFS.DeleteFile(Path.Combine(currentDir, name), false);
            var list = testVFS.ListDirectory(currentDir);
            bool notDeleted = false;
            foreach (var entry in list)
                if (entry.Name == name)
                {
                    notDeleted = true;
                    break;
                }
            Assert.IsFalse(notDeleted);
        }

        [TestMethod()]
        public void DeleteFileRecursiveTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            createFile(TestVariables.SourcePath(), TestVariables.SIZE_SMALL);
            testVFS.CreateDirectory(Path.Combine(currentDir, @"dir"), false);

            testVFS.ImportFile(TestVariables.SourcePath(), Path.Combine(currentDir, @"dir\vfsSrc.txt"));
            testVFS.DeleteFile(Path.Combine(currentDir, @"dir\vfsSrc.txt"), true);
            var list = testVFS.ListDirectory(currentDir);

            Assert.AreEqual(0, list.Length);
            // Assert.Inconclusive("No way to verify the result");
        }

        [TestMethod()]
        [ExpectedException(typeof(vfs.exceptions.FileNotFoundException),
        "The fact that the file to delete does not exist was discovered.")]
        public void DeleteFileNotExistingTest()
        {
            testVFS.DeleteFile("file", true);
            Assert.Inconclusive("No way to verify the result");
        }

        #endregion

        #region RenameFile Tests

        [TestMethod()]
        public void RenameFileNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            testVFS.CreateDirectory(Path.Combine(currentDir, @"dir"), false);

            testVFS.RenameFile(Path.Combine(currentDir, @"dir"), @"new");
            var list = testVFS.ListDirectory(currentDir);

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
            testVFS.RenameFile("file", "newName");
            Assert.Inconclusive("No way to verify the result");
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the file name to change to does already exist was discovered.")]
        public void RenameFileToExistingNameTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            testVFS.CreateDirectory(Path.Combine(currentDir, @"old"), false);
            testVFS.CreateDirectory(Path.Combine(currentDir, @"new"), false);

            testVFS.RenameFile(Path.Combine(currentDir, @"old"), @"new");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region MoveFile Tests

        [TestMethod()]
        public void MoveFileNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            string targetDir = "target";
            createFile(TestVariables.TEST_DIRECTORY + @"file.txt", TestVariables.SIZE_SMALL);

            testVFS.CreateDirectory(Path.Combine(currentDir, targetDir), false);
            testVFS.ImportFile(TestVariables.TEST_DIRECTORY + @"file.txt", Path.Combine(currentDir, name));
            testVFS.MoveFile(Path.Combine(currentDir, name), Path.Combine(currentDir, targetDir, name));
            var targetList = testVFS.ListDirectory(Path.Combine(currentDir, targetDir));
            bool found = false;
            foreach (var entry in targetList)
                if (entry.Name == name)
                {
                    found = true;
                    break;
                }
            var sourceList = testVFS.ListDirectory(Path.Combine(currentDir, name));
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
        [ExpectedException(typeof(Exception),
        "The fact that the file to move does not exist was discovered.")]
        public void MoveFileNotExistingTest()
        {
            testVFS.CreateDirectory("dir", false);
            testVFS.MoveFile("file", "dir");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region CopyFile Tests

        [TestMethod()]
        public void CopyFileNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = @"vfsSrc.txt";
            string targetDir = "target";
            createFile(TestVariables.TEST_DIRECTORY + @"file.txt", TestVariables.SIZE_SMALL);

            testVFS.CreateDirectory(Path.Combine(currentDir, targetDir), false);
            testVFS.ImportFile(TestVariables.TEST_DIRECTORY + @"file.txt", Path.Combine(currentDir, name));
            testVFS.CopyFile(Path.Combine(currentDir, name), Path.Combine(currentDir, targetDir, name));
            var targetList = testVFS.ListDirectory(Path.Combine(currentDir, targetDir));
            bool found = false;
            foreach (var entry in targetList)
                if (entry.Name == name)
                {
                    found = true;
                    break;
                }
            var sourceList = testVFS.ListDirectory(Path.Combine(currentDir, name));
            bool stillThere = false;
            foreach (var entry in sourceList)
                if (entry.Name == name)
                {
                    stillThere = true;
                    break;
                }
            Assert.IsTrue(found && stillThere);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the file to copy does not exist was discovered.")]
        public void CopyFileNotExistingTest()
        {
            testVFS.CreateDirectory("dir", false);
            testVFS.CopyFile("file", "dir");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region ListDirectory Tests

        [TestMethod()]
        public void ListDirectoryNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = "dir";

            testVFS.CreateDirectory(Path.Combine(currentDir, name), false);
            var list = testVFS.ListDirectory(currentDir);
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
            string currentDir = testVFS.GetCurrentDirectory();
            string name = "dir";

            var list = testVFS.ListDirectory(Path.Combine(currentDir, name));
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region SetCurrentDirectory Tests

        [TestMethod()]
        public void SetCurrentDirectoryNormalTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = "dir";

            testVFS.CreateDirectory(Path.Combine(currentDir, name), false);
            testVFS.SetCurrentDirectory(Path.Combine(currentDir, name));

            Assert.AreEqual(Path.Combine(currentDir, name), testVFS.GetCurrentDirectory());
        }

        [TestMethod()]
        public void SetCurrentDirectoryUpwardsTest()
        {
            string currentDir = testVFS.GetCurrentDirectory();
            string name = @"dir\inner";

            testVFS.CreateDirectory(Path.Combine(currentDir, name), true);
            testVFS.SetCurrentDirectory(Path.Combine(currentDir, name));
            testVFS.SetCurrentDirectory("..");

            Assert.AreEqual(Path.Combine(currentDir, "dir"), testVFS.GetCurrentDirectory());
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
        "The fact that the directory to set cannot exist was discovered.")]
        public void SetCurrentDirectoryUpwardsAtRootTest()
        {
            testVFS.SetCurrentDirectory(@"\");
            testVFS.SetCurrentDirectory("..");
            Assert.Inconclusive("Should throw some exception");
        }

        #endregion

        #region GetCurrentDirectory Tests

        [TestMethod()]
        public void GetCurrentDirectoryNormalTest()
        {
            string name = @"\dir";

            testVFS.CreateDirectory(name, false);
            testVFS.SetCurrentDirectory(name);

            Assert.AreEqual(name, testVFS.GetCurrentDirectory());
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
            testName = GetTestFileName(testName);
            TestHelpers.DeleteFiles(testName);
            return JCDFAT.Create(testName, size);
        }

        public static JCDFAT CreateJCDFAT(string testName) {
            return CreateJCDFAT(testName, MB50);
        }

        public static JCDFAT OpenJCDFAT(string testName) {
            return JCDFAT.Open(GetTestFileName(testName));
        }

        public static void CloseJCDFAT(JCDFAT vfs, string testName) {
            vfs.Close();
            TestHelpers.DeleteFiles(GetTestFileName(testName));
        }

        public static string GetTestFileName(string testName) {
            return testName;
        }
    }
}
