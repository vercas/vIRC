using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents an <see cref="vIRC.IrcUser"/> in a <see cref="vIRC.IrcChannel"/>.
    /// </summary>
    public class IrcChannelUser
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcClient"/> associated with this channel user.
        /// </summary>
        public IrcClient Client { get; private set; }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannel"/> associated with this channel user.
        /// </summary>
        public IrcChannel Channel { get; private set; }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcUser"/> associated with this channel user.
        /// </summary>
        public IrcUser User { get; private set; }

        /// <summary>
        /// Gets the modes of this channel user.
        /// </summary>
        public IReadOnlyList<char> Modes { get { return this.modes; } }
        internal List<char> modes = new List<char>();

        /// <summary>
        /// Gets whether the user has parted the channel or not.
        /// </summary>
        public bool Parted { get; internal set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcChannelUser"/> class with the given client, channel and user.
        /// </summary>
        /// <param name="cl"><see cref="vIRC.IrcClient"/> associated with this channel user.</param>
        /// <param name="ch"><see cref="vIRC.IrcChannel"/> associated with this channel user.</param>
        /// <param name="us"><see cref="vIRC.IrcUser"/> associated with this channel user.</param>
        internal IrcChannelUser(IrcClient cl, IrcChannel ch, IrcUser us)
        {
            if (cl == null)
                throw new ArgumentNullException("cl");
            if (ch == null)
                throw new ArgumentNullException("ch");
            if (us == null)
                throw new ArgumentNullException("us");

            this.Client = cl;
            this.Channel = ch;
            this.User = us;
        }
    }
}
