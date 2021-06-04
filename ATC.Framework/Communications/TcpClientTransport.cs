using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ATC.Framework.Communications
{
    public class TcpClientTransport : Transport
    {
        #region Fields

        private TcpClient client;

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
            // validate properties
            var (isValid, message) = ValidateProperties();
            if (!isValid)
            {
                TraceError($"Connect() {message}");
                return false;
            }

            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;
                client = new TcpClient(Hostname, Port);

                Trace("Connect() connection was successful.");
                ConnectionState = ConnectionState.Connected;
                _ = ReadStreamAsync(client.GetStream());

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                Disconnect();

                return false;
            }
        }

        public override async Task<bool> ConnectAsync()
        {
            // validate properties
            var (isValid, message) = ValidateProperties();
            if (!isValid)
            {
                TraceError($"ConnectAsync() {message}");
                return false;
            }

            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;
                client = new TcpClient();
                await client.ConnectAsync(Hostname, Port);

                Trace("Connect() connection was successful.");
                ConnectionState = ConnectionState.Connected;
                _ = ReadStreamAsync(client.GetStream());

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                Disconnect();

                return false;
            }
        }

        public override bool Disconnect()
        {
            if (client == null)
            {
                TraceError("Disconnect() client has not been initialized.");
                return false;
            }

            Trace(client.Connected ? "Disconnect() disconnecting from remote host." : "Disconnect() cleaning up resources.");

            client.Close();
            client = null;

            ConnectionState = ConnectionState.NotConnected;

            return true;
        }

        public override bool Send(string s)
        {
            // perform autoconnection logic
            if (AutoConnect && ConnectionState == ConnectionState.NotConnected)
                Connect();
            else if (client == null)
            {
                TraceError("Send() TCP client has not been initialized.");
                return false;
            }

            var stream = client.GetStream();
            if (!stream.CanWrite)
            {
                TraceError($"Send() network stream is not ready or not writable.");
                return false;
            }

            try
            {
                byte[] bytes = Encoding.GetBytes(s);
                Trace($"Send() sending {bytes.Length} bytes, content: \"{s.ToControlCodeString()}\"");
                stream.WriteAsync(bytes, 0, bytes.Length)
                    .ContinueWith((task) =>
                    {
                        if (task.IsFaulted)
                            TraceWarning("Send() error encountered while trying to send.");
                        else
                            Trace($"Send() completed write of {bytes.Length} bytes.");
                    });

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                Disconnect();
                return false;
            }
        }

        public override async Task<bool> SendAsync(string s)
        {
            // perform autoconnection logic
            if (AutoConnect && ConnectionState == ConnectionState.NotConnected)
                await ConnectAsync();
            else  if (client == null)
            {
                TraceError("Send() TCP client has not been initialized.");
                return false;
            }

            var stream = client.GetStream();
            if (!stream.CanWrite)
            {
                TraceError($"Send() network stream is not ready or not writable.");
                return false;
            }

            try
            {
                byte[] bytes = Encoding.GetBytes(s);
                Trace($"Send() sending {bytes.Length} bytes, content: \"{s.ToControlCodeString()}\"");
                await stream.WriteAsync(bytes, 0, bytes.Length);

                Trace($"Send() completed write of {bytes.Length} bytes.");
                return true;
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                Disconnect();
                return false;
            }
        }
        #endregion

        #region Private methods

        private (bool, string) ValidateProperties()
        {
            if (string.IsNullOrEmpty(Hostname))
                return (false, "Hostname is null or empty.");
            else if (Port <= IPEndPoint.MinPort || Port > IPEndPoint.MaxPort)
                return (false, $"Invalid port number: {Port}");

            return (true, string.Empty);
        }

        private async Task ReadStreamAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (stream.CanRead)
                {
                    // read string response from stream
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.GetString(buffer, 0, bytesRead);
                    Trace($"ReadStream() received {bytesRead} bytes, content: \"{response.ToControlCodeString()}\"");

                    // raise event
                    RaiseResponseReceivedEvent(response);
                }

                TraceError("ReadStreamAsync() unable to read from stream.");
                Disconnect();
            }
            catch (Exception ex)
            {
                TraceException("ReadStreamAsync() exception caught.", ex);
                Disconnect();
            }
        }

        #endregion

        #region Object cleanup

        /// <summary>
        /// Free up any unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }

        #endregion
    }
}