using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Utils
{
    /// <summary>
    /// Represents a message prefix in IRC.
    /// </summary>
    public class Prefix
    {
        /// <summary>
        /// Gets the name of the server contained in this prefix, or null if it does not represent a server.
        /// </summary>
        public string ServerName { get; private set; } = null;

        /// <summary>
        /// Gets the nickname contained in this prefix, or null if the prefix represents a server.
        /// </summary>
        public string Nickname { get; private set; } = null;

        /// <summary>
        /// Gets the hostname contained in this prefix, or null if the prefix represents a server or none was specified.
        /// </summary>
        public string Hostname { get; private set; } = null;

        /// <summary>
        /// Gets the username contained in this prefix, or null if the prefix represents a server or none was specified.
        /// </summary>
        public string Username { get; private set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Utils.Prefix"/> class from the given string representation.
        /// </summary>
        /// <param name="prefix"></param>
        public Prefix(string prefix)
        {
            if (prefix == null)
                throw new ArgumentNullException("prefix");

            int atInd = prefix.IndexOf('@');

            if (atInd < 0)
            {
                //  So, no @ symbol means this is either a lonely nick or a server name... The best way to determine that is by the presence of a dot.

                if (prefix.Contains("."))
                    this.ServerName = prefix;
                else
                    this.Nickname = prefix;
            }
            else
            {
                int exclInd = prefix.IndexOf('!', 0, atInd);
                //  The ! must be before the @.

                if (exclInd < 0)
                {
                    //  No exclamation mark means no username specified.

                    this.Nickname = prefix.Substring(0, atInd);
                }
                else
                {
                    //  An exclamation mark present means nick, username and hostname are all present!

                    this.Nickname = prefix.Substring(0, exclInd);
                    this.Username = prefix.Substring(exclInd + 1, atInd - exclInd - 1);
                }

                this.Hostname = prefix.Substring(atInd + 1);
            }
        }

        /// <summary>
        /// Returns a string representation of the current prefix.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.ServerName == null)
                if (this.Hostname == null)
                    return "[nickname " + this.Nickname + "]";
                else if (this.Username == null)
                    return "[nickname " + this.Nickname + "; hostname " + this.Hostname + "]";
                else
                    return "[nickname " + this.Nickname + "; username " + this.Username + "; hostname " + this.Hostname + "]";
            else
                return "[srvname " + this.ServerName + "]";
        }
    }
}
