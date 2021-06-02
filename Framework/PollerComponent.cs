using Crestron.SimplSharp;
using System;

namespace ATC.Framework
{
    /// <summary>
    /// This class adds polling elements to SystemComponent class.
    /// </summary>
    public abstract class PollerComponent : SystemComponent, IPollerComponent
    {
        #region Class variables
        long pollingInterval;
        CTimer pollingTimer;
        #endregion

        #region Constants
        const long PollingIntervalDefault = 10000;
        #endregion

        #region Properties
        /// <summary>
        /// Whether or not polling is enabled for this component.
        /// </summary>
        public bool PollingEnabled
        {
            get { return pollingTimer != null; }
            set { PollingEnable(value); }
        }

        /// <summary>
        /// How long in milliseconds between each subsequenct call of PollingCallback().
        /// </summary>
        public long PollingInterval
        {
            get { return pollingInterval; }
            set
            {
                if (value > 0)
                {
                    pollingInterval = value;
                    Trace("PollingInterval set to: " + value);
                    if (PollingEnabled)
                        PollingEnable(true);
                }
            }
        }
        #endregion

        #region Constructor
        public PollerComponent()
        {
            PollingInterval = PollingIntervalDefault;
        }
        #endregion

        #region Internal methods
        void PollingEnable(bool enable)
        {
            try
            {
                if (enable)
                {
                    if (PollingEnabled)
                    {
                        Trace("PollingEnable() restarting polling timer with new interval: " + pollingInterval);
                        PollingTimerStop();
                    }
                    else
                        Trace("PollingEnable() enabling polling.");


                    pollingTimer = new CTimer(PollingCallback, null, pollingInterval, pollingInterval);
                }
                else
                {
                    Trace("PollingEnable() disabling polling.");
                    PollingTimerStop();
                }
            }
            catch (Exception ex)
            {
                TraceException("PollingEnable() exception caught.", ex);
            }
        }

        abstract protected void PollingCallback(object o);

        void PollingTimerStop()
        {
            if (pollingTimer != null)
            {
                Trace("PollingTimerStop() stopping existing timer.");
                pollingTimer.Stop();
                pollingTimer.Dispose();
                pollingTimer = null;
            }
        }
        #endregion
    }

    public interface IPollerComponent
    {
        bool PollingEnabled { get; set; }
        long PollingInterval { get; set; }
    }
}
