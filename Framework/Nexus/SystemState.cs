using Crestron.SimplSharp;
using System;

namespace ATC.Framework.Nexus
{
    public class SystemState : SystemComponent
    {
        #region Fields
        private bool _powered;
        private string _status;
        private CTimer timer;
        #endregion

        #region Constants
        /// <summary>
        /// How long to wait before raising an update event (to prevent flooding system).
        /// </summary>
        private const long UpdateDelay = 5000;
        #endregion

        #region Properties
        /// <summary>
        /// Whether or not the system is powered up.
        /// </summary>
        public bool Powered
        {
            get { return _powered; }
            set
            {
                if (_powered != value)
                {
                    _powered = value;
                    Trace("Powered set to: " + value);
                    RaiseUpdateEvent();
                }
            }
        }

        /// <summary>
        /// Descriptive status status of the system.
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    Trace("Status set to: " + value);
                    RaiseUpdateEvent();
                }
            }
        }
        #endregion

        #region Events
        private void RaiseUpdateEvent()
        {
            TimerCleanup();
            timer = new CTimer(TimerCallback, UpdateDelay);
        }

        private void TimerCallback(object o)
        {
            if (UpdateHandler != null)
                UpdateHandler(this);
            else
                TraceWarning("TimerCallback() update handler is not assigned.");
        }

        private void TimerCleanup()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        public event Action<SystemState> UpdateHandler;
        #endregion
    }
}
