using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace ATC.Framework.Communications
{
    public class ConnectionWatchdog : SystemComponent
    {
        #region Fields

        private readonly List<IConnectable> components = new List<IConnectable>();
        private Timer timer;

        #endregion

        #region Properties

        /// <summary>
        /// How long to wait between checks.
        /// </summary>
        public double Interval { get; set; } = 10000;

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
                timer = new Timer(Interval);
                timer.Elapsed += TimerCallback;
                timer.Start();
            }
            else
                TraceError("Start() there are no devices to monitor.");
        }

        /// <summary>
        /// Stop monitoring of added devices. This will also disconnect any connected devices.
        /// </summary>
        public void Stop()
        {
            // stop timer
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= TimerCallback;
            }
                
            // disconnect any connected components
            Trace("Stop() disconnecting any connected components.");
            foreach (var component in components)
            {
                if (component.ConnectionState == ConnectionState.Connected)
                {
                    Trace($"Stop() instructing {component.ComponentName} to disconnect.");
                    component.Disconnect();
                }
            }

            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        private async void TimerCallback(object sender, ElapsedEventArgs e)
        {
            Trace($"TimerCallback() checking {components.Count} devices connection state.");
            List<Task> tasks = new List<Task>();

            foreach (var component in components)
            {
                if (component.ConnectionState == ConnectionState.NotConnected)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            Trace($"TimerCallback() instructing {component.ComponentName} to connect.");
                            bool success = component.Connect();

                            if (success)
                                TraceInfo($"TimerCallback() successfully connected to: {component.ComponentName}");
                            else
                                TraceWarning($"TimerCallback() could not complete connection to: {component.ComponentName}");
                        }
                        catch (Exception ex)
                        {
                            TraceException(ex, nameof(TimerCallback), $"Error occurred while trying to connect");
                        }
                    }));
                };
            }

            await Task.WhenAll(tasks);
        }
    }
}
