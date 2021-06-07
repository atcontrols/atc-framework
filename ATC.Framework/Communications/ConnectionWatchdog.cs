using System;
using System.Collections.Generic;
using System.Threading;

namespace ATC.Framework.Communications
{
    public class ConnectionWatchdog : SystemComponent, IDisposable
    {
        #region Fields

        private readonly List<IConnectable> components = new List<IConnectable>();
        private Timer timer;
        private int index;

        #endregion

        #region Properties

        /// <summary>
        /// How long to wait (in milliseconds) after Start is called to start checking devices.
        /// </summary>
        public int StartDelay { get; set; } = 500;

        /// <summary>
        /// How long to wait between checks.
        /// </summary>
        public int Interval { get; set; } = 1000;

        /// <summary>
        /// Number of components being monitored by this watchdog.
        /// </summary>
        public int DeviceCount => components.Count;
        
        /// <summary>
        /// Returns true if the watchdog is currently active.
        /// </summary>
        public bool IsActive => timer != null;

        #endregion

        /// <summary>
        /// Add a device that supports the IConnectable interface to be monitored.
        /// </summary>
        /// <param name="component"></param>
        public void Add(IConnectable component)
        {
            Trace($"AddDevice() adding {component.ComponentName} to list of components.");
            components.Add(component);
            index = 0;
        }

        /// <summary>
        /// Add an array of devices that support the IConnectable interface to be monitored.
        /// </summary>
        /// <param name="components"></param>
        public void Add(params IConnectable[] components)
        {
            foreach (IConnectable connectable in components)
                Add(connectable);
        }

        /// <summary>
        /// Start monitoring of added devices.
        /// </summary>
        public void Start()
        {
            if (DeviceCount > 0)
            {
                Trace($"Start() starting monitoring of {DeviceCount} devices.");
                timer = new Timer(TimerCallback, null, StartDelay, Interval);
            }
            else
                TraceError("Start() there are no devices to monitor.");
        }

        /// <summary>
        /// Stop monitoring of added devices.
        /// </summary>
        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private void TimerCallback(object o)
        {
            IConnectable component = components[index++];

            if (component.ConnectionState == ConnectionState.NotConnected)
            {
                Trace($"TimerCallback() instructing {component.ComponentName} to connect.");
                component.Connect();
            }

            // reset index
            if (index >= DeviceCount)
                index = 0;
        }
    }
}
