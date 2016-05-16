using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.CaseMappings
{
    /// <summary>
    /// Represents an object which can turn a string to a normalized representation.
    /// </summary>
    public interface INormalizer : IEqualityComparer<string>
    {
        /// <summary>
        /// Normalizes the given string.
        /// </summary>
        /// <param name="str">String to normalize.</param>
        /// <returns>Normalized representation of the given string.</returns>
        string Normalize(string str);
    }
}
