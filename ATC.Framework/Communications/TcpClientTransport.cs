using Crestron.SimplSharp.CrestronSockets;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public class TcpClientTransport : Transport, IDisposable
    {
        #region Fields

        private TCPClient client;

        #endregion

        #region Constants
        private const int ClientBufferSize = 4096;
        #endregion

        #region Properties
        public string Hostname { get; set; }
        public int Port { get; set; }
        #endregion

        #region Constructor
        public TcpClientTransport(string hostname, int port)
            : base()
        {
            Hostname = hostname;
            Port = port;
        }
        #endregion

        #region Public methods
        public override bool Connect()
        {
            // create new client object
            if (client == null)
            {
                client = new TCPClient(Hostname, Port, ClientBufferSize);
                client.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(ClientSocketStatusChange);
                Trace("Connect() created new client object.");
            }

            // check if an existing attempt is in progress
            if (ConnectionState == ConnectionState.Connecting)
            {
                Trace("Connect() an existing attempt to connect is in progress.");
                return false;
            }

            // attempt to connect asynchronously
            if (client.ClientStatus == SocketStatus.SOCKET_STATUS_NO_CONNECT)
            {
                Trace("Connect() attempting connection to: " + Hostname + " on port: " + Port);
                SocketErrorCodes code = client.ConnectToServerAsync(ClientConnectCallback);

                switch (code)
                {
                    case SocketErrorCodes.SOCKET_OK:
                    case SocketErrorCodes.SOCKET_OPERATION_PENDING:
                        if (ConnectionState == ConnectionState.NotConnected)
                        {
                            Trace("Connect() successfully created connection request.");
                            RaiseConnectionStateEvent(ConnectionState.Connecting);
                        }
                        return true;

                    default:
                        TraceError("Connect() could not create connection request: " + code);
                        RaiseConnectionStateEvent(new ConnectionStateEventArgs(ConnectionState.ErrorConnecting, "Error connecting: " + code.ToString()));
                        RaiseConnectionStateEvent(new ConnectionStateEventArgs(ConnectionState.NotConnected));
                        Reset();
                        return false;
                }
            }
            else
            {
                TraceError("Connect() could not connect. Client status: " + client.ClientStatus);
                Reset();
                return false;
            }
        }

        public override bool Disconnect()
        {
            try
            {
                // H.O. - Fix for TCP sockets staying open to the server, after this method was called.
                // Updated this method to no longer call return Reset(); here, as it turned out to be disposing the item before the disconnect occurred.  Instead, when a client.DisconnectFromServer() occurs,
                // ClientSocketStatusChange() is called with a "SocketStatus.SOCKET_STATUS_NO_CONNECT", which then calls the Reset() for us, but only after a disconnect has occurred, which is when we want it to be.

                // disconnect any active connection
                if (client != null)
                {
                    if (client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                    {
                        client.DisconnectFromServer();
                        Trace("Disconnect() disconnect request initiated.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Disconnect() exception occurred.", ex);
                return false;
            }
        }

        public override bool Send(string s)
        {
            try
            {
                if (client == null)
                {
                    TraceError("Send() cannot send as client is null.");
                    return false;
                }
                else if (client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                {
                    // encode string
                    byte[] bytes = Encoding.GetBytes(s);

                    // send string to host                   
                    SocketErrorCodes code = client.SendDataAsync(bytes, bytes.Length, ClientSendCallback);
                    if (code == SocketErrorCodes.SOCKET_OPERATION_PENDING)
                    {
                        Trace("Send() succesfully dispatched asynchronous request.");
                        return true;
                    }
                    else
                    {
                        TraceError(String.Format("Send() error occurred while sending, code: {0}", code));
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

        #region TCP client event handlers
        protected virtual void ClientConnectCallback(TCPClient client)
        {
            if (client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                // raise error connecting event
                string message = "Could not complete connection: " + client.ClientStatus;
                RaiseConnectionStateEvent(ConnectionState.ErrorConnecting, message);

                // raise not connected event
                RaiseConnectionStateEvent(ConnectionState.NotConnected);
                Reset();
            }
        }

        protected virtual void ClientSendCallback(TCPClient client, int numberOfBytesSent)
        {
            Trace(string.Format("ClientSendCallback() sent: {0} bytes successfully.", numberOfBytesSent));
        }

        protected virtual void ClientSocketStatusChange(TCPClient client, SocketStatus status)
        {
            Trace("ClientSocketStatusChange() status: " + status);

            switch (status)
            {
                case SocketStatus.SOCKET_STATUS_CONNECTED:
                    RaiseConnectionStateEvent(ConnectionState.Connected);
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

        protected virtual void ClientReceiveDataHandler(TCPClient client, int bytesReceived)
        {
            try
            {
                // decode incoming bytes
                string data = Encoding.GetString(client.IncomingDataBuffer, 0, bytesReceived);
                Trace("ClientReceiveDataHandler() received " + bytesReceived + " bytes.");

                // raise response received event
                RaiseResponseReceivedEvent(data);

                // listen for more responses
                client.ReceiveDataAsync(ClientReceiveDataHandler);
            }
            catch (Exception ex)
            {
                TraceException("ClientReceiveDataHandler() exception occurred.", ex);
            }
        }
        #endregion

        #region Object cleanup
        protected bool Reset()
        {
            try
            {
                // reset client
                if (client != null)
                {
                    // disconnect any active connection
                    if (client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                        client.DisconnectFromServer();

                    client.Dispose();
                    client = null;
                    Trace("Reset() client reset.");
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
        ~TcpClientTransport()
        {
            Trace("~TcpClientTransport() object destructor called.");
            Dispose(false);
        }
        #endregion
    }
}