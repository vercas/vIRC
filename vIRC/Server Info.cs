using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents available information about an IRC server.
    /// </summary>
    public class IrcServerInformation
    {
        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        public string Name { get; internal set; } = null;
        /// <summary>
        /// Gets the version of the server.
        /// </summary>
        public string Version { get; internal set; } = null;
        /// <summary>
        /// Gets the user modes available on the server.
        /// </summary>
        public string UserModes { get; internal set; } = null;

        /// <summary>
        /// Gets the channel modes available on the server.
        /// </summary>
        public IReadOnlyDictionary<char, IrcChannelModeSpecification> ChannelModes { get { return this.channelModes; } }
        internal Dictionary<char, IrcChannelModeSpecification> channelModes = new Dictionary<char, IrcChannelModeSpecification>();
        
        /// <summary>
        /// Gets the maximum nickname length on the server.
        /// </summary>
        public int NickLength { get; internal set; } = 9;
        /// <summary>
        /// Gets the maximum channel name length on the server.
        /// </summary>
        public int ChannelLength { get; internal set; } = 200;
        /// <summary>
        /// Gets the maximum channel topic length on the server.
        /// </summary>
        public int? TopicLength { get; internal set; } = null;
        /// <summary>
        /// Gets the maximum length of a kick message on the server.
        /// </summary>
        public int? KickLength { get; internal set; } = null;
        /// <summary>
        /// Gets the maximum length of an away message on the server.
        /// </summary>
        public int? AwayLength { get; internal set; } = null;

        /// <summary>
        /// Gets the characters that begin channel names on this server.
        /// </summary>
        public string ChannelTypes { get; internal set; } = "#&";
        /// <summary>
        /// Gets the maximum number of channels joinable on this server.
        /// </summary>
        public int? MaxChannels { get; internal set; } = null;
        /// <summary>
        /// Gets the channel limits of this server.
        /// </summary>
        public IReadOnlyList<ChannelLimit> ChannelLimits { get { return this.chanLims; } }
        internal List<ChannelLimit> chanLims = new List<ChannelLimit>();

        /// <summary>
        /// Gets the maximum number of targets allowed for commands on this server.
        /// </summary>
        public int MaxTargets { get; internal set; } = 1;
        /// <summary>
        /// Gets the target limits for specific commands on this server.
        /// </summary>
        public IReadOnlyList<CommandTargetLimit> CommandTargetLimits { get { return this.cmdTrgLims; } }
        internal List<CommandTargetLimit> cmdTrgLims = new List<CommandTargetLimit>();

        /// <summary>
        /// Gets the prefixes that the server allows to use in NOTICEs.
        /// </summary>
        public string NoticePrefixes { get; internal set; } = null;

        /// <summary>
        /// Gets the modes associated with their prefixes on this server.
        /// </summary>
        public IReadOnlyDictionary<char, char> PrefixModes { get { return this.prefixModes; } }
        internal Dictionary<char, char> prefixModes = new Dictionary<char, char>() { { '@', 'o' }, { '+', 'v' } };
        /// <summary>
        /// Gets the channel user prefixes supported by this server.
        /// </summary>
        public string ChannelUserPrefixes { get; internal set; } = "@+";

        /// <summary>
        /// Gets whether the server uses the WHOX protocol or not.
        /// </summary>
        public bool UsesWhox { get; internal set; } = false;

        internal IrcServerInformation()
        {

        }
    }

    /// <summary>
    /// Represents a limit associated with channels with specific prefixes.
    /// </summary>
    public class ChannelLimit
    {
        /// <summary>
        /// Gets the prefixes for which this channel limit applies.
        /// </summary>
        public string Prefixes { get; internal set; }
        /// <summary>
        /// Gets the limit for channels with this prefix.
        /// </summary>
        public int Limit { get; internal set; }

        internal ChannelLimit(string spec)
        {
            int colIndex = spec.IndexOf(':');

            if (colIndex < 0)
                throw new FormatException("Channel limit specification should contain a colon.");

            this.Prefixes = spec.Substring(0, colIndex);
            this.Limit = int.Parse(spec.Substring(colIndex + 1), System.Globalization.CultureInfo.InvariantCulture);

            //System.Diagnostics.Debug.WriteLine("\t\tChannel limit: {0} -> {1}.", this.Prefixes, this.Limit);
        }
    }

    /// <summary>
    /// Represents a limit of targets to a specific command.
    /// </summary>
    public class CommandTargetLimit
    {
        /// <summary>
        /// Gets the prefixes for which this channel limit applies.
        /// </summary>
        public string Command { get; internal set; }
        /// <summary>
        /// Gets the limit for channels with this prefix.
        /// </summary>
        public int TargetLimit { get; internal set; }

        internal CommandTargetLimit(string spec)
        {
            int colIndex = spec.IndexOf(':');

            if (colIndex < 0)
                throw new FormatException("Command target limit specification should contain a colon.");

            this.Command = spec.Substring(0, colIndex);
            this.TargetLimit = int.Parse(spec.Substring(colIndex + 1), System.Globalization.CultureInfo.InvariantCulture);

            //System.Diagnostics.Debug.WriteLine("\t\tChannel limit: {0} -> {1}.", this.Prefixes, this.Limit);
        }
    }
}
