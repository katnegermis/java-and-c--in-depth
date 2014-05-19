using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using vfs.core;

namespace vfs.synchronizer.common
{

    [Serializable]
    public class SynchronizerEvent
    {
        public JCDSynchronizationEventType Type;
        public object[] Data;

        public SynchronizerEvent(JCDSynchronizationEventType type, params object[] data)
        {
            this.Type = type;
            this.Data = data;
        }
    }

    [Serializable]
    public enum JCDSynchronizationEventType
    {
        Modified = 1,
        Added = 2,
        Deleted = 3,
        Moved = 4,
        Resized = 5,
    }

    public static class JCDSynchronizerSerialization
    {
        public static byte[] Serialize(JCDSynchronizationEventType type, params object[] args)
        {
            CheckTypeAndArgs(type, args.Length);
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            var syncEvent = new SynchronizerEvent(type, args);
            bf.Serialize(ms, syncEvent);
            return ms.ToArray();
        }

        private static SynchronizerEvent Deserialize(JCDSynchronizationEventType type, byte[] data)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream(data);
            var syncEvent = (SynchronizerEvent)bf.Deserialize(ms);
            if (syncEvent.Type != type)
            {
                throw new SerializationException(String.Format("Expected Deleted event, but got {0}", syncEvent.Type.ToString()));
            }
            CheckTypeAndArgs(type, syncEvent.Data.Length);
            return syncEvent;
        }

        public static void Deserialize<T1>(JCDSynchronizationEventType type, byte[] data, out T1 arg1)
        {
            var syncEvent = Deserialize(type, data);
            arg1 = (T1)syncEvent.Data[0];
        }

        public static void Deserialize<T1, T2>(JCDSynchronizationEventType type, byte[] data, out T1 arg1, out T2 arg2)
        {
            var syncEvent = Deserialize(type, data);
            arg1 = (T1)syncEvent.Data[0];
            arg2 = (T2)syncEvent.Data[1];
        }

        public static void Deserialize<T1, T2, T3>(JCDSynchronizationEventType type, byte[] data, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            var syncEvent = Deserialize(type, data);
            arg1 = (T1)syncEvent.Data[0];
            arg2 = (T2)syncEvent.Data[1];
            arg3 = (T3)syncEvent.Data[2];
        }

        private static void CheckTypeAndArgs(JCDSynchronizationEventType type, int args)
        {
            switch (type)
            {
                case JCDSynchronizationEventType.Added:
                    if (args != NumArgs(typeof(AddFileEventHandler)))
                    {
                        throw new SerializationException("Incorrect number of args for this type!");
                    }
                    break;
                case JCDSynchronizationEventType.Deleted:
                    if (args != NumArgs(typeof(DeleteFileEventHandler)))
                    {
                        throw new SerializationException("Incorrect number of args for this type!");
                    }
                    break;
                case JCDSynchronizationEventType.Modified:
                    if (args != NumArgs(typeof(ModifyFileEventHandler)))
                    {
                        throw new SerializationException("Incorrect number of args for this type!");
                    }
                    break;
                case JCDSynchronizationEventType.Moved:
                    if (args != NumArgs(typeof(MoveFileEventHandler)))
                    {
                        throw new SerializationException("Incorrect number of args for this type!");
                    }
                    break;
                case JCDSynchronizationEventType.Resized:
                    if (args != NumArgs(typeof(ResizeFileEventHandler)))
                    {
                        throw new SerializationException("Incorrect number of args for this type!");
                    }
                    break;
            }
        }

        private static int NumArgs(Type type) {
            var v = type.GetMethod("Invoke");
            return v.GetParameters().Length;
        }
    }
}
