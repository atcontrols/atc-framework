using ATC.Framework.Debugging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ATC.Framework.Devices
{
    internal static class DeviceSerializer
    {
        public static bool PrintOutput { get; set; }
        public static bool IndentedFormatting { get; set; }

        public static string Serialize(IDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            try
            {
                // create temporary object
                var obj = new JObject();
                obj["id"] = device.Id;
                obj["category"] = device.Category.ToString();
                obj["details"] = JObject.FromObject(device.Details);
                obj["state"] = GetState(device);

                // serialize object
                Formatting formatting = IndentedFormatting ? Formatting.Indented : Formatting.None;
                string json = JsonConvert.SerializeObject(obj, formatting);

                // print output to console (if enabled)
                if (PrintOutput)
                    Tracer.PrintLine(string.Format("Serialize() output for device ID: {0}\r\n{1}", device.Id, json));

                return json;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine(string.Format("Serialize() exception caught: {0}, {1}\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
                return "{}";
            }
        }

        private static JObject GetState(IDevice device)
        {

            // create state property
            var state = new JObject();

            // add online property (all devices have online property)
            AddProperty(state, "online", "The online status of the device.", device.Online);

            // add additional states depending on device type
            if (device is IPowerDevice)
                AddPowerState((IPowerDevice)device, state);
            if (device is IDisplayDevice)
                AddDisplayState((IDisplayDevice)device, state);
            if (device is IProjectorDevice)
                AddProjectorState((IProjectorDevice)device, state);
            if (device is IScreenDevice)
                AddScreenState((IScreenDevice)device, state);
            if (device is ISwitcherDevice)
                AddSwitcherState((ISwitcherDevice)device, state);
            if (device is IConferenceCodecDevice)
                AddConferenceCodecState((IConferenceCodecDevice)device, state);

            return state;
        }

        private static void AddPowerState(IPowerDevice device, JObject state)
        {
            AddProperty(state, "power", "The current power status of the device.", device.Power);
        }

        private static void AddDisplayState(IDisplayDevice device, JObject state)
        {
            AddProperty(state, "input", "The current input for this display device.", device.Input);
        }

        private static void AddProjectorState(IProjectorDevice device, JObject state)
        {
            AddProperty(state, "lampHours", "The number of hours the lamp has been on.", device.LampHours);
        }

        private static void AddScreenState(IScreenDevice device, JObject state)
        {
            AddProperty(state, "position", "The physical position of the screen.", device.Position);
        }

        private static void AddSwitcherState(ISwitcherDevice device, JObject state)
        {
            AddProperty(state, "inputCount", "The number of inputs on the switcher.", device.InputCount);
            AddProperty(state, "outputCount", "The number of outputs on the switcher.", device.OutputCount);
        }

        private static void AddConferenceCodecState(IConferenceCodecDevice device, JObject state)
        {
            AddProperty(state, "micMute", "Local microphone mute / privacy.", device.MicMute);
            AddProperty(state, "cameraMute", "Local camera mute / privacy.", device.CameraMute);
            AddProperty(state, "callActive", "Currently in a call", device.CallActive);
            AddProperty(state, "contentActive", "Local presentation active", device.ContentActive);
        }

        private static void AddProperty(JObject obj, string name, string description, object value)
        {
            var property = new JObject();
            property["description"] = description;

            if (value == null)
                property["value"] = null;
            else if (value is bool)
                property["value"] = (bool)value;
            else if (value is int)
                property["value"] = (int)value;
            else if (value is float)
                property["value"] = (float)value;
            else if (value is double)
                property["value"] = (double)value;
            else if (value is string)
                property["value"] = (string)value;
            else
                property["value"] = value.ToString();

            obj[name] = property;
        }
    }
}
