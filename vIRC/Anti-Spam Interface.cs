using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Represents the minimal interface necessary to implement and interact with anti-spam measures.
    /// </summary>
    public interface IAntiSpam
    {
        /// <summary>
        /// Registers a hit at the present time, and returns a task which finishes when the hit is ready to be processed.
        /// </summary>
        /// <remarks>
        /// If the hit can be processed right away, the returned task will already be completed.
        /// </remarks>
        /// <returns>A task which finishes when the hit is ready to be processed.</returns>
        Task Hit();
    }
}
