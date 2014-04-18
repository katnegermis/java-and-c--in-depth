﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace vfs.core.indexing.tests {
    [TestClass]
    public class MyFileTests {
        [TestMethod]
        public void TestMyFileEquality() {
            // Set up
            var f1 = new MyFile("file", "/var/file");
            var f2 = new MyFile("file", "/var/file");
            // Test
            Assert.AreEqual(f1, f2);
            Assert.IsTrue(f1 == f2);
        }

        [TestMethod]
        public void TestMyFileInequality() {
            // Set up
            var f1 = new MyFile("file", "/var/file");
            var f2 = new MyFile("file2", "/var/file");
            // Test
            Assert.AreNotEqual(f1, f2);
            Assert.IsTrue(f1 != f2);

            // Set up
            var f3 = new MyFile("file", "/var/file");
            var f4 = new MyFile("file", "/var/file2");
            // Test
            Assert.AreNotEqual(f3, f4);
            Assert.IsTrue(f3 != f4);
        }
    }
}