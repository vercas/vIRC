using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.UserMessageReceived"/> and <see cref="vIRC.IrcUser.NicknameChanged"/>.
    /// </summary>
    public class UserNicknameChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="vIRC.IrcUser"/> whose nickname has changed.
        /// </summary>
        public IrcUser User { get; internal set; }

        /// <summary>
        /// Gets the old nickname of the user.
        /// </summary>
        public string OldNickname { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.UserNicknameChangedEventArgs"/> class with the given user and old nickname.
        /// </summary>
        /// <param name="user">The user whose nickname has changed.</param>
        /// <param name="oldNick">The old nickname of the user.</param>
        public UserNicknameChangedEventArgs(IrcUser user, string oldNick)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (oldNick == null)
                throw new ArgumentNullException("oldNick");

            this.User = user;
            this.OldNickname = oldNick;
        }
    }
}
