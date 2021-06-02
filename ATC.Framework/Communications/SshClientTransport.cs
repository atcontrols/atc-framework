using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;
using Crestron.SimplSharp.Ssh.Messages.Transport;
using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public class SshClientTransport : Transport
    {
        #region Fields

        private SshClient client;
        private ShellStream stream;

        #endregion

        #region Constants

        public const int PortDefault = 22;
        private const int StreamBufferSize = 4096;

        #endregion

        #region Constructor

        public SshClientTransport()
            : this(null, PortDefault, null, null) { }

        public SshClientTransport(string hostname, string username, string password)
            : this(hostname, PortDefault, username, password) { }

        public SshClientTransport(string hostname, int port, string username, string password)
            : base()
        {
            // assign properties
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
        }

        #endregion

        #region Properties

        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Returns true if all required properties have been set correctly.
        /// </summary>
        public bool Initialized
        {
            get
            {
                if (string.IsNullOrEmpty(Hostname) || Port == 0 || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                    return false;

                return true;
            }

        }

        #endregion

        #region Public methods

        public override bool Connect()
        {
            if (!Initialized)
            {
                Trace("Connect() called but data is not initialized.");
                return false;
            }
            else if (client == null)
            {
                try
                {
                    // raise connecting event
                    Trace(String.Format("Connect() attempting connection to {0} on port {1}.", Hostname, Port));
                    RaiseConnectionStateEvent(ConnectionState.Connecting);

                    // create new client
                    client = new SshClient(Hostname, Port, Username, Password);
                    client.ErrorOccurred += new EventHandler<ExceptionEventArgs>(ClientErrorOccurredHandler);
                    client.HostKeyReceived += new EventHandler<HostKeyEventArgs>(ClientHostKeyEventHandler);

                    // attempt to connect
                    client.Connect();

                    // create shellstream
                    stream = client.CreateShellStream("terminal", 80, 24, 800, 600, 1024);
                    stream.DataReceived += new EventHandler<ShellDataEventArgs>(StreamDataReceivedHandler);
                    stream.ErrorOccurred += new EventHandler<ExceptionEventArgs>(StreamErrorOccurredHandler);

                    // report success
                    Trace("Connect() connection successful.");
                    RaiseConnectionStateEvent(ConnectionState.Connected);
                    return true;
                }
                catch (SshConnectionException ex)
                {
                    if (ex.DisconnectReason == DisconnectReason.None)
                    {
                        TraceException("Connect() connection exception. Timeout while connecting.", ex);
                        RaiseConnectionStateEvent(new ConnectionStateEventArgs(ConnectionState.ErrorConnecting, "Timeout while connecting."));
                    }
                    else
                    {
                        TraceException("Connect() connection exception. Reason: " + ex.DisconnectReason, ex);
                        RaiseConnectionStateEvent(new ConnectionStateEventArgs(ConnectionState.ErrorConnecting, ex.DisconnectReason.ToString()));
                    }
                    Reset();
                    return false;
                }
                catch (Exception ex)
                {
                    TraceException("Connect() exception caught.", ex);
                    Reset();
                    return false;
                }
            }
            else
            {
                Trace("Connect() called, but client already exists. Connection status: " + client.IsConnected);
                return client.IsConnected;
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
                    // check if stream is ready
                    if (stream != null && stream.CanWrite)
                    {
                        Trace(string.Format("Send() beginning write of: \"{0}\"", s.ToControlCodeString()));

                        // convert string to byte array
                        byte[] bytes = Encoding.GetBytes(s);
                        if (bytes == null)
                        {
                            TraceError("Send() received null string from GetBytes method. Unable to proceed.");
                            return false;
                        }

                        // send string to host
                        stream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, s);

                        return true;
                    }
                    else
                    {
                        TraceError("Send() stream is null or not writable.");
                        Reset();
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

        #region Event handlers

        private void WriteCallback(Crestron.SimplSharp.CrestronIO.IAsyncResult result)
        {
            try
            {
                if (result.IsCompleted)
                {
                    stream.EndWrite(result);
                    stream.Flush();

                    string s = result.AsyncState is string ?
                        (string)result.AsyncState : string.Empty;

                    Trace(string.Format("WriteCallback() {0} completed write of: \"{1}\"", result.CompletedSynchronously ? "synchronously" : "asynchronously", s.ToControlCodeString()));
                }
                else
                    TraceWarning("WriteCallback() called but write isn't completed.");
            }
            catch (Exception ex)
            {
                TraceException("WriteCallback() exception caught.", ex);
            }
        }

        private void ClientErrorOccurredHandler(object sender, ExceptionEventArgs args)
        {
            Trace("ClientErrorOccurredHandler() error occurred: " + args.Exception.Message);
            client = null;
            Reset();
        }

        private void ClientHostKeyEventHandler(object sender, HostKeyEventArgs args)
        {
            Trace("ClientHostKeyEventHandler() host key received.");
            args.CanTrust = true;
        }

        private void StreamDataReceivedHandler(object sender, ShellDataEventArgs args)
        {
            Trace("StreamDataReceivedHandler() received data. Length: " + args.Data.Length);

            var stream = (ShellStream)sender;

            while (stream.DataAvailable)
            {
                string data = stream.Read();
                RaiseResponseReceivedEvent(data);
            }
        }

        private void StreamErrorOccurredHandler(object sender, EventArgs args)
        {
            Trace("StreamErrorOccurredHandler() error occurred: " + args.ToString());
            stream = null;
            Reset();
        }

        #endregion

        #region Object cleanup

        private bool Reset()
        {
            try
            {
                Trace("Reset() resetting all objects.");

                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                    Trace("Reset() stream reset.");
                }

                if (client != null)
                {
                    if (client.IsConnected)
                        client.Disconnect();
                    client.Dispose();
                    client = null;

                    Trace("Reset() client reset.");
                }

                // raise NotConnected event
                if (ConnectionState != ConnectionState.NotConnected)
                    RaiseConnectionStateEvent(ConnectionState.NotConnected);

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Reset() exception caught.", ex);
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
        ~SshClientTransport()
        {
            Trace("~SshTransport() object destructor called.");
            Dispose(false);
        }

        #endregion
    }
}
