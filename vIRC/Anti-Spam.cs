using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC
{
    /// <summary>
    /// Encompasses common anti-spam measurements.
    /// </summary>
    public class AntiSpam : IAntiSpam
    {
        TimeSpan cooldown = new System.TimeSpan(0, 0, 2);
        TimeSpan delay = new TimeSpan(0, 0, 1);
        TimeSpan burstDelay = new TimeSpan(0, 0, 0, 0, 300);
        int burstCount = 4;
        
        DateTime lastHit = DateTime.MinValue;
        int currentBurst = 0;

        /// <summary>
        /// Gets or sets the cooldown after a burst.
        /// </summary>
        public TimeSpan Cooldown
        {
            get
            {
                return this.cooldown;
            }
            set
            {
                this.cooldown = value;
            }
        }

        /// <summary>
        /// Gets or sets the normal delay.
        /// </summary>
        public TimeSpan Delay
        {
            get
            {
                return this.delay;
            }
            set
            {
                this.delay = value;
            }
        }

        /// <summary>
        /// Gets or sets the burst delay.
        /// </summary>
        public TimeSpan BurstDelay
        {
            get
            {
                return this.burstDelay;
            }
            set
            {
                this.burstDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the burst count.
        /// </summary>
        public int BurstCount
        {
            get
            {
                return this.burstCount;
            }
            set
            {
                this.burstCount = value;
            }
        }

        /// <summary>
        /// Gets the current burst.
        /// </summary>
        public int CurrentBurst { get { return this.currentBurst; } }

        /// <summary>
        /// Gets the time of the last hit.
        /// </summary>
        public DateTime LastHit { get { return this.lastHit; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.AntiSpam"/> class with default parameters.
        /// </summary>
        public AntiSpam()
        {

        }

        /// <summary>
        /// Registers a hit at the present time, and returns a task which finishes when the hit is ready to be processed.
        /// </summary>
        /// <remarks>
        /// If the hit can be processed right away, the returned task will already be completed.
        /// </remarks>
        /// <returns>A task which finishes when the hit is ready to be processed.</returns>
        public Task Hit()
        {
            return this.HitInternal(DateTime.Now);
        }

        /// <summary>
        /// Registers a hit at the given time, and returns a task which finishes when the hit is ready to be processed.
        /// </summary>
        /// <remarks>
        /// If the hit can be processed right away, the returned task will already be completed.
        /// </remarks>
        /// <returns>A task which finishes when the hit is ready to be processed.</returns>
        public Task Hit(DateTime time)
        {
            if (time < this.lastHit)
                throw new ArgumentOutOfRangeException("Cannot register a hit which happened before the previous one!");

            return this.HitInternal(time);
        }

        private Task HitInternal(DateTime time)
        {
            var diff = time - this.lastHit;

            System.Diagnostics.Debug.WriteLine("Anti-spam hit @ {0}; diff {1}.", time, diff);

            if (diff > this.cooldown)
            {
                //  Well, enough time has passed for things to go back to normal.

                System.Diagnostics.Debug.WriteLine("\tCooldown passed after {0} hits in a burst.");

                this.currentBurst = 0;

                this.lastHit = time;
                return Task.FromResult(true);
            }

            if (diff > this.delay)
            {
                //  More than the imposed delay has passed, therefore there's no reason to wait, but the burst
                //  is not excused.

                System.Diagnostics.Debug.WriteLine("\tDiff over delay.");

                this.lastHit = time;
                return Task.FromResult(true);
            }

            //  So, reaching this point means there has been less than the required delay between this message
            //  and the previous one. Therefore, the burst must be calculated.

            if (this.currentBurst++ < this.burstCount)
            {
                //  Burst count not reached yet means quick messages can still be sent.
                
                if (diff > this.burstDelay)
                {
                    //  Nothing to wait.

                    System.Diagnostics.Debug.WriteLine("\tWithin burst count; over burst delay.");

                    this.lastHit = time;
                    return Task.FromResult(true);
                }
                else
                {
                    //  Ensure proper delay.

                    System.Diagnostics.Debug.WriteLine("\tWithin burst count; under burst delay.");

                    this.lastHit += this.burstDelay;
                    return Task.Delay(this.burstDelay - diff);
                }
            }
            else
            {
                //  Current burst has reached the burst count, means the normal delay must be enforced.

                ++this.currentBurst;

                System.Diagnostics.Debug.WriteLine("\tBurst count exceeded; diff under normal delay.");

                this.lastHit += this.delay;
                return Task.Delay(this.delay - diff);
            }
        }
    }
}
