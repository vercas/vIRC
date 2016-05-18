using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.Disconnected"/>.
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the reason why the client disconnected.
        /// </summary>
        public DisconnectReason Reason { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.DisconnectedEventArgs"/> class with the given disconnect reason.
        /// </summary>
        /// <param name="reason">The reason why the client disconnected.</param>
        public DisconnectedEventArgs(DisconnectReason reason)
        {
            this.Reason = reason;
        }
    }

    /// <summary>
    /// Possibly reasons for an IRC client to disconnect.
    /// </summary>
    public enum DisconnectReason
    {
        /// <summary>
        /// The client asked to quit.
        /// </summary>
        Quit = 0,
        /// <summary>
        /// The connection with the server was lost.
        /// </summary>
        ConnectionLoss = 1,
        /// <summary>
        /// The connection registration failed.
        /// </summary>
        RegistrationFailure = 2,
    }
}
