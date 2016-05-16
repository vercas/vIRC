using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Base event data for message receipt events.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// Gets the type of the message received.
        /// </summary>
        public IrcMessageTypes MessageType { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.MessageReceivedEventArgs"/> class with the given message and message type.
        /// </summary>
        /// <param name="msg">The received message.</param>
        /// <param name="type">The type of message received.</param>
        public MessageReceivedEventArgs(string msg, IrcMessageTypes type)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            
            this.Message = msg;
            this.MessageType = type;
        }
    }
}
