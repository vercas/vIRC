using System;
using System.Collections.Generic;

namespace vIRC.Utils
{
    /// <summary>
    /// Contains utilitary methods for parsing IRC messages.
    /// </summary>
    public static class MessageParsing
    {
        /// <summary>
        /// Enumerates through all the IRC message components found in the given string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static IEnumerable<MessageComponent> Parse(string str, int start, int len)
        {
            int end = Math.Min(start + len, str.Length), lastInd = -1, paramInd = 0;
            bool source = false, longParam = false;

            for (int i = start; i < end; ++i)
            {
                if (str[i] == ':')
                {
                    if (lastInd != -1)
                        continue;
                    //  This means it is inside a normal parameter...

                    lastInd = i + 1;

                    if (paramInd == 0)
                        source = true;
                    else
                    {
                        //  If this is a long parameter, there's no reason to iterate over anything else.

                        longParam = true;

                        break;
                    }
                }
                else if (str[i] == ' ')
                {
                    if (lastInd == -1)
                        continue;

                    if (source)
                    {
                        yield return new MessageComponent()
                        {
                            IsSource = true,
                            IsLong = false,
                            String = str,
                            Start = lastInd,
                            Length = i - lastInd,
                            Index = paramInd,
                        };

                        source = false;
                    }
                    else
                        yield return new MessageComponent()
                        {
                            IsSource = false,
                            IsLong = false,
                            String = str,
                            Start = lastInd,
                            Length = i - lastInd,
                            Index = paramInd,
                        };

                    ++paramInd;
                    lastInd = -1;
                }
                else if (lastInd == -1)
                    lastInd = i;
            }

            if (lastInd > -1)
                yield return new MessageComponent()
                {
                    IsSource = source,
                    IsLong = longParam,
                    String = str,
                    Start = lastInd,
                    Length = end - lastInd,
                    Index = paramInd,
                };
        }
    }

    /// <summary>
    /// Represents a component of an IRC protocol message.
    /// </summary>
    public struct MessageComponent
    {
        /// <summary>
        /// True if the component is the source of a message; otherwise false.
        /// </summary>
        public bool IsSource;
        /// <summary>
        /// True if the component is a long parameter; otherwise false.
        /// </summary>
        public bool IsLong;
        /// <summary>
        /// The string containing the message component.
        /// </summary>
        public string String;
        /// <summary>
        /// The index within the string where the component starts.
        /// </summary>
        public int Start;
        /// <summary>
        /// The length of the message component within the string.
        /// </summary>
        public int Length;
        /// <summary>
        /// The index of the component within the message.
        /// </summary>
        public int Index;

        /// <summary>
        /// Returns a string representation of the message component.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "\"{0}\"-{1}-{2}"
                , this.String.Substring(this.Start, this.Length)
                , this.IsLong ? "LNG" : this.IsSource ? "SRC" : "nrm"
                , this.Index);
        }
    }
}
