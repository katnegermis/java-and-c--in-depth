using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace vfs.core.tests {
    [TestClass]
    public class JCDFATEventTests {
        private const uint MB1 = 1000000;
        private const uint MB50 = 50 * MB1;

        [TestMethod]
        public void TestModifiedEvent() {
            // Set up
            var testName = "test_modified_event";
            var vfs = CreateVFS(testName);
            var fileName = "file";
            var fileSize = MB1;
            vfs.CreateFile(fileName, (uint)fileSize, false);
            var fs = vfs.GetFileStream(fileName);


            // Test
            var data = GenerateRandomData((int)fileSize, 1);
            // Add function to be called on FileModified event.
            vfs.FileModified += (path, startByte, inData) => {
                AreEqual(data, inData);
            };
            fs.Write(data, 0, (int)fileSize);

            CloseVFS(vfs, testName);
        }

        private JCDFAT CreateVFS(string testName, uint size) {
            DeleteFiles(new string[] { testName });
            return JCDFAT.Create(testName, size);
        }

        private JCDFAT CreateVFS(string testName) {
            return CreateVFS(testName, MB50);
        }

        private void CloseVFS(JCDFAT vfs, string testName) {
            vfs.Close();
            DeleteFiles(new string[] { testName });
        }

        private void DeleteFiles(string[] files) {
            foreach (var file in files) {
                try {
                    File.Delete(file);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }

        private byte[] GenerateRandomData(int size, int seed) {
            var data = new byte[size];
            var rnd = new Random(seed);
            rnd.NextBytes(data);
            return data;
        }

        private void AreEqual(byte[] arr1, byte[] arr2) {
            Assert.AreEqual(arr1.Length, arr2.Length);
            for (int i = 0; i < arr1.Length; i += 1) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }
    }
}
