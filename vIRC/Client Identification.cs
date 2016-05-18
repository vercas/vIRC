using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Identification data for an <see cref="vIRC.IrcClient"/>.
    /// </summary>
    public class IrcClientIdentification
    {
        /// <summary>
        /// Gets or sets the nickname.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the real name.
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Gets or sets the method called to solve nickname conflicts on connection.
        /// </summary>
        public Func<string, string> NickConflictResolver { get; set; }

        /// <summary>
        /// Gets or sets the username used to authenticate with the nickname service.
        /// </summary>
        public string NickServUsername { get; set; }

        /// <summary>
        /// Gets or sets the password used to authenticate with the nickname service.
        /// </summary>
        public string NickServPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SASL authentication is used or not.
        /// </summary>
        public bool UseSasl { get; set; }

        /// <summary>
        /// Gets or sets the password used to authenticate with the server.
        /// </summary>
        public string ServerPassword { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcClientIdentification"/> class with the given nickname, username and real name.
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="username"></param>
        /// <param name="realName"></param>
        public IrcClientIdentification(string nickname, string username, string realName)
        {
            if (nickname == null)
                throw new ArgumentNullException("nickname");
            if (username == null)
                throw new ArgumentNullException("username");
            if (realName == null)
                throw new ArgumentNullException("realName");

            if (!Utils.Validation.IsNick(nickname))
                throw new FormatException("The given nickname contains invalid characters.");

            this.Nickname = nickname;
            this.Username = username;
            this.RealName = realName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.IrcClientIdentification"/> class with all values copied from the given instance.
        /// </summary>
        /// <param name="other">The instance whose value to copy.</param>
        public IrcClientIdentification(IrcClientIdentification other)
        {
            this.Nickname = other.Nickname;
            this.Username = other.Username;
            this.RealName = other.RealName;
            this.NickServUsername = other.NickServUsername;
            this.NickServPassword = other.NickServPassword;
            this.UseSasl = other.UseSasl;
            this.ServerPassword = other.ServerPassword;
        }
    }
}
