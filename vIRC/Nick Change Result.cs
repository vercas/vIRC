using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents possible outcomes of attempting to change the client's nickname.
    /// </summary>
    public enum NickChangeResult
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Success = 0,
        InUse = 1,
        Collision = 2,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
