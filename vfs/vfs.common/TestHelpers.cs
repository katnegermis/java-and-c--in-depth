using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.common {
    public class TestHelpers {

        public static void DeleteFiles(string[] files) {
            foreach (var file in files) {
                try {
                    File.Delete(file);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }

        public static void DeleteFiles(string file) {
            DeleteFiles(new string[] { file });
        }

        public static void DeleteFolders(string folder, bool recursive) {
            DeleteFolders(new string[] { folder }, recursive);
        }

        public static void DeleteFolders(string[] folders, bool recursive) {
            foreach (var folder in folders) {
                try {
                    Directory.Delete(folder, recursive);
                }
                catch (System.IO.FileNotFoundException) { }
            }
        }

        public static byte[] GenerateRandomData(int size) {
            var rnd = new Random();
            return GenerateRandomData(size, rnd.Next());
        }

        public static byte[] GenerateRandomData(int size, int seed) {
            var data = new byte[size];
            var rnd = new Random(seed);
            rnd.NextBytes(data);
            return data;
        }

        public static void AreEqual(byte[] arr1, byte[] arr2) {
            Assert.AreEqual(arr1.Length, arr2.Length);
            for (int i = 0; i < arr1.Length; i += 1) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }

        public static string GetTestFileName(string name) {
            return GetTestFileName(name, "");
        }

        public static string GetTestFileName(string name, string extra) {
            return name + "_" + extra + ".test";
        }

        public static bool HostFileCompare(string hfsFile1, string hfsFile2) {
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (hfsFile1 == hfsFile2) {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(hfsFile1, FileMode.Open);
            fs2 = new FileStream(hfsFile2, FileMode.Open);
            return StreamCompare(fs1, fs2);
        }
        /// <summary>
        /// Helper method to compare 2 files (byte by byte)
        /// </summary>
        /// <param name="file1">First file path</param>
        /// <param name="file2">Second file path</param>
        /// <returns>true if equal, false otherwise</returns>
        public static bool StreamCompare(Stream fs1, Stream fs2) {

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length) {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            int fs1Byte;
            int fs2Byte;

            do {
                fs1Byte = fs1.ReadByte();
                fs2Byte = fs2.ReadByte();
            }
            while (fs1Byte == fs2Byte && fs1Byte != -1);

            // The files will only be fully read if they were the same.
            return (fs1.Position == fs1.Length && fs2.Position == fs2.Length);
        }
    }
}
