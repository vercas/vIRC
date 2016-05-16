namespace vIRC
{
    /// <summary>
    /// Known IRC protocols
    /// </summary>
    public enum IrcProtocols
    {
        /// <summary>
        /// Unknown or invalid.
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// Naked IRC protocol.
        /// </summary>
        Irc = 0,
        /// <summary>
        /// IRC over SSL.
        /// </summary>
        IrcSsl = 1,
    }
}
