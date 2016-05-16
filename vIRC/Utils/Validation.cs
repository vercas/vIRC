using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace vIRC.Utils
{
    /// <summary>
    /// Contains utilitary methods for validating input.
    /// </summary>
    public static class Validation
    {
        internal static Regex NickValidator = new Regex(@"^[a-zA-Z\[\]\\`_\^\{\}\|][a-zA-Z\[\]\\`_\^\{\}\|\d\-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Determines whether the given nickname is valid or not.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNick(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            return NickValidator.IsMatch(str);
        }
    }
}
