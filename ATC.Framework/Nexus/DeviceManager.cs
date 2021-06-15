using ATC.Framework.Debugging;
using ATC.Framework.Devices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ATC.Framework.Nexus
{
    internal class DeviceManager : SystemComponent
    {
        #region Fields
        private readonly Dictionary<string, IDevice> managedDevices = new Dictionary<string, IDevice>();
        private readonly Dictionary<string, IDevice> updatedDevices = new Dictionary<string, IDevice>();
        private readonly IRequestManager requestManager;
        private bool sentManagedDevices = false;
        #endregion

        #region Constructor
        public DeviceManager(IRequestManager requestManager)
        {
            this.requestManager = requestManager;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Add a device to be monitored and its state sent to Nexus.
        /// </summary>
        /// <param name="device"></param>
        public void AddDevice(IDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            else if (device.Id == null)
                throw new ArgumentException("Device ID cannot be null");

            if (managedDevices.ContainsKey(device.Id))
            {
                TraceWarning("AddDevice() managed devices already contains device with ID: " + device.Id);
                managedDevices.Remove(device.Id);
            }
            else
                Trace(string.Format("AddDevice() adding device with ID: {0} to collection.", device.Id));

            managedDevices.Add(device.Id, device);
            AddDeviceEventHandlers(device);
        }

        public void SendDevices()
        {
            if (sentManagedDevices && updatedDevices.Count == 0)
                return;

            List<IDevice> list = sentManagedDevices ?
                updatedDevices.Values.ToList<IDevice>() :
                managedDevices.Values.ToList<IDevice>();

            // generate json array string
            string devices = string.Empty;
            for (int i = 0; i < list.Count; i++)
            {
                devices += list[i].Serialize();
                if (i < list.Count - 1)
                    devices += ",";
            }
            string body = "[" + devices + "]"; // construct json array

            if (TraceLevel == TraceLevel.Extended)
                Trace("SendDevices() JSON to send:\r\n" + body, TraceLevel.Extended);

            string url = string.Format("{0}/device/{1}/{2}/{3}", requestManager.ApiUrl, requestManager.CompanyId, requestManager.GroupId, requestManager.SystemId);

            requestManager.SendRequest(HttpMethod.Put, url, body);
            Trace(string.Format("SendDevices() sent {0} devices.", list.Count));

            if (!sentManagedDevices)
                sentManagedDevices = true;
            else
                updatedDevices.Clear();
        }
        #endregion

        private void AddDeviceEventHandlers(IDevice device)
        {
            device.OnlineEventHandler += new EventHandler<OnlineEventArgs>(DeviceOnlineEventHandler);

            if (device is IPowerDevice powerDevice)
            {
                powerDevice.PowerEventHandler += new EventHandler<PowerEventArgs>(DevicePowerEventHandler);
            }

            if (device is IDisplayDevice displayDevice)
            {
                displayDevice.InputEventHandler += new EventHandler<InputEventArgs>(DeviceInputEventHandler);
            }

            if (device is IProjectorDevice projectorDevice)
            {
                projectorDevice.LampHoursEventHandler += new EventHandler<LampHoursEventArgs>(DeviceLampHoursEventHandler);
            }

            if (device is IScreenDevice screenDevice)
            {
                screenDevice.ScreenPositionHandler += new EventHandler<ScreenPositionEventArgs>(DeviceScreenPositionHandler);
            }

            if (device is ISwitcherDevice switcherDevice)
            {
            }

            if (device is IConferenceCodecDevice conferenceCodecDevice)
            {
                conferenceCodecDevice.MicMuteEventHandler += new EventHandler<MicMuteEventArgs>(DeviceEventHandler);
                conferenceCodecDevice.CameraMuteEventHandler += new EventHandler<CameraMuteEventArgs>(DeviceEventHandler);
                conferenceCodecDevice.CallActiveEventHandler += new EventHandler<CallActiveEventArgs>(DeviceEventHandler);
                conferenceCodecDevice.ContentActiveEventHandler += new EventHandler<ContentActiveEventArgs>(DeviceEventHandler);
            }
        }

        private void SetUpdatedDevice(IDevice device)
        {
            if (updatedDevices.ContainsKey(device.Id))
            {
                updatedDevices.Remove(device.Id);
                Trace("SetUpdatedDevice() removed existing device ID: " + device.Id);
            }

            updatedDevices.Add(device.Id, device);
        }

        #region Event handlers
        private void DeviceOnlineEventHandler(object sender, OnlineEventArgs e)
        {
            IDevice device = (IDevice)sender;
            Trace(string.Format("DeviceOnlineEventHandler() device ID: {0} online state changed: {1}", device.Id, e.Value));
            SetUpdatedDevice(device);
        }

        private void DevicePowerEventHandler(object sender, PowerEventArgs e)
        {
            IDevice device = (IDevice)sender;
            Trace(string.Format("DevicePowerEventHandler() device ID: {0} power state changed: {1}", device.Id, e.Value));
            SetUpdatedDevice(device);
        }

        private void DeviceInputEventHandler(object sender, InputEventArgs e)
        {
            IDevice device = (IDevice)sender;
            Trace(string.Format("DeviceInputEventHandler() device ID: {0} input state changed: {1}", device.Id, e.Value));
            SetUpdatedDevice(device);
        }

        private void DeviceLampHoursEventHandler(object sender, LampHoursEventArgs e)
        {
            IDevice device = (IDevice)sender;
            Trace(string.Format("DeviceLampHoursEventHandler() device ID: {0} lamp hours state changed: {1}", device.Id, e.Value));
            SetUpdatedDevice(device);
        }

        private void DeviceScreenPositionHandler(object sender, ScreenPositionEventArgs e)
        {
            IDevice device = (IDevice)sender;
            Trace(string.Format("DeviceScreenPositionHandler() device ID: {0} screen position state changed: {1}", device.Id, e.Value));
            SetUpdatedDevice(device);
        }

        private void DeviceEventHandler(object sender, EventArgs e)
        {
            if (sender is IDevice device)
            {
                SetUpdatedDevice(device);
            }
            else
                TraceError("DeviceEventHandler() sender is not a device.");
        }

        #endregion
    }
}