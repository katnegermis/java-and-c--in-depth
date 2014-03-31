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
        public const ulong SIZE_STANDARD = UInt32.MaxValue;

        public static string FilePath()
        {
            return Path.GetFullPath(TestVariables.TEST_DIRECTORY + TestVariables.TEST_FILE);
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
        public void CreateNormal()
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
        public void CreateWithFileExisting()
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
            var list = testVFS.ListDirectory(testVFS.GetCurrentDirectory()+ @"\dir");
            Assert.AreEqual(1, list.Length);
        }

        [TestMethod()]
        public void CreateDirectoryParentMissingTest()
        {
            testVFS.CreateDirectory(testVFS.GetCurrentDirectory() + @"\dir\another", false);
            Assert.Inconclusive("No way to verify the result.Some exception might have to be thrown.");
        }

        #endregion

        /* [TestMethod()]
         public void ImportFileTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void ExportFileTest()
         {
             Assert.Fail();
         }

         [TestMethod()]
         public void DeleteFileTest()
         {
             Assert.Fail();
         }

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
