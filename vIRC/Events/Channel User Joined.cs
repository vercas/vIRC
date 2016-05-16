using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcChannel.UserJoined"/>.
    /// </summary>
    public class ChannelUserJoinedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> who parted.
        /// </summary>
        public IrcChannelUser User { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.ChannelUserJoinedEventArgs"/> class with the given channel user.
        /// </summary>
        /// <param name="user">The channel which was joined or parted.</param>
        public ChannelUserJoinedEventArgs(IrcChannelUser user)
        {
            if (user == null)
                throw new ArgumentNullException("chan");

            this.User = user;
        }
    }
}
