using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Known IRC user modes.
    /// </summary>
    public static class UserModes
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const char Away = 'a';
        public const char Invisible = 'i';
        public const char ReceivesWallops = 'w';
        public const char RestrictedConnection = 'r';
        public const char Operator = 'o';
        public const char LocalOperator = 'O';
        public const char ReceivesServerNotices = 's';
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Types of IRC channel modes.
    /// </summary>
    public enum ChannelModeTypes
    {
        /// <summary>
        /// Mode type is not known (yet?).
        /// </summary>
        Unknown,
        /// <summary>
        /// Mode adds and removes the parameter (mandatory) from a list.
        /// </summary>
        A,
        /// <summary>
        /// Mode changes a setting and always has a parameter.
        /// </summary>
        B,
        /// <summary>
        /// Mode changes a setting only only has a parameter when set.
        /// </summary>
        C,
        /// <summary>
        /// Mode changes a setting and never has a parameter.
        /// </summary>
        D,
        /// <summary>
        /// Channel mode applies to a user. A nickname is the parameter.
        /// </summary>
        Prefix,
    }

    /// <summary>
    /// Represents specifications about an IRC channel mode.
    /// </summary>
    public class IrcChannelModeSpecification
    {
        /// <summary>
        /// Gets the character representing this mode.
        /// </summary>
        public char Character { get; internal set; }

        /// <summary>
        /// Gets the type of this channel mode.
        /// </summary>
        public ChannelModeTypes Type { get; internal set; }

        internal IrcChannelModeSpecification(char c)
        {
            this.Character = c;

            switch (c)
            {
            default:
                this.Type = ChannelModeTypes.Unknown;
                break;
            }
        }
    }
}
