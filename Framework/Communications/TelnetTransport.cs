using Crestron.SimplSharp.CrestronSockets;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public sealed class TelnetTransport : TcpClientTransport
    {
        public const int PortDefault = 23;

        #region Constructor
        public TelnetTransport(string hostname)
            : base(hostname, PortDefault) { }

        public TelnetTransport(string hostname, int port)
            : base(hostname, port) { }
        #endregion

        #region TCP client event handlers
        protected override void ClientSocketStatusChange(TCPClient client, SocketStatus status)
        {
            Trace("ClientSocketStatusChange() status: " + status);

            switch (status)
            {
                case SocketStatus.SOCKET_STATUS_CONNECTED:
                    client.ReceiveDataAsync(ClientReceiveDataHandler);
                    break;
                case SocketStatus.SOCKET_STATUS_NO_CONNECT:
                    RaiseConnectionStateEvent(ConnectionState.NotConnected);
                    Reset();
                    break;
                default:
                    TraceWarning("ClientSocketStatusChange() unhandled status: " + status);
                    break;
            }
        }

        protected override void ClientReceiveDataHandler(TCPClient client, int bytesReceived)
        {
            try
            {
                // decode incoming bytes
                string data = Encoding.GetString(client.IncomingDataBuffer, 0, bytesReceived);

                // check for incoming telnet negotation string
                if (data.StartsWith("\xFF") && ConnectionState == ConnectionState.Connecting)
                {
                    Trace(String.Format("ClientReceiveDataHandler() received negotiation line: \"{0}\"", Utilities.HexString(data)));

                    // perform replacements
                    var response = data.Replace('\xFB', '\xFE'); // replace will with don't
                    response = data.Replace('\xFD', '\xFC'); // replace do with won't

                    // send response to server
                    Trace(String.Format("ClientReceiveDataHandler() sending negotiation response: \"{0}\"", Utilities.HexString(response)));
                    Send(response);
                }
                else
                {
                    Trace(string.Format("ClientReceiveDataHandler() received {0} bytes. Content: \"{1}\"", bytesReceived, Utilities.ControlCodeString(data)));

                    // finished negotiation so raise connected event
                    if (ConnectionState == ConnectionState.Connecting)
                        RaiseConnectionStateEvent(ConnectionState.Connected);

                    // raise response received event
                    RaiseResponseReceivedEvent(data);
                }

                // listen for more responses
                client.ReceiveDataAsync(ClientReceiveDataHandler);
            }
            catch (Exception ex)
            {
                TraceException("ClientReceiveDataHandler() exception occurred.", ex);
            }
        }
        #endregion
    }
}
