using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vfs.core;

namespace vfs.core.tests {

    [TestClass]
    public class JCDFileStreamTests
    {
        private ulong MB50 = 50000000;
        private int MB5 = 5000000;
        private int MB1 = 1000000;

        [TestMethod]
        public void TestWriteRead10MB() {
            // Set up
            var stream = CreateJCDAndGetFileStream("test_write_read_10mb", MB50);
            int bytes = MB5 * 2;
            var dataIn = GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);


            // Test
            stream.Seek(0, SeekOrigin.Begin);
            var dataOut = new byte[bytes];
            stream.Read(dataOut, 0, bytes);
            // Verify that all bytes are the same
            AreEqual(dataIn, dataOut);
        }

        [TestMethod]
        public void TestSeekData() {
            // Set up
            var stream = CreateJCDAndGetFileStream("test_seek_data", MB50);
            var bytes = MB5;
            var dataIn = GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);

            // Test
            // Read 4 MB data, starting from 1MB.
            var dataOut = new byte[bytes - MB1];
            stream.Seek(MB1, SeekOrigin.Begin);
            stream.Read(dataOut, 0, bytes - MB1);
            for (int i = 0; i < bytes - MB1; i += 1) {
                Assert.AreEqual(dataIn[i + MB1], dataOut[i]);
            }
        }

        [TestMethod]
        public void TestOffsetReadData() {
            // Set up
            var stream = CreateJCDAndGetFileStream("test_offset_seek_data", MB50);
            var bytes = MB5;
            var dataIn = GenerateRandomData(bytes, 1);
            stream.Write(dataIn, 0, bytes);

            // Test
            // Read 4 MB data, starting from 1MB.
            var dataOut = new byte[bytes - MB1];
            stream.Read(dataOut, MB1, bytes - MB1);
            for (int i = 0; i < bytes - MB1; i += 1) {
                Assert.AreEqual(dataIn[i + MB1], dataOut[i]);
            }
        }

        [TestMethod]
        public void TestSeekWriteData() {
            // Set up
            var stream = CreateJCDAndGetFileStream("test_seek_write_data", MB50);
            var bytes = MB5;
            var dataIn = GenerateRandomData(bytes, 15);
            // Write data 5 megabytes in to file.
            // We expect the file to automatically expand.
            stream.Seek(MB5, SeekOrigin.Begin);
            stream.Write(dataIn, 0, bytes);

            // Test
            // Read 5 MB data, starting from 5MB.
            var dataOut = new byte[bytes];
            stream.Seek(MB5, SeekOrigin.Begin);
            stream.Read(dataOut, 0, bytes);
            AreEqual(dataIn, dataOut);
        }

        [TestMethod]
        public void TestReadSeekReadSeekRead() {
            // Set up
            var stream = CreateJCDAndGetFileStream("test_read_seek_read_seek_read", MB50);
            var datas = new byte[5][];
            // Initialize 5 arrays with random data in them.
            for (int i = 0; i < datas.Length; i += 1) {
                datas[i] = GenerateRandomData(MB1, i);
                stream.Write(datas[i], 0, MB1);
            }

            // Test
            var dataOut = new byte[MB1];
            for (int i = datas.Length - 1; i >= 0; i -= 1) {
                stream.Seek(i * MB1, SeekOrigin.Begin);
                stream.Read(dataOut, 0, MB1);
                AreEqual(datas[i], dataOut);
            }
        }

        public JCDFileStream CreateJCDAndGetFileStream(string vfsFileName, ulong size) {
            DeleteFiles(new string[] { vfsFileName });
            var vfs = JCDFAT.Create(vfsFileName, size);
            var testFileName = "test";
            vfs.CreateFile(testFileName, 0, false);
            return vfs.GetFileStream(testFileName);
        }

        private byte[] GenerateRandomData(int size, int seed) {
            var data = new byte[size];
            var rnd = new Random(seed);
            rnd.NextBytes(data);
            return data;
        }

        private void DeleteFiles(string[] files) {
            foreach (var file in files) {
                try {
                    File.Delete(file);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }

        private void AreEqual(byte[] arr1, byte[] arr2) {
            Assert.AreEqual(arr1.Length, arr2.Length);
            for (int i = 0; i < arr1.Length; i += 1) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }
    }
}
