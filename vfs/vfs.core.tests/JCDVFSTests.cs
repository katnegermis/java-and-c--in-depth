using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using vfs.exceptions;
namespace vfs.core.tests
{

    public class TestVariables
    {
        public const string TEST_DIRECTORY = @"testDirectory\";
        public const string TEST_FILE = @"test.vfs";
        public const ulong SIZE_MAX = UInt64.MaxValue;
        public const ulong SIZE_STANDARD = 50 * 1024 * 1024;
        public const long SIZE_SMALL = 5 * 1024 * 1024;

        public static string FilePath()
        {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + TestVariables.TEST_FILE);
        }

        public static string SourcePath()
        {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + @"source.txt");
        }

        public static string TargetPath()
        {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + @"target.txt");
        }
    }

    [TestClass()]
    public class UnmountedJCDVFSTests
    {

        JCDVFS testVFS = null;

        [TestInitialize()]
        public void UnmountedInitializer()
        {
            if (File.Exists(TestVariables.FilePath()))
                File.Delete(TestVariables.FilePath());
            else if (!Directory.Exists(TestVariables.TEST_DIRECTORY))
                Directory.CreateDirectory(TestVariables.TEST_DIRECTORY);
        }

        [TestCleanup()]
        public void UnmountedCleanup()
        {
            if (testVFS != null)
            {
                testVFS.Close();
                testVFS = null;
            }

            /*try
            {
                //JCDVFS.Delete(TestVariables.FilePath());
              //  if (File.Exists(TestVariables.FilePath()))
                //    File.Delete(TestVariables.FilePath());
            }
            finally
            {
                testVFS = null;
            }*/
        }

        #region Create Tests

        [TestMethod()]
        public void CreateNormalTest()
        {
            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            Assert.IsNotNull(testVFS);
            Assert.IsTrue(File.Exists(TestVariables.FilePath()));
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
         "A path to an invalid file(in this case a directory) was discovered.")]
        public void CreateWithInvalidPathTest()
        {
            testVFS = JCDVFS.Create(TestVariables.TEST_DIRECTORY + @"dir\", TestVariables.SIZE_STANDARD);
            Assert.IsNull(testVFS);
            Assert.IsFalse(File.Exists(TestVariables.TEST_DIRECTORY + @"dir\"));
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidSizeException),
         "A size of UInt64.MaxValue was rejected.")]
        public void CreateWithSizeTooBigTest()
        {
            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_MAX);
            Assert.IsNull(testVFS);
            Assert.IsFalse(File.Exists(TestVariables.FilePath()));
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception),
         "A size of 0 was rejected with a unspecified exception.")]
        public void CreateWithSizeTooSmallTest()
        {
            testVFS = JCDVFS.Create(TestVariables.FilePath(), 0);
            Assert.IsNotNull(testVFS);
            Assert.IsFalse(File.Exists(TestVariables.FilePath()));
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [ExpectedException(typeof(FileAlreadyExistsException),
         "An existing file was discovered.")]
        public void CreateWithFileExistingTest()
        {
            File.Create(TestVariables.FilePath()).Close();
            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            Assert.IsNull(testVFS);
            Assert.IsFalse(File.Exists(TestVariables.FilePath()));
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        #endregion

        #region Open Tests

        [TestMethod()]
        public void OpenNormalTest()
        {
            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            testVFS.Close();
            testVFS = JCDVFS.Open(TestVariables.FilePath());
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
       "The invalid (empty) file was discovered.")]
        public void OpenInvalidFileTest()
        {
            File.Create(TestVariables.FilePath()).Close();
            testVFS = JCDVFS.Open(TestVariables.FilePath());
            Assert.IsNull(testVFS);
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "A path to an invalid file(in this case a directory) was discovered.")]
        public void OpenWithInvalidPathTest()
        {
            testVFS = JCDVFS.Open(TestVariables.TEST_DIRECTORY + @"dir\");
            Assert.IsNull(testVFS);
            Assert.Inconclusive("No real direct way to verify the result.");
        }

        #endregion

        #region Delete Tests

        [TestMethod()]
        public void DeleteNormalTest()
        {
            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            testVFS.Close();
            JCDVFS.Delete(TestVariables.FilePath());
            testVFS = null;
            Assert.IsFalse(File.Exists(TestVariables.FilePath()));
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "The fact that the file is no VFS was discovered.")]
        public void DeleteNoVFSFileTest()
        {
            File.Create(TestVariables.FilePath()).Close();
            JCDVFS.Delete(TestVariables.FilePath());
            Assert.IsTrue(File.Exists(TestVariables.FilePath()));
        }

        [TestMethod()]
        [ExpectedException(typeof(FileNotFoundException),
        "The fact that the file is not existing was discovered.")]
        public void DeleteNotExistingFileTest()
        {
            JCDVFS.Delete(TestVariables.FilePath());
            Assert.Inconclusive("No real direct way to verify the result.");
        }


        #endregion
    }

    [TestClass()]
    public class MountedJCFVFSTests
    {
        JCDVFS testVFS = null;

        [TestInitialize()]
        public void MountedInitializer()
        {
            if (File.Exists(TestVariables.FilePath()))
                File.Delete(TestVariables.FilePath());
            else if (!Directory.Exists(TestVariables.TEST_DIRECTORY))
                Directory.CreateDirectory(TestVariables.TEST_DIRECTORY);

            if (File.Exists(TestVariables.SourcePath()))
                File.Delete(TestVariables.SourcePath());

            if (File.Exists(TestVariables.TargetPath()))
                File.Delete(TestVariables.TargetPath());

            testVFS = JCDVFS.Create(TestVariables.FilePath(), TestVariables.SIZE_STANDARD);
            testVFS.Close();
            testVFS = JCDVFS.Open(TestVariables.FilePath());
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
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"\dir", false);
            var list = testVFS.ListDirectory(testVFS.GetCurrentDirectory());
            Assert.AreEqual(1, list.Length);
        }

        [TestMethod()]
        public void CreateDirectoryNormalParentTrueTest()
        {
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"\dir\another", true);
            var list = testVFS.ListDirectory(testVFS.GetCurrentDirectory() + @"\dir");
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
        public void ImportNormalTest()
        {
            var freeBefore = testVFS.FreeSpace();
            createFile(TestVariables.TEST_DIRECTORY + @"source.txt", TestVariables.SIZE_SMALL);
            testVFS.ImportFile(TestVariables.TEST_DIRECTORY + @"source.txt", @"vfsSrc.txt");
            var freeAfter = testVFS.FreeSpace();
            Assert.IsTrue(freeBefore > freeAfter);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidFileException),
        "The fact that the file is too big for the VFS was discovered.")]
        public void ImportFileTooBigTest()
        {
            string sourceFile = TestVariables.TEST_DIRECTORY + @"source.txt";
            createFile(sourceFile, (long)TestVariables.SIZE_STANDARD + 8);
            testVFS.ImportFile(sourceFile, @"vfsSrc.txt");
            Assert.Inconclusive("No way to verify the result, but no exception was thrown.");
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
        public void ExportFileNotExistingTest()
        {
            testVFS.ExportFile(@"vfsSrc.txt", TestVariables.TargetPath());
            Assert.Inconclusive("No way to verify the result, but no exception was thrown.");
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
            createFile(TestVariables.SourcePath(), TestVariables.SIZE_SMALL);
            testVFS.ImportFile(TestVariables.SourcePath(), @"vfsSrc.txt");
            testVFS.DeleteFile(@"vfsSrc.txt", false);
            Assert.Inconclusive("No way to verify the result");
        }

        #endregion

        /*
         [TestMethod()]
         public void RenameFileTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void MoveFileTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void ListDirectoryTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void SetCurrentDirectoryTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void GetCurrentDirectoryTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void CombinePathWithCurrentDirectoryTest()
         {
             Assert.Fail();
         }*/
    }
}
