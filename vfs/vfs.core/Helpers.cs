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
    }
}
