using System;
using System.Text;
using System.Threading.Tasks;

namespace ATC.Framework.Communications
{
    public abstract class Transport : SystemComponent, ITransport, IDisposable
    {
        #region Fields

        private StringBuilder responseBuffer;
        private ConnectionState _connectionState;

        #endregion

        #region Constants

        private const int ResponseBufferCapacity = 4096;

        #endregion

        #region Properties

        public ConnectionState ConnectionState
        {
            get { return _connectionState; }
            protected set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    Trace($"ConnectionState set to: {value}");
                    RaiseConnectionStateEvent(value);
                }
            }
        }

        /// <summary>
        /// If set, the transport will automatically try and connect on sending.
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// If this is not an empty string, then ReceiveDataCallback will only be invoked when the specified delimeter is detected.
        /// The delimeter is usually used to mark the end of a line of text.
        /// </summary>
        public string Delimeter { get; set; }

        /// <summary>
        /// The text encoding to use for this transport.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.GetEncoding("ISO-8859-1");

        #endregion

        #region Constructor

        public Transport()
        {
            Delimeter = String.Empty;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Instruct the transport to connect.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public abstract bool Connect();

        public abstract Task<bool> ConnectAsync();

        /// <summary>
        /// Instruct the transport to disconnect.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public abstract bool Disconnect();

        /// <summary>
        /// Send the specified string via the transport.
        /// </summary>
        /// <param name="s">The string to send.</param>
        /// <returns>True on success, false on fail.</returns>
        public abstract bool Send(string s);

        public abstract Task<bool> SendAsync(string s);

        #endregion

        #region Events

        /// <summary>
        /// Event that gets raised when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;

        protected void RaiseConnectionStateEvent(ConnectionState state)
        {
            if (ConnectionStateCallback != null)
                ConnectionStateCallback(this, new ConnectionStateEventArgs() { State = state });
            else
                TraceError("RaiseConnectionStateEvent() ConnectionStateCallback is null.");
        }

        /// <summary>
        /// Event that gets raised when the Transport object receives a response.
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;

        /// <summary>
        /// Process the received response looking for any delimeter and invokes the callback method.
        /// </summary>
        /// <param name="args"></param>
        protected void RaiseResponseReceivedEvent(string response)
        {
            try
            {
                // skip over empty replies
                if (string.IsNullOrEmpty(response))
                    return;

                if (string.IsNullOrEmpty(Delimeter)) // no delimeter present
                {
                    var args = new ResponseReceivedEventArgs() { Response = response };
                    // invoke response callback
                    if (ResponseReceivedCallback != null)
                        ResponseReceivedCallback(this, args);
                    else
                        TraceError("RaiseResponseReceivedEvent() ResponseReceivedCallback is null.");
                }
                else // look for delimeter
                {
                    Trace($"RaiseResponseReceivedEvent() looking for delimeter in: \"{Utilities.ControlCodeString(responseBuffer.ToString())}\"");

                    // add to buffer
                    if (responseBuffer == null)
                        responseBuffer = new StringBuilder(response) { Capacity = ResponseBufferCapacity };
                    else
                        responseBuffer.Append(response); // add incoming string to buffer

                    // process buffer while looking for the delimeter
                    var index = responseBuffer.ToString().IndexOf(Delimeter);
                    var count = 0;
                    while (index != -1)
                    {
                        count++;

                        var length = index + Delimeter.Length;
                        Trace(string.Format("RaiseResponseReceivedEvent() delimeter found at index: {0}, length: {1}, count: {2}", index, length, count));

                        // remove chunk from buffer
                        var chunk = responseBuffer.ToString().Substring(0, length);
                        responseBuffer.Remove(0, length);
                        Trace(string.Format("RaiseResponseReceivedEvent() extracted chunk: \"{0}\", length: {1}, buffer size: {2}", Utilities.ControlCodeString(chunk), chunk.Length, responseBuffer.Length));

                        // invoke response callback
                        if (ResponseReceivedCallback != null)
                            ResponseReceivedCallback(this, new ResponseReceivedEventArgs() { Response = chunk });
                        else
                            TraceError("RaiseResponseReceivedEvent() ResponseReceivedCallback is null.");

                        // get next index
                        index = responseBuffer.ToString().IndexOf(Delimeter);
                    }

                    // report status
                    if (count == 0)
                        Trace("RaiseResponseReceivedEvent() delimeter not found in buffer at this stage. Buffer size: " + responseBuffer.Length);
                    else
                        Trace(string.Format("RaiseResponseReceivedEvent() finished processing {0} chunks. Buffer size: {1}", count, responseBuffer.Length));
                }
            }
            catch (Exception ex)
            {
                TraceException("RaiseResponseReceivedEvent() exception caught.", ex);
                responseBuffer = null;
            }
        }

        #endregion

        #region Object cleanup

        /// <summary>
        /// Free up any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        ~Transport()
        {
            Dispose(false);
        }

        #endregion
    }
}
