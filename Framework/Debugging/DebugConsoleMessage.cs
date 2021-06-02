using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ATC.Framework.Debugging
{
    public class DebugConsoleMessage
    {
        public string ProgramName { get; private set; }
        public int ProgramNumber { get; private set; }
        public Category MessageType { get; private set; }
        public string ComponentName { get; private set; }
        public string MessageText { get; private set; }

        public DebugConsoleMessage(Category messageType, string componentName, string messageText)
        {
            Initialize(String.Empty, 0, messageType, componentName, messageText);
        }

        public DebugConsoleMessage(string programName, int programNumber, Category messageType, string componentName, string messageText)
        {
            Initialize(programName, programNumber, messageType, componentName, messageText);
        }

        private void Initialize(string programName, int programNumber, Category messageType, string componentName, string messageText)
        {
            ProgramName = programName;
            ProgramNumber = programNumber;
            MessageType = messageType;
            ComponentName = componentName;
            MessageText = messageText;
        }

        /// <summary>
        /// Encode a DebugConsoleMessage object into a JSON formatted string.
        /// </summary>
        /// <param name="dcm">The object to convert.</param>
        /// <returns>The JSON formatted string.</returns>
        public static string EncodeToJson(DebugConsoleMessage dcm)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(dcm);
                return jsonString;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DebugConsoleMessage.EncodeToJson() exception caught: " + ex.Message);
                return String.Empty;
            }
        }

        /// <summary>
        /// Decode a JSON formatted string into a DebugConsoleMessage object.
        /// </summary>
        /// <param name="jsonString">The JSON formatted string to decode.</param>
        /// <returns>The decode DebugConsoleMessage object.</returns>
        public static DebugConsoleMessage DecodeFromJson(string jsonString)
        {
            try
            {
                JObject obj = (JObject)JsonConvert.DeserializeObject(jsonString);

                // parse json
                var messageTypeInt = (int)obj.SelectToken("MessageType");
                Category messageType = (Category)messageTypeInt;
                var componentName = (string)obj.SelectToken("ComponentName");
                var messageText = (string)obj.SelectToken("MessageText");

                DebugConsoleMessage dcm = new DebugConsoleMessage(messageType, componentName, messageText);

                return dcm;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DebugConsoleMessage.DecodeFromJson() exception caught: " + ex.Message);
                return null;
            }
        }
    }
}
