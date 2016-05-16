using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcChannel.UserParted"/>.
    /// </summary>
    public class ChannelUserPartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> who parted.
        /// </summary>
        public IrcChannelUser User { get; private set; }

        /// <summary>
        /// Gets whether the user quit (true) or simply parted (false).
        /// </summary>
        public bool Quit { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.ChannelUserPartedEventArgs"/> class with the given channel user.
        /// </summary>
        /// <param name="user">The channel which was joined or parted.</param>
        /// <param name="quit">Whether the user quit (true) or simply parted (false).</param>
        public ChannelUserPartedEventArgs(IrcChannelUser user, bool quit)
        {
            if (user == null)
                throw new ArgumentNullException("chan");

            this.User = user;
            this.Quit = quit;
        }
    }
}
