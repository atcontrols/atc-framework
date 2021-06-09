using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public class SshClientTransport : Transport
    {
        #region Fields

        private SshClient client;

        #endregion

        #region Constants

        public const int PortDefault = 22;
        public const int TimeoutDefault = 5000;

        #endregion

        #region Properties

        /// <summary>
        /// The hostname to connect to.
        /// </summary>
        public string Hostname { get; private set; }

        /// <summary>
        /// The TCP port number to use. SSH standard is 22.
        /// </summary>
        public int Port { get; private set; } = PortDefault;

        /// <summary>
        /// Username to use for authentication.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Password to use for authentication.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// How long (in milliseconds) to wait for a connection to complete.
        /// </summary>
        public int Timeout { get; set; } = TimeoutDefault;

        #endregion

        #region Constructor

        public SshClientTransport(string hostname, string username, string password)
           : this(hostname, PortDefault, username, password) { }

        public SshClientTransport(string hostname, int port, string username, string password)
        {
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;

            // set the default encoding
            Encoding = Encoding.UTF8;
        }

        #endregion

        #region Public methods

        public override bool Connect()
        {
            if (client != null)
            {
                TraceError("Connect() called but client already initialized.");
                return false;
            }

            try
            {
                // create new client using password authentication
                PasswordAuthenticationMethod passwordAuth = new PasswordAuthenticationMethod(Username, Password);
                ConnectionInfo connectionInfo = new ConnectionInfo(Hostname, Port, Username, passwordAuth)
                {
                    Timeout = new TimeSpan(hours: 0, minutes: 0, seconds: Timeout / 1000)
                };
                client = new SshClient(connectionInfo);

                // handle receipt of host key
                client.HostKeyReceived += (sender, e) =>
                {
                    string fingerprint = Encoding.GetString(e.FingerPrint);
                    Trace($"Connect() received fingerprint {fingerprint.ToHexString()}, length {e.FingerPrint.Length}");
                    e.CanTrust = true; // trust this hostkey
                };

                // handle any client errors
                client.ErrorOccurred += (sender, e) =>
                {
                    TraceError($"Connect() client error occurred: {e.Exception.Message}");
                    Dispose();
                };

                // connect to the remote host
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;
                client.Connect();

                Trace("Connect() connection was successful.");
                ConnectionState = ConnectionState.Connected;

                // create shell stream
                string terminalName = nameof(SshClientTransport);
                ShellStream stream = client.CreateShellStream(terminalName, 80, 24, 800, 600, 1024);
                stream.DataReceived += StreamDataReceivedHandler;
                stream.ErrorOccurred += StreamErrorOccurredHandler;

                return true;
            }
            catch (SshAuthenticationException ex)
            {
                TraceException(ex, nameof(Connect), "Authentication failure.");
                Dispose();
                return false;
            }
            catch (SshOperationTimeoutException ex)
            {
                TraceException(ex, nameof(Connect), "Timed out while trying to connect.");
                Dispose();
                return false;
            }
            catch (Exception ex)
            {
                TraceException(ex, nameof(Connect));
                Dispose();
                return false;
            }
        }

        public override bool Disconnect()
        {
            throw new NotImplementedException();
        }

        public override bool Send(string s)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (client != null)
            {
                string message = client.IsConnected ? "disconnecting from remote host and cleaning up." : "cleaning up resources.";
                Trace($"Dispose() {message}");
                client.Disconnect();
                client.Dispose();
                client = null;
            }

            ConnectionState = ConnectionState.NotConnected;
        }

        #endregion

        #region Event handlers

        private void StreamDataReceivedHandler(object sender, ShellDataEventArgs e)
        {
            Trace($"StreamDataReceivedHandler() received {e.Data.Length} bytes.");
            string response = Encoding.GetString(e.Data);
            Trace($"StreamDataReceivedHandler() received string: \"{response.ToControlCodeString()}\"");

            // raise event
            RaiseResponseReceivedEvent(response);
        }

        private void StreamErrorOccurredHandler(object sender, ExceptionEventArgs e)
        {
            TraceError($"StreamDataReceivedHandler() stream error occurred: {e.Exception.Message}");
            Dispose();
        }

        #endregion
    }
}
