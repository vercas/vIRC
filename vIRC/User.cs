using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents a user in IRC.
    /// </summary>
    public class IrcUser
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcClient"/> associated with this user.
        /// </summary>
        public IrcClient Client { get; private set; }

        /// <summary>
        /// Gets the nickname of the user.
        /// </summary>
        public string Nickname { get; internal set; }

        /// <summary>
        /// Gets the hostname of the user.
        /// </summary>
        public string Hostname { get; internal set; } = null;

        /// <summary>
        /// Gets the username of the user.
        /// </summary>
        public string Username { get; internal set; } = null;

        /// <summary>
        /// Gets the real name of the user.
        /// </summary>
        public string RealName { get; internal set; } = null;

        internal List<char> modes = new List<char>();

        /// <summary>
        /// Gets a list of modes of this user.
        /// </summary>
        public IReadOnlyList<char> Modes { get; private set; }

        /// <summary>
        /// Gets whether the user has quit the server or not.
        /// </summary>
        public bool Quit { get; internal set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcUser"/> class with the given nickname and client.
        /// </summary>
        /// <param name="nickname">Nickname of the user.</param>
        /// <param name="cl"><see cref="vIRC.IrcClient"/> associated with this user.</param>
        internal IrcUser(string nickname, IrcClient cl)
        {
            if (nickname == null)
                throw new ArgumentNullException("nickname");
            if (cl == null)
                throw new ArgumentNullException("cl");

            this.Client = cl;
            this.Nickname = nickname;
            this.Modes = this.modes.AsReadOnly();
        }

        #region Messaging

        /// <summary>
        /// Sends a message to this user, and monitors cancellation requests.
        /// </summary>
        /// <param name="message">The message to send to the user.</param>
        /// <param name="type">The type of message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public Task SendMessageAsync(string message, IrcMessageTypes type, CancellationToken cancellationToken)
        {
            return this.Client.SendMessageAsync(this, message, type, cancellationToken);
        }

        /// <summary>
        /// Sends a message to this user.
        /// </summary>
        /// <param name="message">The message to send to the user.</param>
        /// <param name="type">The type of message to send.</param>
        /// <returns></returns>
        public Task SendMessageAsync(string message, IrcMessageTypes type = IrcMessageTypes.Standard)
        {
            return this.Client.SendMessageAsync(this, message, type, CancellationToken.None);
        }

        #endregion
    }
}
