using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vIRC
{
    using Events;
    using Utils;

    /// <summary>
    /// A client for the IRC protocol.
    /// </summary>
    public class IrcClient
    {
        internal static readonly string EndMessageString = "\r\n";
        internal static readonly byte[] EndMessageBytes = Encoding.ASCII.GetBytes(EndMessageString);
        /// <summary>
        /// Maximum size of an IRC message.
        /// </summary>
        public const int MaximumMessageSize = 512;

        Stream stream = null;
        TcpClient tcpClient = null;
        Encoding enc = Encoding.UTF8;
        internal CaseMappings.INormalizer normalizer = new CaseMappings.Rfc1459();
        //  The default normalizer is the most strict.

        internal ConcurrentDictionary<string, IrcUser> users = new ConcurrentDictionary<string, IrcUser>(StringComparer.OrdinalIgnoreCase);
        internal ConcurrentDictionary<string, IrcChannel> channels = new ConcurrentDictionary<string, IrcChannel>(StringComparer.OrdinalIgnoreCase);

        private object nickChangeSyncer = new object();
        //  Easiest way to avoid a name swap exploit.

        /// <summary>
        /// Gets the IRC protocol in use.
        /// </summary>
        public IrcProtocols Protocol { get; internal set; }

        /// <summary>
        /// Gets the status of the IRC client.
        /// </summary>
        public IrcClientStatus Status
        {
            get
            {
                return (IrcClientStatus)this.status;
            }
            private set
            {
                this.status = (int)value;
            }
        }
        int status = (int)IrcClientStatus.Offline;

        /// <summary>
        /// Gets the information known about the server, if any.
        /// </summary>
        public IrcServerInformation ServerInformation { get; internal set; }

        /// <summary>
        /// Gets the <see cref="vIRC.CaseMappings.INormalizer"/> used for normalizing, comparing and hashing nicknames and channel names.
        /// </summary>
        public CaseMappings.INormalizer NameNormalizer { get { return this.normalizer; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcClient"/> class.
        /// </summary>
        public IrcClient()
        {
            this.Protocol = IrcProtocols.Invalid;
        }

        #region Connection

        /// <summary>
        /// Raised when the client connected to the server.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Raised when the client quit from the server.
        /// </summary>
        public event EventHandler Quit;

        TaskCompletionSource<bool> connectionSource = null;
        TaskCompletionSource<bool> quitSource = null;

        /// <summary>
        /// Connects the client to the given server, and monitors cancellation requests.
        /// </summary>
        /// <param name="target">URI indicating the server to connect to and the protocol to use.</param>
        /// <param name="id">Identification data to present to the server.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task ConnectAsync(Uri target, IrcClientIdentification id, CancellationToken cancellationToken)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (id == null)
                throw new ArgumentNullException("id");

            if (target.Scheme.Equals("ircs", StringComparison.InvariantCultureIgnoreCase))
                this.Protocol = IrcProtocols.IrcSsl;
            else if (target.Scheme.Equals("irc", StringComparison.InvariantCultureIgnoreCase))
                this.Protocol = IrcProtocols.Irc;

            if (this.Protocol == IrcProtocols.Invalid)
                throw new UriFormatException("Available protocols are \"irc\" and \"ircs\".");

            if ((int)IrcClientStatus.Offline != Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.Connecting, (int)IrcClientStatus.Offline))
                throw new NotSupportedException("Only a client which is offline can connect to a server.");

            this.tcpClient = new TcpClient();

            await this.tcpClient.ConnectAsync(target.Host, target.IsDefaultPort ? (this.Protocol == IrcProtocols.IrcSsl ? 6697 : 6667) : target.Port);

            if (this.Protocol == IrcProtocols.IrcSsl)
            {
                SslStream sslStream = new SslStream(this.tcpClient.GetStream(), false, RemoteCertificateValidationCallback);

                this.stream = sslStream;

                await sslStream.AuthenticateAsClientAsync(target.Host);
            }
            else
                this.stream = this.tcpClient.GetStream();

            if ((int)IrcClientStatus.Connecting != Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.LoggingIn, (int)IrcClientStatus.Connecting))
                return;
            //  Failure to compare-exchange here means quitting was requested.

            if (id.ServerPassword != null)
                await this.WriteMessagesAsync(MessageBuilder.Pass(id.ServerPassword), cancellationToken);

            await this.WriteMessagesAsync(MessageBuilder.Nick(id.Nickname).Concat(MessageBuilder.User(id.Username, id.RealName)), cancellationToken);

            this.ServerInformation = new IrcServerInformation();
            this.connectionSource = new TaskCompletionSource<bool>();

            this.StartReceiving();

            await this.connectionSource.Task;
            this.connectionSource = null;

            if ((int)IrcClientStatus.LoggingIn != Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.Online, (int)IrcClientStatus.LoggingIn))
                return;
            //  Failure to compare-exchange here too means quitting was requested.
        }

        /// <summary>
        /// Connects the client to the given server.
        /// </summary>
        /// <param name="target">URI indicating the server to connect to and the protocol to use.</param>
        /// <param name="id">Identification data to present to the server.</param>
        /// <returns></returns>
        public Task ConnectAsync(Uri target, IrcClientIdentification id)
        {
            return this.ConnectAsync(target, id, CancellationToken.None);
        }

        /// <summary>
        /// Disconnects the client from the server, with the given reason, and monitors cancellation requests.
        /// </summary>
        /// <param name="reason">Reason for quitting; null for no reason.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task QuitAsync(string reason, CancellationToken cancellationToken)
        {
            if ((int)IrcClientStatus.Connecting == Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.Quitting, (int)IrcClientStatus.Connecting))
            {
                //  It appears to just be connecting, so killing the socket should be enough.

                try
                {
                    this.tcpClient?.Close();
                }
                catch { /* Swallow exceptions. They are irrelevant. */ }
            }
            else if ((int)IrcClientStatus.LoggingIn == Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.Quitting, (int)IrcClientStatus.LoggingIn))
            {
                //  It is logging in, so a connection is at least established. It may be sending and receiving messages.
                //  So a QUIT message is sent and the connection is killed immediately after. No need for a confirmation.

                try
                {
                    await this.WriteMessagesAsync(MessageBuilder.Quit(reason), cancellationToken);
                }
                catch { /* Swallow exceptions. They are irrelevant. */ }

                try
                {
                    this.tcpClient.Close();
                }
                catch { /* Swallow exceptions. They are irrelevant. */ }
            }
            else if ((int)IrcClientStatus.Online == Interlocked.CompareExchange(ref this.status, (int)IrcClientStatus.Quitting, (int)IrcClientStatus.Online))
            {
                //  Being online means everything needs to be done properly. A QUIT message is sent and a response
                //  is anticipated.

                var task = (quitSource = new TaskCompletionSource<bool>()).Task;

                try
                {
                    await this.WriteMessagesAsync(MessageBuilder.Quit(reason), cancellationToken);
                }
                catch { /* Swallow exceptions. They are irrelevant. */ }

                await task;

                try
                {
                    if (this.tcpClient.Connected)
                        this.tcpClient.Close();
                    //  Maybe it was already killed.
                }
                catch { /* Swallow exceptions. They are irrelevant. */ }
            }
            else
                return;

            this.Quit?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Disconnects the client from the server, with the given reason.
        /// </summary>
        /// <param name="reason">Reason for quitting; null for no reason.</param>
        /// <returns></returns>
        public Task QuitAsync(string reason = null)
        {
            return this.QuitAsync(reason, CancellationToken.None);
        }

        #endregion

        #region Receipt

        private byte[] receiveBuffer = new byte[MaximumMessageSize];
        private StringBuilder stringBuilder = new StringBuilder(MaximumMessageSize);

        private async void ReceiveData(object state)
        {
            this.stringBuilder.Clear();
            string str;

            do
            {
                int read;

                try
                {
                    read = await this.stream.ReadAsync(this.receiveBuffer, 0, this.receiveBuffer.Length);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (read == 0)
                    break;

                stringBuilder.Append(enc.GetString(this.receiveBuffer, 0, read));

                do
                {
                    str = stringBuilder.ToString();
                    int endIndex = str.IndexOf("\r\n");

                    if (endIndex < 0)
                        break;
                    //  No newline means more pieces of this message are required.

                    stringBuilder.Remove(0, endIndex + 2);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    this.ProcessMessageAsync(str, endIndex);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                } while (this.stringBuilder.Length >= 2 && this.Status != IrcClientStatus.Offline);
            } while (this.Status != IrcClientStatus.Offline);

            this.Status = IrcClientStatus.Offline;

            Trace.WriteLine("Connection appears to be closed.");
        }

        private void StartReceiving()
        {
            Task.Factory.StartNew(ReceiveData, null, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Processes the IRC message in the given substring.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="len"></param>
        protected virtual async Task ProcessMessageAsync(string str, int len)
        {
            Trace.WriteLine(string.Format("Processing message: {0}", str.Substring(0, len)));
            
            var prlst = new List<string>(15);
            string cmd = null;
            Prefix pref = null;

            var enumerator = MessageParsing.Parse(str, 0, len).GetEnumerator();

            bool moved = enumerator.MoveNext();
            Trace.Assert(moved, "Empty IRC message", string.Format("An IRC message seems to contain no valid component: {0}", str.Substring(0, len)));

            if (enumerator.Current.IsSource)
            {
                pref = new Prefix(enumerator.Current.String.Substring(enumerator.Current.Start, enumerator.Current.Length));

                moved = enumerator.MoveNext();
                Trace.Assert(moved, "Malformed IRC message", string.Format("An IRC message seems to contain no valid component: {0}", str.Substring(0, len)));
            }

            cmd = enumerator.Current.String.Substring(enumerator.Current.Start, enumerator.Current.Length);

            MessageHandler han = null;
            
            if (!Handlers.TryGetValue(cmd, out han))
            {
#if TRACE
                Trace.WriteLine(string.Format("Unhandled IRC message command: {0} (source: {1})", cmd, pref?.ToString() ?? "NULL"));

                while (enumerator.MoveNext())
                    Trace.WriteLine(string.Format("\t{0}", enumerator.Current));
#endif

                return;
            }

            while (enumerator.MoveNext())
                prlst.Add(enumerator.Current.String.Substring(enumerator.Current.Start, enumerator.Current.Length));

            await han(this, pref, prlst);
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// A method which can handle an IRC message.
        /// </summary>
        /// <param name="cl"></param>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public delegate Task MessageHandler(IrcClient cl, Prefix source, List<string> args);

        private static Dictionary<string, MessageHandler> Handlers = new Dictionary<string, MessageHandler>(StringComparer.Ordinal)
        {
            { "PING", HandlerPing },

            { "NICK", HandlerNick },
            { "MODE", HandlerMode },

            { "JOIN", HandlerJoin },
            { "PART", HandlerPart },
            { "QUIT", HandlerQuit },

            { "ERROR", HandlerError },

            { "PRIVMSG", HandlerPrivmsg },
            { "NOTICE", HandlerNotice },

            { "001", Handler001 },
            { "004", Handler004 },
            { RPL_ISUPPORT.NumericCommand, RPL_ISUPPORT.Handler },

            { "305", Handler305 },
            { "306", Handler306 },

            { "332", Handler332 },
            { "353", Handler353 },
            { "366", Handler366 },
            { "396", Handler396 },

            { "433", Handler433 },
            { "436", Handler436 },
        };

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async Task HandlerPing(IrcClient cl, Prefix source, List<string> args)
        {
            await cl.WriteMessagesAsync(MessageBuilder.Pong(args[0]));
        }

        private static async Task HandlerNick(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(source?.Nickname != null, "A NICK message was received with an invalid prefix!");

            IrcUser u;
            bool found, added = false, removed = false;

            lock (cl.nickChangeSyncer)
                if (found = cl.users.TryGetValue(source.Nickname, out u))
                    if (added = cl.users.TryAdd(args[0], u))
                        removed = cl.users.TryRemove(source.Nickname, out u);

            Debug.Assert(found);
            //  The former nick must have been registered!

            if (!u.Quit)
            {
                //  The client hasn't quit in the meantime, so all three operations should have succeeded.

                Debug.Assert(added);
                Debug.Assert(removed);

                u.Nickname = args[0];

                if (u == cl.LocalUser)
                    Interlocked.Exchange(ref cl.nickChangeCompletionSource, null)?.SetResult(NickChangeResult.Success);

                var e = new UserNicknameChangedEventArgs(u, source.Nickname);

                cl.UserNicknameChanged?.Invoke(cl, e);
                u.OnNicknameChanged(e);
            }
        }

        private static async Task HandlerMode(IrcClient cl, Prefix source, List<string> args)
        {
            bool add = false;
            int paramCursor = 2;

            if (args[1][0] == '+')
                add = true;
            else if (args[1][0] != '-')
                Trace.WriteLine(string.Format("Invalid modes specification: {0}", args[1]));

            if (source?.Nickname == args[0])
            {
                //  Spec says the source and nick must be identical.

                var u = cl._GetUser(args[0]);

                if (add)
                    u.modes.AddRange(args[1].Skip(1));
                else
                    u.modes.RemoveAll(c => args[1].IndexOf(c) > 0);

                if (cl.LocalUser == u && cl.connectionSource != null)
                {
                    cl.connectionSource.SetResult(true);

                    cl.Connected?.Invoke(cl, new EventArgs());
                }
            }
            else
            {
                if (cl.ServerInformation.ChannelTypes.IndexOf(args[0][0]) >= 0)
                {
                    //  This appears to be a channel.

                    var c = cl._GetChannel(args[0]);

                    foreach (var modeChar in args[1].Skip(1))
                    {
                        var m = cl.ServerInformation.ChannelModes[modeChar];

                        Trace.Assert(m.Type != ChannelModeTypes.Unknown, "Unknown mode type for " + modeChar);

                        if (m.Type == ChannelModeTypes.Prefix)
                        {
                            var u = cl._GetUser(args[paramCursor++]);
                            var cu = c._GetUser(u);

                            if (add)
                                cu.modes.Add(modeChar);
                            else
                                cu.modes.Remove(modeChar);
                        }
                    }
                }
            }
        }

        private static async Task HandlerJoin(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(source?.Nickname != null, "A JOIN message was received with an invalid prefix!");

            var u = cl._GetUser(source.Nickname);
            var c = cl._GetChannel(args[0]);
            IrcChannelUser cu;

            if (u == cl.LocalUser && !c.Joined)
            {
                c.UsersUpToDate = false;
                c.users.Clear();
                c.LocalUser = cu = c._GetUser(u);

                c.Joined = true;
            }
            else
            {
                cu = c._GetUser(u);

                c.OnUserJoined(new ChannelUserJoinedEventArgs(cu));
            }

            if (source.Username != null) u.Username = source.Username;
            if (source.Hostname != null) u.Hostname = source.Hostname;
            //  Update the user info...

            //Interlocked.Exchange(ref c.joinCompletionSource, null)?.SetResult(true);
        }

        private static async Task HandlerPart(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(source?.Nickname != null, "A PART message was received with an invalid prefix!");

            var u = cl._GetUser(source.Nickname);
            IrcChannel c;

            if (source.Username != null) u.Username = source.Username;
            if (source.Hostname != null) u.Hostname = source.Hostname;
            //  Update the user info...

            if (u == cl.LocalUser)
            {
                cl.channels.TryRemove(args[0], out c);
                c.UsersUpToDate = c.Joined = false;
                c.partingStatus = 2;

                if (c.LocalUser != null)
                    c.LocalUser.Parted = true;

                Interlocked.Exchange(ref c.partCompletionSource, null)?.SetResult(true);

                cl.ChannelParted?.Invoke(cl, new ChannelJoinedPartedEventArgs(c, false));
            }
            else
            {
                c = cl._GetChannel(args[0]);
                IrcChannelUser cu;

                if (c.users.TryRemove(u, out cu))
                {
                    cu.Parted = true;

                    c.OnUserParted(new ChannelUserPartedEventArgs(cu, false));
                }
            }
        }

        private static async Task HandlerQuit(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(source?.Nickname != null, "A QUIT message was received with an invalid prefix!");

            var u = cl._GetUser(source.Nickname);
            u.Quit = true;

            if (source.Username != null) u.Username = source.Username;
            if (source.Hostname != null) u.Hostname = source.Hostname;
            //  Update the user info...

            if (u == cl.LocalUser)
            {
                //  HUH??
            }
            else
            {
                foreach (var c in cl.channels.Values)
                {
                    IrcChannelUser cu;

                    if (c.users.TryRemove(u, out cu))
                    {
                        cu.Parted = true;

                        c.OnUserParted(new ChannelUserPartedEventArgs(cu, true));
                    }
                }

                //  TODO: Invoke event.
            }
        }

        private static async Task HandlerError(IrcClient cl, Prefix source, List<string> args)
        {
            Interlocked.Exchange(ref cl.quitSource, null)?.SetResult(true);
        }

        private static async Task HandlerPrivmsg(IrcClient cl, Prefix source, List<string> args)
        {
            var u = cl._GetUser(source.Nickname);
            var action = args[1].GetIrcAction();

            if (cl.ServerInformation.ChannelTypes.IndexOf(args[0][0]) >= 0)
            {
                var c = cl._GetChannel(args[0]);
                var cu = c._GetUser(u);

                ChannelMessageReceivedEventArgs e = (action == null)
                    ? new ChannelMessageReceivedEventArgs(cu, args[1], IrcMessageTypes.Standard)
                    : new ChannelMessageReceivedEventArgs(cu, action, IrcMessageTypes.Action);

                cl.ChannelMessageReceived?.Invoke(cl, e);
                c.OnMessageReceived(e);
                cu.OnMessageReceived(e);
                u.OnChannelMessageReceived(e);
            }
            else if (cl.normalizer.Equals(args[0], cl.LocalUser.Nickname))
            {
                UserMessageReceivedEventArgs e = (action == null)
                    ? new UserMessageReceivedEventArgs(u, args[1], IrcMessageTypes.Standard)
                    : new UserMessageReceivedEventArgs(u, action, IrcMessageTypes.Action);

                cl.UserMessageReceived?.Invoke(cl, e);
                u.OnPrivateMessageReceived(e);
            }
            else
                Trace.Fail("Unknown PRIVMSG target: " + args[0]);
        }

        private static async Task HandlerNotice(IrcClient cl, Prefix source, List<string> args)
        {
            if (source.Nickname != null)
            {
                var u = cl._GetUser(source.Nickname);

                if (cl.ServerInformation.ChannelTypes.IndexOf(args[0][0]) >= 0)
                {
                    var c = cl._GetChannel(args[0]);
                    var cu = c._GetUser(u);

                    ChannelMessageReceivedEventArgs e = new ChannelMessageReceivedEventArgs(cu, args[1], IrcMessageTypes.Notice);

                    cl.ChannelMessageReceived?.Invoke(cl, e);
                    c.OnMessageReceived(e);
                    cu.OnMessageReceived(e);
                    u.OnChannelMessageReceived(e);
                }
                else if (cl.normalizer.Equals(args[0], cl.LocalUser.Nickname))
                {
                    UserMessageReceivedEventArgs e = new UserMessageReceivedEventArgs(u, args[1], IrcMessageTypes.Notice);

                    cl.UserMessageReceived?.Invoke(cl, e);
                    u.OnPrivateMessageReceived(e);
                }
                else
                    Trace.Fail("Unknown NOTICE target: " + args[0]);
            }
        }

        private static async Task Handler001(IrcClient cl, Prefix source, List<string> args)
        {
            cl.LocalUser = cl._GetUser(args[0]);
        }

        private static async Task Handler004(IrcClient cl, Prefix source, List<string> args)
        {
            cl.ServerInformation.Name = args[1];
            cl.ServerInformation.Version = args[2];
            cl.ServerInformation.UserModes = args[3];

            foreach (var c in args[4])
                cl.ServerInformation.channelModes.Add(c, new IrcChannelModeSpecification(c));
        }

        private static async Task Handler305(IrcClient cl, Prefix source, List<string> args)
        {
            Interlocked.Exchange(ref cl.awayCompletionSource, null)?.SetResult(false);
        }

        private static async Task Handler306(IrcClient cl, Prefix source, List<string> args)
        {
            Interlocked.Exchange(ref cl.awayCompletionSource, null)?.SetResult(true);
        }

        private static async Task Handler332(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(args[0] == cl.LocalUser.Nickname);

            cl._GetChannel(args[1]).Topic = args[2];
        }

        private static async Task Handler353(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(args[0] == cl.LocalUser.Nickname);

            //  TODO: Handle args[1]
            var c = cl._GetChannel(args[2]);

            foreach (var nick in args[3].SplitEx(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int prefixCnt = 0;

                while (cl.ServerInformation.ChannelUserPrefixes.IndexOf(nick[prefixCnt]) >= 0)
                    ++prefixCnt;
                //  This simply computes the number of prefixes in the nickname.

                var u = cl._GetUser(nick.Substring(prefixCnt));
                var cu = c._GetUser(u);

                for (int i = 0; i < prefixCnt; ++i)
                    cu.modes.Add(cl.ServerInformation.PrefixModes[nick[i]]);
            }
        }

        private static async Task Handler366(IrcClient cl, Prefix source, List<string> args)
        {
            Trace.Assert(args[0] == cl.LocalUser.Nickname);
            
            var c = cl._GetChannel(args[1]);

            c.UsersUpToDate = true;

            var cs = Interlocked.Exchange(ref c.joinCompletionSource, null);

            if (cs != null)
            {
                cs.SetResult(true);

                cl.ChannelJoined?.Invoke(cl, new ChannelJoinedPartedEventArgs(c, true));
            }
        }

        private static async Task Handler396(IrcClient cl, Prefix source, List<string> args)
        {
            cl.LocalUser.Hostname = args[1];
        }

        private static async Task Handler433(IrcClient cl, Prefix source, List<string> args)
        {
            Interlocked.Exchange(ref cl.nickChangeCompletionSource, null)?.SetResult(NickChangeResult.InUse);
        }

        private static async Task Handler436(IrcClient cl, Prefix source, List<string> args)
        {
            Interlocked.Exchange(ref cl.nickChangeCompletionSource, null)?.SetResult(NickChangeResult.Collision);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        #endregion

        #region Writing Messages

        /// <summary>
        /// Sends the given message pieces to the server, and monitors cancellation requests.
        /// </summary>
        /// <param name="pieces"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        protected internal Task WriteMessagesAsync(IEnumerable<object> pieces, CancellationToken cancellationToken)
        {
            if (this.Status == IrcClientStatus.Offline)
                throw new InvalidOperationException("Client must connect to a server before sending any messages.");

            byte[][] msgs = pieces.Select(msg => msg is byte[] ? (byte[])msg : this.enc.GetBytes(msg.ToString())).ToArray();
            byte[] major = new byte[msgs.Sum(ba => ba.Length)];

            for (int i = 0, off = 0; i < msgs.Length; off += msgs[i++].Length)
                Buffer.BlockCopy(msgs[i], 0, major, off, msgs[i].Length);

            Trace.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture
                , "Sending {0} pieces totalling {1} bytes."
                , msgs.Length, major.Length));

            return this.stream.WriteAsync(major, 0, major.Length, cancellationToken);
        }

        /// <summary>
        /// Sends the given message pieces to the server.
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        protected internal Task WriteMessagesAsync(IEnumerable<object> piece)
        {
            return this.WriteMessagesAsync(piece, CancellationToken.None);
        }

        #endregion

        #region SSL Certificates

        /// <summary>
        /// Raised when the server presents a certificate for validation.
        /// </summary>
        public event EventHandler<RemoteCertificateValidationEventArgs> RemoteCertificateValidation;

        bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var ev = RemoteCertificateValidation;

            if (ev == null)
                return sslPolicyErrors == SslPolicyErrors.None;

            var e = new RemoteCertificateValidationEventArgs(certificate, chain, sslPolicyErrors);

            ev.Invoke(this, e);

            return e.Accept ?? (sslPolicyErrors == SslPolicyErrors.None);
        }

        #endregion

        #region Users

        /// <summary>
        /// Gets a collection of known IRC users.
        /// </summary>
        public IEnumerable<IrcUser> Users
        {
            get
            {
                return this.users.Values;
            }
        }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcUser"/> associated with the client.
        /// </summary>
        public IrcUser LocalUser { get; private set; } = null;

        internal IrcUser _GetUser(string nick)
        {
            return this.users.GetOrAdd(nick, n => new IrcUser(n, this));
        }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcUser"/> with the given nickname, if known.
        /// </summary>
        /// <param name="nick"></param>
        /// <returns>A valid <see cref="vIRC.IrcUser"/> if the nickname is known; otherwise null.</returns>
        public IrcUser GetUser(string nick)
        {
            if (nick == null)
                throw new ArgumentNullException("nick");

            //if (this.Status != IrcClientStatus.Online)
            //    throw new InvalidOperationException("Client must be online to deal with users.");

            IrcUser ret = null;

            if (this.users.TryGetValue(this.normalizer.Normalize(nick), out ret))
                return ret;

            return null;
        }

        #endregion

        #region Channels

        /// <summary>
        /// Raised when the client joins a channel.
        /// </summary>
        public event EventHandler<ChannelJoinedPartedEventArgs> ChannelJoined;

        /// <summary>
        /// Raised when the client parts a channel.
        /// </summary>
        public event EventHandler<ChannelJoinedPartedEventArgs> ChannelParted;

        /// <summary>
        /// Gets a collection of known IRC channels.
        /// </summary>
        public IEnumerable<IrcChannel> Channels
        {
            get
            {
                return this.channels.Values;
            }
        }

        internal IrcChannel GetChannel(string name, out bool existed)
        {
            bool exstd = false;

            var ret = this.channels.GetOrAdd(name, n =>
            {
                exstd = false;
                return new IrcChannel(n, this);
            });

            existed = exstd;
            return ret;
        }

        internal IrcChannel _GetChannel(string name)
        {
            return this.channels.GetOrAdd(name, n => new IrcChannel(n, this));
        }

        /// <summary>
        /// Gets the <see cref="vIRC.IrcChannel"/> with the given name, if known.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A valid <see cref="vIRC.IrcChannel"/> if the name is known; otherwise null.</returns>
        public IrcChannel GetChannel(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            //if (this.Status != IrcClientStatus.Online)
            //    throw new InvalidOperationException("Client must be online to deal with channels.");

            IrcChannel ret = null;

            if (this.channels.TryGetValue(this.normalizer.Normalize(name), out ret))
                return ret;

            return null;
        }

        /// <summary>
        /// Joins the given channel, and monitors cancellation requests.
        /// </summary>
        /// <param name="channel">Name of channel to join.</param>
        /// <param name="key">optional; Key to join the channel with; null if none.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IrcChannel> JoinAsync(string channel, string key, CancellationToken cancellationToken)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            if (channel.Length < 1)
                throw new ArgumentOutOfRangeException("Channel name must be non-empty.");
            
            channel = this.normalizer.Normalize(channel);
            //  The name of the channel must be normalized according to the known rules.

            if (this.Status != IrcClientStatus.Online)
                throw new InvalidOperationException("Client must be fully connected to join channels.");

            if (this.ServerInformation.ChannelTypes.IndexOf(channel[0]) < 0)
                throw new ArgumentOutOfRangeException("The given channel type (first character) is not supported by the server.");

            if (channel.Length > this.ServerInformation.ChannelLength)
                throw new ArgumentOutOfRangeException("The given channel name is too long for this server.");

            bool channelExisted;
            IrcChannel chan = this.GetChannel(channel, out channelExisted);

            if (chan.Joined)
                throw new InvalidOperationException("Client has already joined the given channel.");
            else if (channelExisted)
                throw new InvalidOperationException("Client is already attempting to join the given channel.");

            var task = chan.joinCompletionSource.Task;

            await this.WriteMessagesAsync(MessageBuilder.Join(channel, key), cancellationToken);

            await task;

            return chan;
        }

        /// <summary>
        /// Joins the given channel.
        /// </summary>
        /// <param name="channel">Name of channel to join.</param>
        /// <param name="key">optional; Key to join the channel with; null if none.</param>
        /// <returns></returns>
        public Task<IrcChannel> JoinAsync(string channel, string key = null)
        {
            return this.JoinAsync(channel, key, CancellationToken.None);
        }

        /// <summary>
        /// Parts the given channel, and monitors cancellation requests.
        /// </summary>
        /// <param name="chan">The channel to part.</param>
        /// <param name="reason">Reason for parting the channel. Null for no reason.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task PartAsync(IrcChannel chan, string reason, CancellationToken cancellationToken)
        {
            if (chan == null)
                throw new ArgumentNullException("chan");

            if (chan.Client != this)
                throw new InvalidOperationException("The given channel is associated with another client.");

            if (!chan.Joined)
                throw new IndexOutOfRangeException("The client has not joined that channel (anymore).");

            if (this.Status != IrcClientStatus.Online)
                throw new InvalidOperationException("Client must be fully connected to part channels.");

            var oldPartStatus = Interlocked.CompareExchange(ref chan.partingStatus, 1, 0);

            if (oldPartStatus == 1)
                throw new InvalidOperationException("The given channel is already in the process of parting.");
            else if (oldPartStatus == 2)
                throw new InvalidOperationException("The given channel is already parted.");

            var task = (chan.partCompletionSource = new TaskCompletionSource<bool>()).Task;

            await this.WriteMessagesAsync(MessageBuilder.Part(chan.Name, reason), cancellationToken);

            await task;
        }

        /// <summary>
        /// Perts the given channel.
        /// </summary>
        /// <param name="chan">The channel to part.</param>
        /// <param name="reason">optional; Reason for parting the channel. Null for no reason.</param>
        /// <returns></returns>
        public Task PartAsync(IrcChannel chan, string reason = null)
        {
            return this.PartAsync(chan, reason, CancellationToken.None);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Raised when a message is received from a channel.
        /// </summary>
        public event EventHandler<ChannelMessageReceivedEventArgs> ChannelMessageReceived;

        /// <summary>
        /// Raised when a message is received from a user.
        /// </summary>
        public event EventHandler<UserMessageReceivedEventArgs> UserMessageReceived;

        /// <summary>
        /// Sends a message to the given channel, and monitors cancellation requests.
        /// </summary>
        /// <param name="chan">The channel to send the message to.</param>
        /// <param name="message">The message to send to the channel.</param>
        /// <param name="type">The type of message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task SendMessageAsync(IrcChannel chan, string message, IrcMessageTypes type, CancellationToken cancellationToken)
        {
            if (chan == null)
                throw new ArgumentNullException("chan");
            if (message == null)
                throw new ArgumentNullException("message");

            if (chan.Client != this)
                throw new InvalidOperationException("The given channel is associated with another client.");

            if (!chan.Joined)
                throw new IndexOutOfRangeException("The client has not joined that channel (anymore).");

            if (chan.partingStatus != 0)
                throw new InvalidOperationException("The given channel is in the process of parting.");

            switch (type)
            {
            case IrcMessageTypes.Action:
                message = message.TurnIntoIrcAction();
                goto case IrcMessageTypes.Standard;

            case IrcMessageTypes.Standard:
                await this.WriteMessagesAsync(MessageBuilder.Privmsg(chan.Name, message), cancellationToken);
                break;

            case IrcMessageTypes.Notice:
                await this.WriteMessagesAsync(MessageBuilder.Notice(chan.Name, message), cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException("type", "Unknown IRC message type!");
            }
        }

        /// <summary>
        /// Sends a message to the given channel.
        /// </summary>
        /// <param name="chan">The channel to send the message to.</param>
        /// <param name="message">The message to send to the channel.</param>
        /// <param name="type">The type of message to send.</param>
        /// <returns></returns>
        public Task SendMessageAsync(IrcChannel chan, string message, IrcMessageTypes type = IrcMessageTypes.Standard)
        {
            return this.SendMessageAsync(chan, message, type, CancellationToken.None);
        }

        /// <summary>
        /// Sends a message to the given user, and monitors cancellation requests.
        /// </summary>
        /// <param name="user">The user to send the message to.</param>
        /// <param name="message">The message to send to the user.</param>
        /// <param name="type">The type of message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task SendMessageAsync(IrcUser user, string message, IrcMessageTypes type, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (message == null)
                throw new ArgumentNullException("message");

            if (user.Client != this)
                throw new InvalidOperationException("The given user is associated with another client.");

            if (user.Quit)
                throw new InvalidOperationException("The given user has quit the server.");
            
            switch (type)
            {
            case IrcMessageTypes.Action:
                message = message.TurnIntoIrcAction();
                goto case IrcMessageTypes.Standard;

            case IrcMessageTypes.Standard:
                await this.WriteMessagesAsync(MessageBuilder.Privmsg(user.Nickname, message), cancellationToken);
                break;

            case IrcMessageTypes.Notice:
                await this.WriteMessagesAsync(MessageBuilder.Notice(user.Nickname, message), cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException("type", "Unknown IRC message type!");
            }
        }

        /// <summary>
        /// Sends a message to the given user.
        /// </summary>
        /// <param name="user">The user to send the message to.</param>
        /// <param name="message">The message to send to the user.</param>
        /// <param name="type">The type of message to send.</param>
        /// <returns></returns>
        public Task SendMessageAsync(IrcUser user, string message, IrcMessageTypes type = IrcMessageTypes.Standard)
        {
            return this.SendMessageAsync(user, message, type, CancellationToken.None);
        }

        /// <summary>
        /// Sends a message to the given named target, and monitors cancellation requests.
        /// </summary>
        /// <param name="target">The named target to send the message to.</param>
        /// <param name="message">The message to send to the named target.</param>
        /// <param name="type">The type of message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task SendMessageAsync(string target, string message, IrcMessageTypes type, CancellationToken cancellationToken)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (message == null)
                throw new ArgumentNullException("message");

            switch (type)
            {
            case IrcMessageTypes.Action:
                message = message.TurnIntoIrcAction();
                goto case IrcMessageTypes.Standard;

            case IrcMessageTypes.Standard:
                await this.WriteMessagesAsync(MessageBuilder.Privmsg(target, message), cancellationToken);
                break;

            case IrcMessageTypes.Notice:
                await this.WriteMessagesAsync(MessageBuilder.Notice(target, message), cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException("type", "Unknown IRC message type!");
            }
        }

        /// <summary>
        /// Sends a message to the given named target.
        /// </summary>
        /// <param name="target">The named target to send the message to.</param>
        /// <param name="message">The message to send to the named target.</param>
        /// <param name="type">The type of message to send.</param>
        /// <returns></returns>
        public Task SendMessageAsync(string target, string message, IrcMessageTypes type = IrcMessageTypes.Standard)
        {
            return this.SendMessageAsync(target, message, type, CancellationToken.None);
        }

        #endregion

        #region Nicknames

        /// <summary>
        /// Raised when a user changes their nickname.
        /// </summary>
        public event EventHandler<UserNicknameChangedEventArgs> UserNicknameChanged;

        TaskCompletionSource<NickChangeResult> nickChangeCompletionSource = null;
        int nickChangeState = 0;

        /// <summary>
        /// Attempts to change the nickname of the local user, and monitors cancellation requests.
        /// </summary>
        /// <param name="newNickname"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<NickChangeResult> ChangeNicknameAsync(string newNickname, CancellationToken cancellationToken)
        {
            if (newNickname == null)
                throw new ArgumentNullException("newNickname");

            if (this.Status != IrcClientStatus.Online)
                throw new InvalidOperationException("Client must be fully connected to change its nickname.");

            if (newNickname.Length == 0 || newNickname.Length > this.ServerInformation.NickLength)
                throw new ArgumentOutOfRangeException("newNickname", "The given nickname is either empty or too long for this server.");

            if (!Validation.IsNick(newNickname))
                throw new FormatException("The given nickname contains invalid characters.");

            if (0 != Interlocked.Exchange(ref this.nickChangeState, 1))
                throw new InvalidOperationException("A nickname change is already in progress.");

            var t = (this.nickChangeCompletionSource = new TaskCompletionSource<NickChangeResult>()).Task;

            await this.WriteMessagesAsync(MessageBuilder.Nick(newNickname), cancellationToken);

            await t;

            Thread.VolatileWrite(ref this.nickChangeState, 0);

            return t.Result;
        }

        /// <summary>
        /// Attempts to change the nickname of the local user.
        /// </summary>
        /// <param name="newNickname"></param>
        /// <returns></returns>
        public Task<NickChangeResult> ChangeNicknameAsync(string newNickname)
        {
            return this.ChangeNicknameAsync(newNickname, CancellationToken.None);
        }

        #endregion

        #region Away
        
        TaskCompletionSource<bool> awayCompletionSource = null;
        int awayState = 0;

        /// <summary>
        /// Sets the away state of the local user, and monitors cancellation requests.
        /// </summary>
        /// <param name="reason">Reason for being away; null for un-away.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>True if the client becomes away; false if the away status was removed.</returns>
        public async Task<bool> SetAwayAsync(string reason, CancellationToken cancellationToken)
        {
            if (this.Status != IrcClientStatus.Online)
                throw new InvalidOperationException("Client must be fully connected to change its away status.");

            if (this.ServerInformation.AwayLength.HasValue
                && reason != null && reason.Length > this.ServerInformation.AwayLength)
                throw new ArgumentOutOfRangeException("Away reason is too long for this server.");
            
            if (0 != Interlocked.Exchange(ref this.awayState, 1))
                throw new InvalidOperationException("An away status change is already in progress.");

            var t = (this.awayCompletionSource = new TaskCompletionSource<bool>()).Task;

            await this.WriteMessagesAsync(MessageBuilder.Away(reason), cancellationToken);

            await t;

            Thread.VolatileWrite(ref this.awayState, 0);

            return t.Result;
        }

        /// <summary>
        /// Sets the away state of the local user.
        /// </summary>
        /// <param name="reason">Reason for being away; null for un-away.</param>
        /// <returns>True if the client becomes away; false if the away status was removed.</returns>
        public Task<bool> SetAwayAsync(string reason)
        {
            return this.SetAwayAsync(reason, CancellationToken.None);
        }

        #endregion
    }
}
