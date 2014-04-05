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
            if (path == null)
            {
                path = "/";
            }
            return System.IO.Path.Combine(path, fileName);
        }

        public static string PathGetDirectoryName(string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }

        public static string PathGetFileName(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        public static bool PathIsValid(string path)
        {
            return true;
        }
    }
}
