using ATC.Framework.Debugging;
using Crestron.SimplSharp;
using System;
using System.Collections.Generic;

namespace ATC.Framework.Devices
{
    public interface IDevice : ISystemComponent
    {
        /// <summary>
        /// Unique identifier for this device
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The category of this device (e.g. Projector, Screen).
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Descriptive information about this device
        /// </summary>
        IDeviceDetails Details { get; }

        /// <summary>
        /// The device is ready for external input.
        /// </summary>
        bool Online { get; }

        /// <summary>
        /// The device is busy processing.
        /// </summary>
        bool Busy { get; }

        /// <summary>
        /// Serialize this Device to JSON and return string.
        /// </summary>
        string Serialize();

        event EventHandler<OnlineEventArgs> OnlineEventHandler;
        event EventHandler<BusyEventArgs> BusyEventHandler;

        /// <summary>
        /// Notifies listeners of any errors that occur on the device
        /// </summary>
        event EventHandler<ErrorEventArgs> ErrorEventHandler;
    }

    public abstract class Device : SystemComponent, IDevice, IDisposable
    {
        #region Fields
        private bool _online, _busy;
        private CTimer busyTimer;
        private static readonly Dictionary<string, int> deviceCount = new Dictionary<string, int>();
        #endregion

        #region Properties
        public string Id { get; set; }
        public string Category
        {
            get
            {
                if (this is ConferenceCodecDevice) return "conference-codec";
                if (this is ProjectorDevice) return "projector";
                if (this is DisplayDevice) return "display";
                if (this is ScreenDevice) return "screen";
                if (this is PowerDevice) return "power";
                if (this is SwitcherDevice) return "switcher";

                return "device";
            }
        }
        public IDeviceDetails Details { get; protected set; }

        public bool Online
        {
            get { return _online; }
            protected set
            {
                if (_online != value)
                {
                    _online = value;
                    Trace("Online set to: " + value, TraceLevel.Extended);

                    // raise event
                    if (OnlineEventHandler != null)
                    {
                        var args = new OnlineEventArgs()
                        {
                            Value = value,
                        };
                        OnlineEventHandler(this, args);
                    }
                }
            }
        }

        public bool Busy
        {
            get { return _busy; }
            protected set
            {
                if (_busy != value)
                {
                    _busy = value;
                    Trace("Busy set to: " + value, TraceLevel.Extended);

                    BusyTimerCleanup();

                    // handle timeout
                    if (value && BusyTimeout > 0)
                        busyTimer = new CTimer(BusyTimerCallback, BusyTimeout);

                    if (BusyEventHandler != null)
                    {
                        var args = new BusyEventArgs()
                        {
                            Value = value,
                        };

                        BusyEventHandler(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// How long to wait to automatically set Busy back to false. Set to 0 to never timeout.
        /// </summary>
        protected long BusyTimeout { get; set; }

        #endregion

        #region Constructor
        public Device()
        {
            if (deviceCount.ContainsKey(Category))
                deviceCount[Category]++;
            else
                deviceCount[Category] = 1;

            Id = string.Format("{0}-{1}", Category, deviceCount[Category]);
            Details = new DeviceDetails()
            {
                Name = Id,
            };
        }
        #endregion

        #region Virtual methods
        protected virtual void Reset()
        {
            Online = false;
            Busy = false;
        }

        public string Serialize()
        {
            return DeviceSerializer.Serialize(this);
        }
        #endregion

        #region Events
        public event EventHandler<OnlineEventArgs> OnlineEventHandler;
        public event EventHandler<BusyEventArgs> BusyEventHandler;
        public event EventHandler<ErrorEventArgs> ErrorEventHandler;

        /// <summary>
        /// Inform external listeners that an error has occured.
        /// </summary>
        /// <param name="message">The message for the error event.</param>
        protected void RaiseErrorEvent(string message)
        {
            if (ErrorEventHandler != null)
            {
                var args = new ErrorEventArgs()
                {
                    Message = message,
                };

                ErrorEventHandler(this, args);
            }
            else
                TraceWarning(string.Format("RaiseErrorEvent() no event handler is defined. Message: \"{0}\"", message));
        }
        #endregion

        #region Busy methods
        private void BusyTimerCleanup()
        {
            if (busyTimer != null)
            {
                busyTimer.Stop();
                busyTimer.Dispose();
                busyTimer = null;
            }
        }

        private void BusyTimerCallback(object o)
        {
            Trace("BusyTimerCallback() timeout reached.");
            Busy = false;
        }
        #endregion

        #region Object cleanup
        /// <summary>
        /// Clean up resources used by this device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                BusyTimerCleanup();
            }
        }
        #endregion
    }

    #region Event classes
    public class OnlineEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }

    public class BusyEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
    #endregion
}
