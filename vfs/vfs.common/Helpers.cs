using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace vfs.common
{
    public class Helpers
    {
        /// <summary>
        /// Round up integer divison
        /// </summary>
        /// <param name="num">Numerator</param>
        /// <param name="den">Denominator</param>
        /// <returns></returns>
        public static ulong ruid(ulong num, ulong den)
        {
            return (num + den - 1) / den;
        }

        public static long ruid(long num, long den) {
            return (num + den - 1) / den;
        }

        public static int ruid(int num, int den) {
            return (num + den - 1) / den;
        }

        public static uint ruid(uint num, uint den) {
            return (num + den - 1) / den;
        }

        public static string PathCombine(string path, string fileName)
        {
            path = path.Replace('\\', '/');
            if (!path.EndsWith("/")) {
                path += "/";
            }
            return System.IO.Path.Combine(path, fileName);
        }

        public static string PathGetDirectoryName(string path)
        {
            var tmp = TrimLastSlash(path);
            var slash = tmp.LastIndexOf('/');
            if(slash > -1) {
                tmp = tmp.Remove(slash + 1);
            }
            else {
                return ".";
            }

            return tmp;
        }

        public static string PathGetFileName(string path)
        {
            var tmp = TrimLastSlash(path);
            return tmp.Substring(tmp.LastIndexOf("/") + 1);
        }

        public static string TrimLastSlash(string name) {
            return name.TrimEnd(new char[] { '/' });
        }

        public static bool FileNameIsValid(string name) {
            return (name != "." && name != ".." && name.IndexOf('/') < 0);
        }
    }
}
