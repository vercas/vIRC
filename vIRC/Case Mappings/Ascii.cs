using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.CaseMappings
{
    /// <summary>
    /// A-Z equivalent to a-z.
    /// </summary>
    public class Ascii : IEqualityComparer<string>, INormalizer
    {
        /// <summary>
        /// Normalizes the given string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Normalize(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            return str.ToUpperInvariant();
        }

        /// <summary>
        /// Determines whether the specified strings are equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(string x, string y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x, y);
        }

        /// <summary>
        /// Returns a hash code of the specified string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(string obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
        }
    }
}
