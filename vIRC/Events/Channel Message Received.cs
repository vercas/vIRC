using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.ChannelMessageReceived"/>.
    /// </summary>
    public class ChannelMessageReceivedEventArgs : MessageReceivedEventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> from which the message arrived.
        /// </summary>
        public IrcChannelUser ChannelUser { get; internal set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.ChannelMessageReceivedEventArgs"/> class with the given channel user, message and message type.
        /// </summary>
        /// <param name="chanUser">The channel from which the message was received.</param>
        /// <param name="msg">The received message.</param>
        /// <param name="type">The type of message received.</param>
        public ChannelMessageReceivedEventArgs(IrcChannelUser chanUser, string msg, IrcMessageTypes type)
            : base(msg, type)
        {
            if (chanUser == null)
                throw new ArgumentNullException("chanUser");

            this.ChannelUser = chanUser;
        }
    }
}
