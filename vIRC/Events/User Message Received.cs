using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.UserMessageReceived"/>.
    /// </summary>
    public class UserMessageReceivedEventArgs : MessageReceivedEventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcUser"/> from which the message arrived.
        /// </summary>
        public IrcUser User { get; internal set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.UserMessageReceivedEventArgs"/> class with the given user, message and message type.
        /// </summary>
        /// <param name="user">The user from which the message was received.</param>
        /// <param name="msg">The received message.</param>
        /// <param name="type">The type of message received.</param>
        public UserMessageReceivedEventArgs(IrcUser user, string msg, IrcMessageTypes type)
            : base(msg, type)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            this.User = user;
        }
    }
}
