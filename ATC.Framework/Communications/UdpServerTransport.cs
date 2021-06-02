using Crestron.SimplSharp.CrestronSockets;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public class UdpServerTransport : Transport, IDisposable
    {
        #region Fields

        private readonly UDPServer server;

        #endregion

        #region Constants

        private const int BufferSize = 4096;

        #endregion

        #region Properties

        public string Hostname
        {
            get
            {
                if (server == null)
                    return string.Empty;
                else
                    return server.AddressToAcceptConnectionFrom;
            }
        }

        public int LocalPort
        {
            get
            {
                if (server == null)
                    return 0;
                else
                    return server.PortNumber;
            }
        }

        public int RemotePort
        {
            get
            {
                if (server == null)
                    return 0;
                else
                {
                    if (server.RemotePortNumber == 0)
                        return LocalPort;
                    else
                        return server.RemotePortNumber;
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Create new UdpServerTransport
        /// </summary>
        /// <param name="hostname">The hostname to connect to / accept connection from. Can be set to "0.0.0.0" to accept connection from any address.</param>
        /// <param name="portNumber">The local and remote port number to use.</param>
        public UdpServerTransport(string hostname, int portNumber)
            : this(hostname, portNumber, 0)
        {
        }

        /// <summary>
        /// Create new UdpServerTransport
        /// </summary>
        /// <param name="hostname">The hostname to connect to / accept connection from. Can be set to "0.0.0.0" to accept connection from any address.</param>
        /// <param name="localPort">The local port number on this system to use.</param>
        /// <param name="remotePort">The remote port number to connect to.</param>
        public UdpServerTransport(string hostname, int localPort, int remotePort)
            : base()
        {
            // create udp server object
            if (remotePort > 0)
                server = new UDPServer(hostname, localPort, BufferSize, remotePort);
            else
                server = new UDPServer(hostname, localPort, BufferSize);
        }

        #endregion

        #region Public methods

        public override bool Connect()
        {
            try
            {
                if (server != null && ConnectionState == ConnectionState.NotConnected)
                {
                    RaiseConnectionStateEvent(ConnectionState.Connecting);

                    // try to enable udp server
                    SocketErrorCodes code = server.EnableUDPServer();
                    if (code != SocketErrorCodes.SOCKET_OK)
                    {
                        TraceError("Connect() enable UDP server error. Code: " + code);
                        return false;
                    }

                    RaiseConnectionStateEvent(ConnectionState.Connected);

                    // set up receive data callback
                    server.ReceiveDataAsync(ReceiveResponseCallback);

                    return true;
                }
                else
                {
                    if (server == null)
                        TraceError("Connect() server is null.");
                    else
                        TraceError("Connect() server is not in NotConnected state.  Connection state = " + ConnectionState);
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
                if (ConnectionState == ConnectionState.Connected)
                {
                    // send string to host                   
                    byte[] bytes = Encoding.GetBytes(s);
                    SocketErrorCodes code = server.SendData(bytes, bytes.Length);
                    if (code == SocketErrorCodes.SOCKET_OK)
                    {
                        Trace(String.Format("Send() sent: \"{0}\" successfully.", s.Trim()));
                        return true;
                    }
                    else
                    {
                        TraceError(String.Format("Send() could not send: \"{0}\", code: .", s.Trim(), code));
                        return false;
                    }
                }
                else
                {
                    TraceError("Send() cannot send as not connected.");
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

        #region Event callbacks

        void ReceiveResponseCallback(UDPServer server, int bytesReceived)
        {
            try
            {
                Trace("ReceiveResponseCallback() received " + bytesReceived + " bytes.");

                // decode response
                byte[] buffer = server.IncomingDataBuffer;
                string response = Encoding.GetString(buffer, 0, bytesReceived);

                // raise response received event
                RaiseResponseReceivedEvent(response);

                // listen for more responses
                server.ReceiveDataAsync(ReceiveResponseCallback);
            }
            catch (Exception ex)
            {
                TraceException("ReceiveResponseCallback() exception caught.", ex);
            }
        }

        #endregion

        #region Object cleanup

        bool Reset()
        {
            try
            {
                if (server != null)
                {
                    SocketErrorCodes code = server.DisableUDPServer();
                    if (code == SocketErrorCodes.SOCKET_OK)
                    {
                        RaiseConnectionStateEvent(ConnectionState.NotConnected);
                        Trace("Reset() successfully disabled UDP server.");
                        return true;
                    }
                    else
                    {
                        TraceError("Reset() couldn't disable UDP server. Code: " + code);
                        return false;
                    }
                }
                else
                {
                    Trace("Reset() server is null. Nothing to reset.");
                    return true;
                }
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
        ~UdpServerTransport()
        {
            Trace("~UdpServerTransport() object destructor called.");
            Dispose(false);
        }

        #endregion
    }
}
