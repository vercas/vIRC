using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.CaseMappings
{
    /// <summary>
    /// A-Z equivalent to a-z, and []\^ equivalent to {}|~
    /// </summary>
    public class Rfc1459 : IEqualityComparer<string>, INormalizer
    {
        /// <summary>
        /// Normalizes the given string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Normalize(string str)
        {
            StringBuilder sb = new StringBuilder(str);

            for (int i = 0; i < sb.Length; ++i)
                switch (sb[i])
                {
                case '{': sb[i] = '['; break;
                case '}': sb[i] = ']'; break;
                case '|': sb[i] = '\\'; break;
                case '~': sb[i] = '^'; break;

                default:
                    if (char.IsLower(sb[i]))
                        sb[i] = char.ToUpperInvariant(sb[i]);
                    break;
                }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the specified strings are equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(string x, string y)
        {
            if (x == null)
                return false;
            if (y == null)
                return false;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; ++i)
                switch (x[i])
                {
                case '{': case '[':  if (y[i] != '['  && y[i] != '[') return false; break;
                case '}': case ']':  if (y[i] != ']'  && y[i] != '}') return false; break;
                case '|': case '\\': if (y[i] != '\\' && y[i] != '|') return false; break;
                case '~': case '^':  if (y[i] != '^'  && y[i] != '~') return false; break;

                default:
                    if (char.ToUpperInvariant(x[i]) != char.ToUpperInvariant(y[i]))
                        return false;
                    break;
                }

            return true;
        }

        /// <summary>
        /// Returns a hash code of the specified string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(string obj)
        {
            StringBuilder sb = new StringBuilder(obj);

            for (int i = 0; i < sb.Length; ++i)
                switch (sb[i])
                {
                case '{': sb[i] = '['; break;
                case '}': sb[i] = ']'; break;
                case '|': sb[i] = '\\'; break;
                case '~': sb[i] = '^'; break;
                }

            return StringComparer.OrdinalIgnoreCase.GetHashCode(sb.ToString());
        }
    }
}
