using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ATC.Framework.Communications
{
    public class TcpClientTransport : Transport
    {
        #region Fields

        private readonly TcpClient client = new TcpClient();
        private readonly byte[] readBuffer = new byte[4096];

        #endregion

        #region Properties

        public string Hostname { get; set; }
        public int Port { get; set; }

        #endregion

        #region Constructor

        public TcpClientTransport()
            : this(default, IPEndPoint.MinPort) { }

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
            var (isValid, message) = ValidateProperties();
            if (!isValid)
            {
                TraceError($"Connect() {message}");
                return;
            }

            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;
                client.ConnectAsync(Hostname, Port).ContinueWith((task) =>
                {
                    Trace("Connect() connection was successful.");
                    ConnectionState = ConnectionState.Connected;
                    _ = ReadStream(client.GetStream());
                });
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                Disconnect();
            }
        }

        public override void Disconnect()
        {
            if (client.Connected)
            {
                Trace("Disconnect() disconnecting from remote host.");
                client.Close();
            }

            client.Dispose();
            client.GetStream()?.Dispose();

            ConnectionState = ConnectionState.NotConnected;
        }

        public override void Send(string s)
        {
            var stream = client.GetStream();

            if (stream == null || !stream.CanWrite)
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
                            Trace($"Send() completed write of {bytes.Length}");
                    });
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                Disconnect();
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

        private async Task ReadStream(NetworkStream stream)
        {
            try
            {
                while (stream.CanRead)
                {
                    // read string response from stream
                    int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                    string response = Encoding.GetString(readBuffer, 0, bytesRead);
                    Trace($"ReadStream() received {bytesRead} bytes, content: \"{response.ToControlCodeString()}\"");

                    // raise event
                    RaiseResponseReceivedEvent(response);
                }

                TraceError("ReadStream() unable to read from stream.");
                Disconnect();
            }
            catch (Exception ex)
            {
                TraceException("ReadStream() exception caught.", ex);
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