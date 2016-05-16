using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.ChannelJoined"/> and <see cref="vIRC.IrcClient.ChannelParted"/>.
    /// </summary>
    public class ChannelJoinedPartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannel"/> which was joined or parted.
        /// </summary>
        public IrcChannel Channel { get; private set; }

        /// <summary>
        /// Gets whether the channel was joined (true) or parted (false).
        /// </summary>
        public bool Joined { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.ChannelJoinedPartedEventArgs"/> class with the given channel.
        /// </summary>
        /// <param name="chan">The channel which was joined or parted.</param>
        /// <param name="joined">Whether the channel was joined (true) or parted (false).</param>
        public ChannelJoinedPartedEventArgs(IrcChannel chan, bool joined)
        {
            if (chan == null)
                throw new ArgumentNullException("chan");

            this.Channel = chan;
            this.Joined = joined;
        }
    }
}
