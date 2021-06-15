using System;
using System.Threading.Tasks;
using System.Timers;
using ATC.Framework.Debugging;

namespace ATC.Framework
{
    public interface IPollerComponent
    {
        bool PollingEnabled { get; set; }
        long PollingInterval { get; set; }
    }

    /// <summary>
    /// This class adds polling elements to SystemComponent class.
    /// </summary>
    public abstract class PollerComponent : SystemComponent, IPollerComponent
    {
        #region Fields

        private long _interval = 10000;
        private Timer timer;

        #endregion

        #region Properties

        /// <summary>
        /// Whether or not polling is enabled for this component.
        /// </summary>
        public bool PollingEnabled
        {
            get => timer != null && timer.Enabled;
            set
            {
                ResetTimer();

                if (value)
                {
                    Trace($"PollingEnable.set enabling polling with interval of: {PollingInterval}ms.");

                    timer = new Timer(PollingInterval);
                    timer.Elapsed += async (sender, e) =>
                    {
                        TraceInfo($"PollingEnable.set elapsed event at: {e.SignalTime} ", TraceLevel.Extended);
                        try 
                        { 
                            await PollingCallback(); 
                        }
                        catch (Exception ex)
                        {
                            TraceException(ex, "PollingEnabled.set", "Exception occurred in polling callback method.");
                        }
                    };
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// How long in milliseconds between each subsequenct call of PollingCallback().
        /// </summary>
        public long PollingInterval
        {
            get => _interval;
            set
            {
                if (value > 0)
                {
                    _interval = value;
                    Trace("PollingInterval set to: " + value);
                    PollingEnabled = PollingEnabled; // recall set method of PollingEnabled property
                }
                else
                    TraceError($"PollingInterval must be a positive value: {value}");
            }
        }
        #endregion

        protected virtual Task PollingCallback()
        {
            TraceWarning("PollingCallback() method getting called in PollerComponent class. You probably want to override this in your derived class.");
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                ResetTimer();
            }
        }

        private void ResetTimer()
        {
            if (timer != null)
            {
                Trace("ResetTimer() resetting existing polling timer.");
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }
    }
}
