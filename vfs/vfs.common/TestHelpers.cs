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
            return name + ".test";
        }
    }
}
