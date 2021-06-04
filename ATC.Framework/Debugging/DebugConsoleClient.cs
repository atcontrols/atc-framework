using ATC.Framework.Communications;
using System;

namespace ATC.Framework.Debugging
{
    public static class DebugConsoleClient
    {
        #region Properties
        /// <summary>
        /// The name of the current program to report to the DebugConsole server.
        /// </summary>
        public static string ProgramName { get; set; }

        /// <summary>
        /// The number of the current program to report to the DebugConsole server.
        /// </summary>
        public static int ProgramNumber { get; set; }

        /// <summary>
        /// Hostname of the DebugConsole server.
        /// </summary>
        public static string Hostname { get; set; }

        /// <summary>
        /// Port number of the DebugConsole server.
        /// </summary>
        public static int Port { get; set; } = PortDefault;

        /// <summary>
        /// Returns true if currently connected to the DebugConsole server.
        /// </summary>
        public static bool Connected
        {
            get
            {
                if (transport == null)
                    return false;
                else
                    return transport.ConnectionState == ConnectionState.Connected;
            }
        }
        #endregion

        #region Constants
        public const int PortDefault = 64000;
        #endregion

        #region Class variables
        private static UdpClientTransport transport;
        #endregion

        /// <summary>
        /// Instruct the client to connect to the DebugConsole server.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public static void Connect()
        {
            // validate hostname
            if (Hostname == null || Hostname == String.Empty)
            {
                Tracer.PrintLine("DebugConsoleClient.Connect() error, hostname is invalid.");
                return;
            }

            // validate port
            if (Port == 0)
            {
                Tracer.PrintLine("DebugConsoleClient.Connect() error, port is invalid.");
                return;
            }

            if (transport == null)
            {
                transport = new UdpClientTransport(Hostname, Port);

                // add event callback handlers
                transport.ConnectionStateCallback += new EventHandler<ConnectionStateEventArgs>(TransportConnectionStateCallback);
                transport.ResponseReceivedCallback += new EventHandler<ResponseReceivedEventArgs>(TransportResponseReceivedCallback);

            }
            else
            {
                Tracer.PrintLine("DebugConsoleClient.Connect() called but transport is not null.");
                return;
            }
        }

        /// <summary>
        /// Instruct the client to disconnect from the DebugConsole server.
        /// </summary>
        public static void Disconnect()
        {
            if (transport != null)
            {
                transport.Disconnect();
                transport.Dispose();

                transport = null;
            }
        }

        public static void Send(Category messageType, string componentName, string message)
        {
            // check that we're connected
            if (!Connected)
                return;

            // replace { and } so that DebugConsole can render message
            if (message.Contains("{") || message.Contains("}"))
                message = message.Replace("{", "(").Replace("}", ")");

            // create dcm object
            DebugConsoleMessage dcm;
            if (!string.IsNullOrEmpty(ProgramName) && ProgramNumber > 0)
                dcm = new DebugConsoleMessage(ProgramName, ProgramNumber, messageType, componentName, message);
            else
                dcm = new DebugConsoleMessage(messageType, componentName, message);

            // convert object to string and send
            string jsonString = DebugConsoleMessage.EncodeToJson(dcm);
            transport.Send(jsonString);
        }

        #region Transport event handlers
        static void TransportConnectionStateCallback(object sender, ConnectionStateEventArgs e)
        {
            Tracer.PrintLine("DebugConsoleClient connection state: " + e.State);
        }

        static void TransportResponseReceivedCallback(object sender, ResponseReceivedEventArgs e)
        {
            Tracer.PrintLine("DebugConsoleClient received response: " + e.Response);
        }
        #endregion
    }
}
