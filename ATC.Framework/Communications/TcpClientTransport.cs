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

        /// <summary>
        /// How long to wait before an operation times out.
        /// </summary>
        public int Timeout { get; set; } = 5000;

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

        public override void Connect()
        {
            // validate properties
            var (isValid, message) = ValidateProperties();
            if (!isValid)
            {
                TraceError($"Connect() {message}");
                return;
            }

            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}, timeout is: {Timeout}ms");
                ConnectionState = ConnectionState.Connecting;
                client = new TcpClient();
                bool success = client.ConnectAsync(Hostname, Port).Wait(Timeout);
                if (success)
                {
                    Trace("Connect() connection was successful.");
                    ConnectionState = ConnectionState.Connected;
                    _ = ReadStreamAsync(client.GetStream());
                }
                else
                {
                    TraceError($"Connect() could not complete connection within timeout period.");
                    Dispose();
                }
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                Dispose();
            }
        }

        public override void Disconnect()
        {
            Dispose();
        }

        public override void Send(string s)
        {
            // perform autoconnection logic
            if (AutoConnect && ConnectionState == ConnectionState.NotConnected)
                Connect();
            else if (client == null)
            {
                TraceError("Send() TCP client has not been initialized.");
                return;
            }

            var stream = client.GetStream();
            if (!stream.CanWrite)
            {
                TraceError($"Send() network stream is not ready or not writable.");
                return;
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
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                Dispose();
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
                Dispose();
            }
            catch (Exception ex)
            {
                TraceException("ReadStreamAsync() exception caught.", ex);
                Dispose();
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
                if (client != null)
                {
                    string message = client.Connected ? "disconnecting from remote host and cleaning up." : "cleaning up resources.";
                    Trace($"Dispose() {message}");
                    client.Close();
                    client.Dispose();
                    client = null;
                }

                ConnectionState = ConnectionState.NotConnected;
            }
        }

        #endregion
    }
}