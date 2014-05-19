using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vfs.common;
using vfs.core;
using vfs.exceptions;

namespace vfs.core.tests {

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class JCDFileStreamTests {
        private ulong MB50 = 50000000;
        private int MB5 = 5000000;
        private int MB1 = 1000000;

        [TestMethod]
        public void TestWriteRead10MB() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, MB50);
            int bytes = MB5 * 2;
            var dataIn = TestHelpers.GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);


            // Test
            stream.Seek(0, SeekOrigin.Begin);
            var dataOut = new byte[bytes];
            stream.Read(dataOut, 0, bytes);
            // Verify that all bytes are the same
            TestHelpers.AreEqual(dataIn, dataOut);
            CleanUp(stream, testName);
        }

        [TestMethod]
        public void TestSeekData() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, MB50);
            var bytes = MB5;
            var dataIn = TestHelpers.GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);

            // Test
            // Read 4 MB data, starting from 1MB.
            var dataOut = new byte[bytes - MB1];
            stream.Seek(MB1, SeekOrigin.Begin);
            stream.Read(dataOut, 0, bytes - MB1);
            for (int i = 0; i < bytes - MB1; i += 1) {
                Assert.AreEqual(dataIn[i + MB1], dataOut[i]);
            }
            CleanUp(stream, testName);
        }

        [TestMethod]
        public void TestOffsetReadData() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, MB50);
            var bytes = MB5;
            var outBytes = MB5 - MB1;
            var dataIn = TestHelpers.GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);
            stream.Seek(0L, SeekOrigin.Begin);

            // Test
            // Read 4 MB data, starting from 1MB.
            var dataOut = new byte[bytes - outBytes];
            stream.Read(dataOut, outBytes, bytes - outBytes);
            for (int i = 0; i < bytes - outBytes; i += 1) {
                Assert.AreEqual(dataIn[i + outBytes], dataOut[i]);
            }
            CleanUp(stream, testName);
        }

        [TestMethod]
        public void TestSeekWriteData() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, MB50);
            var bytes = MB5;
            var dataIn = TestHelpers.GenerateRandomData(bytes, 15);
            // Write data 5 megabytes in to file.
            // We expect the file to automatically expand.
            stream.Seek(MB5, SeekOrigin.Begin);
            stream.Write(dataIn, 0, bytes);

            // Test
            // Read 5 MB data, starting from 5MB.
            var dataOut = new byte[bytes];
            stream.Seek(MB5, SeekOrigin.Begin);
            stream.Read(dataOut, 0, bytes);
            TestHelpers.AreEqual(dataIn, dataOut);
            CleanUp(stream, testName);
        }

        [TestMethod]
        public void TestReadSeekReadSeekRead() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, MB50);
            var datas = new byte[5][];
            // Initialize 5 arrays with random data in them.
            for (int i = 0; i < datas.Length; i += 1) {
                datas[i] = TestHelpers.GenerateRandomData(MB1, i);
                stream.Write(datas[i], 0, MB1);
            }

            // Test
            var dataOut = new byte[MB1];
            for (int i = datas.Length - 1; i >= 0; i -= 1) {
                stream.Seek(i * MB1, SeekOrigin.Begin);
                stream.Read(dataOut, 0, MB1);
                TestHelpers.AreEqual(datas[i], dataOut);
            }
            CleanUp(stream, testName);
        }

        [TestMethod]
        [ExpectedException(typeof(BufferTooSmallException), "Buffer was too small!")]
        public void TestBufferSize() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            var stream = CreateJCDAndGetFileStream(testName, (ulong)MB5);
            // We have to set the file length to be greater than 0, which is the default
            // by `CreateJCDAndGetFileStream`.
            var length = 10;
            stream.SetLength(length);

            // Test
            var buffer = new byte[length - 1];
            stream.Read(buffer, 0, length);
        }

        [TestMethod]
        public void TestShrinkFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            TestHelpers.DeleteFiles(new string[] { testName });
            var vfs = JCDFAT.Create(testName, (ulong)MB5);
            var testFileName = "test";
            var fileSize = MB1;
            var stream = vfs.CreateFile(testFileName, (ulong)fileSize, false);
            var jcdBlockSize = 1 << 12;

            // Test
            int shrinkBytes = fileSize / 2;
            var oldFreeSpace = vfs.FreeSpace();

            // Check that stream.Length is updated correctly.
            stream.SetLength(shrinkBytes);
            Assert.AreEqual(fileSize - shrinkBytes, stream.Length);

            // Check that vfs.FreeSpace is updated correctly.
            var blocksShrunk = (fileSize - shrinkBytes) / jcdBlockSize;
            var newFreeSpace = oldFreeSpace + (ulong)(blocksShrunk * jcdBlockSize);
            Assert.AreEqual(newFreeSpace, vfs.FreeSpace());
        }

        [TestMethod]
        public void TestExpandFile() {
            // Set up
            var testName = MethodBase.GetCurrentMethod().Name;
            // Creates new file with size 0.
            var stream = CreateJCDAndGetFileStream(testName, (ulong)MB5);

            // Test
            var length = MB1;
            stream.SetLength(length);
            Assert.AreEqual(stream.Length, length);
            var data = new byte[1];
            stream.Read(data, length - 1, 1);
            // Assumes that initial file is zero.
            Assert.AreEqual(data[0], (byte)'\0');

            var newLength = 1L;
            stream.SetLength(newLength);
            Assert.AreEqual(newLength, stream.Length);
            CleanUp(stream, testName);
        }

        private JCDFileStream CreateJCDAndGetFileStream(string vfsFileName, ulong size) {
            TestHelpers.DeleteFiles(new string[] { vfsFileName });
            var vfs = JCDFAT.Create(vfsFileName, size);
            var testFileName = "test";
            return vfs.CreateFile(testFileName, 0, false);
        }

        private void CleanUp(JCDFileStream stream, string testName) {
            stream.Close();
            stream.GetVFS().Close();
            TestHelpers.DeleteFiles(new string[] { testName });
        }
    }
}
