namespace vIRC
{
    /// <summary>
    /// Represents the status of an <see cref="vIRC.IrcClient"/> relative to a server.
    /// </summary>
    public enum IrcClientStatus
    {
        /// <summary>
        /// The client is not connected to a server.
        /// </summary>
        Offline = 0,
        /// <summary>
        /// The client is establishing a TCP/SSL connection to the server.
        /// </summary>
        Connecting = 1,
        /// <summary>
        /// The client is presenting its identity to the server.
        /// </summary>
        LoggingIn = 2,
        /// <summary>
        /// The client is presenting its credentials to the server.
        /// </summary>
        Authenticating = 3,
        /// <summary>
        /// The client is communicating freely with the server.
        /// </summary>
        Online = 4,
        /// <summary>
        /// The client has notified the server that it is quitting and is waiting for the connection to close.
        /// </summary>
        Quitting = 5,
    }
}
