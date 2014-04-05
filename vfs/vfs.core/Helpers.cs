using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core
{
    class Helpers
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

        /*public static string PathCombine(string path, string fileName)
        {
            if (path == null)
            {
                path = "/";
            }
            return System.IO.Path.Combine(path, fileName);
        }*/

        public static string PathGetDirectoryName(string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }

        public static string PathGetFileName(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        public static bool PathIsValid(Uri path) {
            if(!path.IsFile) {
                return false;
            }
            return true;
        }

        public static bool PathIsValid(Uri path, bool isFolder)
        {
            if(!PathIsValid(path)) {
                return false;
            }

            if(!isFolder && path.ToString().EndsWith("/")) {
                return false;
            }

            return true;
        }

        public static string TrimLastSlash(string name) {
            return name.TrimEnd(new char[] { '/' });
        }

        public static string PathGetFileName(Uri path) {
            var tmp = TrimLastSlash(path.ToString());
            return tmp.Substring(tmp.LastIndexOf('/') + 1);
        }
    }
}
