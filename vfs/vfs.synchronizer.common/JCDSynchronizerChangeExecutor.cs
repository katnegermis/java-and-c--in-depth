using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.core;
using vfs.synchronizer.common;

namespace vfs.synchronizer.common
{
    public class JCDSynchronizerChangeExecutor
    {

        public static void Execute(string vfs, List<Tuple<int, byte[]>> changeList)
        {
            foreach (Tuple<int, byte[]> tuple in changeList)
                Execute(vfs, tuple.Item1, tuple.Item2);
        }

        public static void Execute(string vfs, int eventType, byte[] changeData)
        {
            string vfsPath;
            string newPath;
            long offset;
            long newSize;
            long size;
            byte[] data;
            bool isFolder;

            switch (eventType)
            {
                case (int)JCDSynchronizationEventType.Added:
                    JCDSynchronizerSerialization.Deserialize<string, long, bool>(JCDSynchronizationEventType.Added, changeData, out vfsPath, out size, out isFolder);
                    makeAdd(vfs, vfsPath, size ,isFolder);
                    break;
                case (int)JCDSynchronizationEventType.Deleted:
                    JCDSynchronizerSerialization.Deserialize<string>(JCDSynchronizationEventType.Deleted, changeData, out vfsPath);
                    makeDelete(vfs, vfsPath);
                    break;
                case (int)JCDSynchronizationEventType.Moved:
                    JCDSynchronizerSerialization.Deserialize<string, string>(JCDSynchronizationEventType.Moved, changeData, out vfsPath, out newPath);
                    makeMove(vfs, vfsPath, newPath);
                    break;
                case (int)JCDSynchronizationEventType.Modified:
                    JCDSynchronizerSerialization.Deserialize<string, long, byte[]>(JCDSynchronizationEventType.Modified, changeData, out vfsPath, out offset, out data);
                    makeModify(vfs, vfsPath, offset, data);
                    break;
                case (int)JCDSynchronizationEventType.Resized:
                    JCDSynchronizerSerialization.Deserialize<string, long>(JCDSynchronizationEventType.Resized, changeData, out vfsPath, out newSize);
                    makeResize(vfs, vfsPath, newSize);
                    break;
                default:
                    Console.WriteLine(String.Format("Execution of a change of type {0} failed", eventType));
                    break;
            }
        }

        private static void makeAdd(string hfsPath, string vfsPath, long size, bool isFolder)
        {
            using (var vfs = JCDFAT.Open(hfsPath))
            {
                if (isFolder)
                {
                    vfs.CreateDirectory(vfsPath, false);
                }
                else
                {
                    vfs.CreateFile(vfsPath, (ulong)size, false);
                }
            }
        }

        private static void makeDelete(string hfsPath, string vfsPath)
        {
            using (var vfs = JCDFAT.Open(hfsPath))
            {
                FileAttributes attr = File.GetAttributes(hfsPath);

                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    vfs.DeleteFile(vfsPath, true);
                else
                    vfs.DeleteFile(vfsPath, false);
            }
        }

        private static void makeMove(string hfsPath, string oldPath, string newPath)
        {
            using (var vfs = JCDFAT.Open(hfsPath))
            {
                vfs.MoveFile(oldPath, newPath);
            }
        }

        private static void makeModify(string hfsPath, string vfsPath, long offset, byte[] data)
        {
            using (var vfs = JCDFAT.Open(hfsPath))
            {
                using (var stream = vfs.GetFileStream(vfsPath))
                {
                    stream.Seek(offset, System.IO.SeekOrigin.Begin);
                    stream.Write(data, 0, 0);
                }
            }
        }

        private static void makeResize(string hfsPath, string vfsPath, long newSize)
        {
            using (var vfs = JCDFAT.Open(hfsPath))
            {
                using (var stream = vfs.GetFileStream(vfsPath)) {
                    stream.SetLength(newSize);
                }
            }
        }

    }
}
