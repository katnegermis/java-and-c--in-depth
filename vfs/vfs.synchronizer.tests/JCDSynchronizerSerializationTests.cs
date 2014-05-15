using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using vfs.common;
using vfs.synchronizer.common;

namespace vfs.synchronizer.tests {

    [TestClass]
    public class JCDSynchronizerSerializationTests {
        [TestMethod]
        public void TestSynchronizerSerializeDelete() {
            // Set up
            var serializePath = "/var/my/path";
            var type = JCDSynchronizationEventType.Deleted;
            var data = JCDSynchronizerSerialization.Serialize(type, serializePath);

            // Test
            string deserializedPath;
            JCDSynchronizerSerialization.Deserialize(type, data, out deserializedPath);
            Assert.AreEqual(serializePath, deserializedPath);
        }

        [TestMethod]
        public void TestSynchronizerSerializeAdd() {
            // Set up
            var serializePath = "/var/my/path";
            var serializeSize = 500000L;
            var serializedIsFolder = true;
            var type = JCDSynchronizationEventType.Added;
            var data = JCDSynchronizerSerialization.Serialize(type, serializePath, serializeSize, serializedIsFolder);

            // Test
            string deserializedPath;
            long deserializedSize;
            bool deserializedIsFolder;
            JCDSynchronizerSerialization.Deserialize(type, data, out deserializedPath, out deserializedSize, out deserializedIsFolder);
            Assert.AreEqual(serializePath, deserializedPath);
            Assert.AreEqual(serializedIsFolder, deserializedIsFolder);
        }

        [TestMethod]
        public void TestSynchronizerSerializeModify() {
            // Set up
            var serializePath = "/var/my/path";
            var serializeOffset = 50000000L;
            var serializeData = TestHelpers.GenerateRandomData(1 << 10);
            var type = JCDSynchronizationEventType.Modified;
            var data = JCDSynchronizerSerialization.Serialize(type, serializePath, serializeOffset, serializeData);

            // Test
            string deserializedPath;
            long deserializedOffset;
            byte[] deserializedData;
            JCDSynchronizerSerialization.Deserialize(type, data, out deserializedPath,
                                                     out deserializedOffset, out deserializedData);
            Assert.AreEqual(serializePath, deserializedPath);
            Assert.AreEqual(serializeOffset, deserializedOffset);
            TestHelpers.AreEqual(serializeData, deserializedData);
        }

        [TestMethod]
        public void TestSynchronizerSerializeMove() {
            // Set up
            var serializeOldPath = "/var/my/path";
            var serializeNewPath = "/var/my/new/path";
            var type = JCDSynchronizationEventType.Moved;
            var data = JCDSynchronizerSerialization.Serialize(type, serializeOldPath, serializeNewPath);

            // Test
            string deserializedOldPath;
            string deserializedNewPath;
            JCDSynchronizerSerialization.Deserialize(type, data, out deserializedOldPath, out deserializedNewPath);
            Assert.AreEqual(serializeOldPath, deserializedOldPath);
            Assert.AreEqual(serializeNewPath, deserializedNewPath);
        }

        [TestMethod]
        public void TestSynchronizerSerializeResize() {
            // Set up
            var serializePath = "/var/my/path";
            var serializeSize = 60000000L;
            var type = JCDSynchronizationEventType.Resized;
            var data = JCDSynchronizerSerialization.Serialize(type, serializePath, serializeSize);

            // Test
            string deserializedPath;
            long deserializedSize;
            JCDSynchronizerSerialization.Deserialize(type, data, out deserializedPath, out deserializedSize);
            Assert.AreEqual(serializePath, deserializedPath);
            Assert.AreEqual(serializeSize, deserializedSize);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException), "Expected serializer to fail because of too few arguments")]
        public void TestSynchronizerIncorrectNumArgs() {
            // Set up
            var type = JCDSynchronizationEventType.Resized;
            var data = JCDSynchronizerSerialization.Serialize(type, "only one arg");
        }
    }
}
