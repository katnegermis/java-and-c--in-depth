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

        public static string PathCombine(string path, string fileName)
        {
            return System.IO.Path.Combine(path, fileName);
        }

        public static string PathGetDirectoryName(string path)
        {
            //return System.IO.Path.GetDirectoryName(path);
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
            //return System.IO.Path.GetFileName(path);
            var tmp = TrimLastSlash(path);
            return tmp.Substring(tmp.LastIndexOf("/") + 1);
        }

        /*public static bool PathIsValid(Uri path) {
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
        }*/

        public static bool PathIsValid(string path) {
            return true;
        }
        public static bool PathIsValid(string path, bool isFolder) {
            return true;
        }

        public static string TrimLastSlash(string name) {
            return name.TrimEnd(new char[] { '/' });
        }

        /*public static string PathGetFileName(Uri path) {
            var tmp = TrimLastSlash(path.ToString());
            return tmp.Substring(tmp.LastIndexOf('/') + 1);
        }

        public static string GetContainingPath(Uri path) {
            var tmp = TrimLastSlash(path.IsAbsoluteUri ? path.AbsolutePath : path.ToString());
            var slash = tmp.LastIndexOf('/');
            if(slash > -1) {
                tmp = tmp.Remove(slash);
            }
            else {
                return ".";
            }

            return tmp;
            
        }*/
    }
}
