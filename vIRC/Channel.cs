using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vIRC
{
    using Events;
    using Utils;

    /// <summary>
    /// Represents a channel in IRC.
    /// </summary>
    public class IrcChannel
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcClient"/> associated with this channel.
        /// </summary>
        public IrcClient Client { get; private set; }

        /// <summary>
        /// Gets whether the client has joined this channel or not.
        /// </summary>
        public bool Joined { get; internal set; } = false;
        internal TaskCompletionSource<bool> joinCompletionSource = new TaskCompletionSource<bool>();
        internal TaskCompletionSource<bool> partCompletionSource = null;
        internal int partingStatus = 0;

        /// <summary>
        /// Gets whether the channel's user list is up to date or not.
        /// </summary>
        public bool UsersUpToDate { get; internal set; } = false;

        /// <summary>
        /// Gets the name of the channel.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the topic of the channel.
        /// </summary>
        public string Topic { get; internal set; } = null;
        
        /// <summary>
        /// Gets a list of modes of this channel.
        /// </summary>
        public IReadOnlyList<char> Modes { get; private set; }
        internal List<char> modes = new List<char>();

        #region Users

        /// <summary>
        /// Raised when a user joins the channel.
        /// </summary>
        public event EventHandler<ChannelUserJoinedEventArgs> UserJoined;

        /// <summary>
        /// Raised when a user parts the channel, simply or by quitting the network.
        /// </summary>
        public event EventHandler<ChannelUserPartedEventArgs> UserParted;

        internal void OnUserJoined(ChannelUserJoinedEventArgs e)
        {
            this.UserJoined?.Invoke(this, e);
        }

        internal void OnUserParted(ChannelUserPartedEventArgs e)
        {
            this.UserParted?.Invoke(this, e);
        }

        /// <summary>
        /// Gets a collection of known IRC users on this channel.
        /// </summary>
        public IEnumerable<IrcChannelUser> Users { get { return this.users.Values; }}
        internal System.Collections.Concurrent.ConcurrentDictionary<IrcUser, IrcChannelUser> users = new System.Collections.Concurrent.ConcurrentDictionary<IrcUser, IrcChannelUser>();

        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> associated with the client on this channel.
        /// </summary>
        public IrcChannelUser LocalUser { get; internal set; } = null;

        internal IrcChannelUser _GetUser(IrcUser us)
        {
            return this.users.GetOrAdd(us, u => new IrcChannelUser(this.Client, this, u));
        }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> with the given nickname, if known.
        /// </summary>
        /// <param name="nick"></param>
        /// <returns>A valid <see cref="vIRC.IrcChannelUser"/> if the nickname is known on this channel; otherwise null.</returns>
        public IrcChannelUser GetUser(string nick)
        {
            var us = this.Client.GetUser(nick);

            if (us == null)
                return null;

            IrcChannelUser ret = null;

            if (this.users.TryGetValue(us, out ret))
                return ret;

            return null;
        }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannelUser"/> associated with the given <see cref="vIRC.IrcUser"/>, if known.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>A valid <see cref="vIRC.IrcChannelUser"/> if the user is known on this channel; otherwise null.</returns>
        public IrcChannelUser GetUser(IrcUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            if (user.Client != this.Client)
                throw new NotSupportedException("The given user belongs to a different client!");

            IrcChannelUser ret = null;

            if (this.users.TryGetValue(user, out ret))
                return ret;

            return null;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcChannel"/> class with the given name and client.
        /// </summary>
        /// <param name="name">Name of the channel.</param>
        /// <param name="cl"><see cref="vIRC.IrcClient"/> associated with this channel.</param>
        internal IrcChannel(string name, IrcClient cl)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (cl == null)
                throw new ArgumentNullException("cl");

            this.Client = cl;
            this.Name = name;
            this.Modes = this.modes.AsReadOnly();
        }

        #region Parting

        /// <summary>
        /// Parts this channel, and monitors cancellation requests.
        /// </summary>
        /// <param name="reason">Reason for parting the channel. Null for no reason.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public Task PartAsync(string reason, CancellationToken cancellationToken)
        {
            return this.Client.PartAsync(this, reason, cancellationToken);
        }

        /// <summary>
        /// Parts this channel.
        /// </summary>
        /// <param name="reason">optional; Reason for parting the channel. Null for no reason.</param>
        /// <returns></returns>
        public Task PartAsync(string reason = null)
        {
            return this.PartAsync(reason, CancellationToken.None);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Sends a message to this channel, and monitors cancellation requests.
        /// </summary>
        /// <param name="message">The message to send to the channel.</param>
        /// <param name="type">The type of message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public Task SendMessageAsync(string message, IrcMessageTypes type, CancellationToken cancellationToken)
        {
            return this.Client.SendMessageAsync(this, message, type, cancellationToken);
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="message">The message to send to the channel.</param>
        /// <param name="type">The type of message to send.</param>
        /// <returns></returns>
        public Task SendMessageAsync(string message, IrcMessageTypes type = IrcMessageTypes.Standard)
        {
            return this.Client.SendMessageAsync(this, message, type, CancellationToken.None);
        }

        #endregion
    }
}
