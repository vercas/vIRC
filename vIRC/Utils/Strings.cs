using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace vIRC.Utils
{
    /// <summary>
    /// Contains utilitary methods related to strings.
    /// </summary>
    public static class Strings
    {
        static Regex FormatRegex = new Regex("(\u0002|\u001D|\u001F|\u0016|\u000F|\u0003(\\d(\\d(,\\d\\d?)?)?)?)"
            , RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Enumerates through all the lines in the given string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="includeLastEmptyLine">True to return an empty line at the end if the string ends in a newline, otherwise false.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetLines(this string s, bool includeLastEmptyLine = false)
        {
            int startPos = -1;
            bool lastIsCr = false;

            for (int i = 0; i < s.Length; i += char.IsSurrogatePair(s, i) ? 2 : 1)
            {
                if (s[i] == '\r')
                    lastIsCr = true;
                else if (s[i] == '\n')
                {
                    if (startPos == -1)
                        yield return "";    //  Consecutive newlines (or first character being a newline) means an empty line.
                    else
                        yield return s.Substring(startPos, i - startPos - (lastIsCr ? 1 : 0));
                    //  If this is a newline after a carriage return (Windows-style newline), skip the carriage return.

                    startPos = -1;
                    lastIsCr = false;
                }
                else
                {
                    if (startPos == -1)
                        startPos = i;

                    lastIsCr = false;
                }
            }

            if (startPos != -1)
                yield return s.Substring(startPos, s.Length - startPos - (lastIsCr ? 1 : 0));
            else if (includeLastEmptyLine)
                yield return "";
        }

        /// <summary>
        /// Splits a string into substrings around the given separators.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="seps">Characters which delimit the strings.</param>
        /// <param name="options"><see cref="System.StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array returned; or <see cref="System.StringSplitOptions.None"/> to include empty array elements in the array returned.</param>
        /// <param name="start">The position within the sring where parsing will begin.</param>
        /// <param name="len">The length of the area within the string where parsing will occur.</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitEx(this string s, char[] seps, StringSplitOptions options, int start, int len)
        {
            if (seps == null)
                throw new ArgumentNullException("seps");
            if (start < 0 || start > s.Length)
                throw new ArgumentOutOfRangeException("start", "Starting position must be within the boundaries of the string.");
            if (start + len > s.Length)
                throw new ArgumentOutOfRangeException("len", "Length (plus start position) exceeds the boundaries of the string.");

            int startPos = -1, end = start + len;
            bool includeEmpty = !options.HasFlag(StringSplitOptions.RemoveEmptyEntries);

            for (int i = start; i < end; i += char.IsSurrogatePair(s, i) ? 2 : 1)
            {
                if (seps.Contains(s[i]))
                {
                    if (startPos == -1)
                    {
                        //  Consecutive separators (or first character being a separator) means an empty string.

                        if (includeEmpty)
                            yield return "";
                    }
                    else
                        yield return s.Substring(startPos, i - startPos);

                    startPos = -1;
                }
                else if (startPos == -1)
                    startPos = i;
            }

            if (startPos != -1)
                yield return s.Substring(startPos, end - startPos);
            else if (includeEmpty)
                yield return "";
        }

        /// <summary>
        /// Splits a string into substrings around the given separators.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="seps">Characters which delimit the strings.</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitEx(this string s, params char[] seps)
        {
            return SplitEx(s, seps, StringSplitOptions.None, 0, s.Length);
        }
        
        /// <summary>
        /// Splits a string into substrings around the given separators.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="seps">Characters which delimit the strings.</param>
        /// <param name="options"><see cref="System.StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array returned; or <see cref="System.StringSplitOptions.None"/> to include empty array elements in the array returned.</param>
        /// <param name="start">The position within the sring where parsing will begin.</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitEx(this string s, char[] seps, StringSplitOptions options, int start = 0)
        {
            return SplitEx(s, seps, options, start, s.Length - start);
        }

        /// <summary>
        /// Splits a string into substrings around separators identified by the given function.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="sepFinder">Function which determines whether a separator is found at a specific point in the string or not.</param>
        /// <param name="options"><see cref="System.StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array returned; or <see cref="System.StringSplitOptions.None"/> to include empty array elements in the array returned.</param>
        /// <param name="start">The position within the sring where parsing will begin.</param>
        /// <param name="len">The length of the area within the string where parsing will occur.</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitEx(this string s, Func<string, int, int> sepFinder, StringSplitOptions options, int start, int len)
        {
            if (sepFinder == null)
                throw new ArgumentNullException("sepFinder");
            if (start < 0 || start > s.Length)
                throw new ArgumentOutOfRangeException("start", "Starting position must be within the boundaries of the string.");
            if (start + len > s.Length)
                throw new ArgumentOutOfRangeException("len", "Length (plus start position) exceeds the boundaries of the string.");

            int startPos = -1, end = start + len;
            bool includeEmpty = !options.HasFlag(StringSplitOptions.RemoveEmptyEntries);

            for (int i = start; i < end; /* nothing */)
            {
                int res = sepFinder(s, i);

                if (res > 0)
                {
                    if (startPos == -1)
                    {
                        //  Consecutive separators (or first character being a separator) means an empty string.

                        if (includeEmpty)
                            yield return "";
                    }
                    else
                        yield return s.Substring(startPos, i - startPos);

                    startPos = -1;
                    i += res;
                }
                else
                {
                    if (startPos == -1)
                        startPos = i;

                    if (res == 0)
                        i += char.IsSurrogatePair(s, i) ? 2 : 1;
                    else
                        i -= res;
                }
            }

            if (startPos != -1)
                yield return s.Substring(startPos, end - startPos);
            else if (includeEmpty)
                yield return "";
        }

        /// <summary>
        /// Strips the mIRC formatting characters from the given string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string StripFormatting(this string str)
        {
            return FormatRegex.Replace(str, "");
        }

        /// <summary>
        /// Transforms the given string to be suitable output from a bot.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Botify(this string str)
        {
            if (str.StartsWith("\u200B", StringComparison.Ordinal))
                return str;
            else
                return "\u200B" + str;
        }

        /// <summary>
        /// Determines whether the given string is a message from a bot or not.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBotified(this string str)
        {
            return str.StartsWith("\u200B", StringComparison.Ordinal);
        }
        
        /// <summary>
        /// Determines whether the string represents an IRC action or not.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsIrcAction(this string str)
        {
            return str.StartsWith("\u0001ACTION ", StringComparison.Ordinal)
                && str[str.Length - 1] == '\u0001';
        }

        /// <summary>
        /// Obtains the text of the IRC action in the given string, if any.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>
        /// A null string if the given string is not an action; otherwise a string representing the text of the action.
        /// </returns>
        public static string GetIrcAction(this string str)
        {
            if (IsIrcAction(str))
                return str.Substring(8, str.Length - 9);
            else
                return null;
        }

        /// <summary>
        /// Turns the given string into an IRC action.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string TurnIntoIrcAction(this string str)
        {
            return "\u0001ACTION " + str + '\u0001';
        }
    }
}
