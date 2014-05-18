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
using vfs.synchronizer.client;

namespace vfs.core.synchronizer.tests {
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class JCDVFSSynchronizerTests {
        private const uint MB1 = 1000000;
        private const uint MB5 = MB1 * 5;
        Type vfsType = typeof(JCDFAT);

        [TestMethod]
        public void TestSynchronizerCreate() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFileName = TestHelpers.GetTestFileName(testName);

            // Test
            var sync = JCDVFSSynchronizer.Create(vfsType, testFileName, MB5);

            CleanUp(sync, testName);
        }

        [TestMethod]
        public void TestSynchronizerDelete() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFileName = TestHelpers.GetTestFileName(testName);
            TestHelpers.DeleteFiles(testFileName);
            var sync = JCDVFSSynchronizer.Create(vfsType, testFileName, MB5);
            Assert.IsTrue(File.Exists(testFileName));
            sync.Close();

            // Test
            JCDVFSSynchronizer.Delete(vfsType, testFileName);
            Assert.IsFalse(File.Exists(testFileName));
        }

        [TestMethod]
        public void TestSynchronizerOpen() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var testFileName = TestHelpers.GetTestFileName(testName);
            TestHelpers.DeleteFiles(testFileName);
            var sync = JCDVFSSynchronizer.Create(vfsType, testFileName, MB5);
            sync.Close();

            // Test
            sync = JCDVFSSynchronizer.Open(vfsType, testFileName);

            CleanUp(sync, testName);
        }

        [TestMethod]
        public void TestSynchronizerCreateFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var sync = CreateSynchronizer(testName);
            long fileSize = 5;
            var filePath = "/file";

            // Test
            sync.CreateFile(filePath, (ulong)fileSize, false);
            var stream = sync.GetFileStream(filePath);
            Assert.AreEqual(fileSize, stream.Length);

            stream.Close();
            CleanUp(sync, testName);
        }

        [TestMethod]
        public void TestSynchronizerCreatefolder() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var sync = CreateSynchronizer(testName);
            var filePath = "/folder";

            // Test
            sync.CreateDirectory(filePath, false);

            CleanUp(sync, testName);
        }

        private JCDVFSSynchronizer CreateSynchronizer(string testName) {
            return CreateSynchronizer(testName, MB5);
        }

        private JCDVFSSynchronizer CreateSynchronizer(string testName, uint size) {
            testName = TestHelpers.GetTestFileName(testName);
            TestHelpers.DeleteFiles(new string[] { testName });
            return JCDVFSSynchronizer.Create(vfsType, testName, size);
        }

        private void CleanUp(JCDVFSSynchronizer sync, string testName) {
            sync.Close();
            TestHelpers.DeleteFiles(new string[] { TestHelpers.GetTestFileName(testName) });
        }
    }
}
