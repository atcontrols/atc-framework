using Newtonsoft.Json;

namespace ATC.Framework.Devices
{
    public interface IDeviceDetails
    {
        string Name { get; set; }
        string Description { get; set; }
        string Manufacturer { get; set; }
        string Model { get; set; }
        string FirmwareVersion { get; set; }
        string SerialNumber { get; set; }
    }

    public class DeviceDetails : IDeviceDetails
    {
        /// <summary>
        /// Friendly name for this device.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Descriptive text about this device.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Manufacturer of the device.
        /// </summary>
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        /// <summary>
        /// Model of the device.
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; }

        /// <summary>
        /// Firmware version of the device.
        /// </summary>
        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// Serial number of the device
        /// </summary>
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }
    }
}
