using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public class TcpServerTransport : Transport
    {
        #region Fields
        private TCPServer server;
        private readonly string listenAddress = "0.0.0.0";
        private readonly Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
        private bool autoListen;
        private CTimer autoListenTimer;
        #endregion

        #region Properties
        public int PortNumber { get; set; }
        public bool AutoListen
        {
            get { return autoListen; }
            set
            {
                autoListen = value;
                if (autoListen && ConnectionState == ConnectionState.NotConnected)
                    Connect();
            }
        }
        #endregion

        #region Constructor
        public TcpServerTransport(int portNumber)
        {
            PortNumber = portNumber;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Starts listening on the specified port number.
        /// </summary>
        /// <returns></returns>
        public override bool Connect()
        {
            try
            {
                // ensure we are currently not connected
                if (ConnectionState != ConnectionState.NotConnected)
                {
                    TraceError("Connect() invalid connection state: " + ConnectionState);
                    return false;
                }

                // validate port number
                if (PortNumber <= 0)
                {
                    TraceError("Connect() invalid port number: " + PortNumber);
                    return false;
                }

                // create new server object
                if (server == null)
                {
                    Trace("Connect() creating new server object.");
                    server = new TCPServer(PortNumber);
                    server.SocketStatusChange += new TCPServerSocketStatusChangeEventHandler(SocketStatusChangeHandler);
                }

                Trace("Connect() data validation passed successfully.");

                // wait for connection asynchronously
                var result = server.WaitForConnectionAsync(listenAddress, ClientConnectCallback);
                if (result == SocketErrorCodes.SOCKET_OPERATION_PENDING)
                {
                    RaiseConnectionStateEvent(ConnectionState.Connecting);
                    Trace(String.Format("StartListening() successfully started listening on port: {0}", PortNumber));
                    return true;
                }
                else
                {
                    TraceError("StartListening() error encountered while attempting to start listening: " + result);
                    return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                return false;
            }
        }

        public override bool Disconnect()
        {
            return Reset();
        }

        public override bool Send(string s)
        {
            try
            {
                if (server.ServerSocketStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                {
                    var bytes = encoding.GetBytes(s);
                    var result = server.SendData(bytes, bytes.Length);

                    if (result == SocketErrorCodes.SOCKET_OK)
                    {
                        Trace(String.Format("Send() sent: {0} bytes successfully.", bytes.Length));
                        return true;
                    }
                    else
                    {
                        TraceError("Send() error occured while attempting to send: " + result);
                        return false;
                    }
                }
                else
                {
                    TraceError("Send() called but server is not connected.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                return false;
            }
        }
        #endregion

        #region Event handlers
        private void SocketStatusChangeHandler(TCPServer server, uint clientIndex, SocketStatus status)
        {
            // only handle first client
            if (clientIndex == 1)
            {
                switch (status)
                {
                    case SocketStatus.SOCKET_STATUS_CONNECTED:
                        Trace("SocketStatusChangeHandler() connected to: " + server.AddressServerAcceptedConnectionFrom);
                        RaiseConnectionStateEvent(ConnectionState.Connected);

                        // receive data asynchonously
                        var result = server.ReceiveDataAsync(ReceiveDataCallback);
                        if (result != SocketErrorCodes.SOCKET_OPERATION_PENDING)
                            TraceWarning("SocketStatusChangeHandler() error setting receive data callback: " + result);

                        break;
                    default:
                        Trace("SocketStatusChangeHandler() disconnected. Socket status: " + status);
                        RaiseConnectionStateEvent(ConnectionState.NotConnected);

                        // check if we need to start listening again
                        if (AutoListen)
                            autoListenTimer = new CTimer(AutoListenCallback, 1000);
                        else
                            Reset();

                        break;
                }
            }
        }

        private void ClientConnectCallback(TCPServer server, uint clientIndex)
        {
            // this method does nothing intentionally. it is required by WaitForConnectionAsync method
        }

        private void ReceiveDataCallback(TCPServer server, uint clientIndex, int numberOfBytesReceived)
        {
            // decode data
            var s = encoding.GetString(server.IncomingDataBuffer, 0, numberOfBytesReceived);
            Trace(String.Format("ReceiveDataCallback() received {0} bytes, decoded string: \"{1}\"", numberOfBytesReceived, Utilities.ControlCodeString(s)));

            // raise response received event
            RaiseResponseReceivedEvent(s);

            // retrigger receive callback (if still connected)
            if (server.ServerSocketStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                var result = server.ReceiveDataAsync(ReceiveDataCallback);
                if (result != SocketErrorCodes.SOCKET_OPERATION_PENDING)
                    TraceWarning("ReceiveDataCallback() error setting receive data callback: " + result);
            }
        }

        private void AutoListenCallback(object o)
        {
            if (AutoListen)
            {
                if (ConnectionState == ConnectionState.NotConnected)
                    Connect();
                else
                    TraceWarning("AutoListenCallback() called but not in expected state: " + ConnectionState);
            }
            else
                TraceError("AutoListenCallback() called but AutoListen is not enabled.");

            autoListenTimer.Dispose();
            autoListenTimer = null;
        }
        #endregion

        #region Object cleanup
        protected bool Reset()
        {
            try
            {
                // reset server
                if (server != null)
                {
                    server.SocketStatusChange -= SocketStatusChangeHandler;
                    server.DisconnectAll();
                    server = null;

                    if (ConnectionState != ConnectionState.NotConnected)
                        RaiseConnectionStateEvent(ConnectionState.NotConnected);

                    Trace("Reset() server reset.");
                }

                AutoListen = false;

                if (autoListenTimer != null)
                {
                    autoListenTimer.Stop();
                    autoListenTimer.Dispose();
                    autoListenTimer = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Reset() exception occurred.", ex);
                return false;
            }
        }

        /// <summary>
        /// Free up any unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Reset();
        }

        /// <summary>
        /// Object destructor
        /// </summary>
        ~TcpServerTransport()
        {
            Trace("~TcpServerTransport() object destructor called.");
            Dispose(false);
        }
        #endregion
    }
}