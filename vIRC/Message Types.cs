using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents possible types of messages in IRC.
    /// </summary>
    public enum IrcMessageTypes
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Standard = 0,
        /// <summary>
        /// Sending this will not result in errors and receiving it must not warrant a reply from a bot or service.
        /// </summary>
        Notice = 1,
        Action = 2,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
