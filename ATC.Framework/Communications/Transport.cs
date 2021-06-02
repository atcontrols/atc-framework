using System;
using System.Text;

namespace ATC.Framework.Communications
{
    public abstract class Transport : SystemComponent, ITransport, IDisposable
    {
        #region Fields

        private Encoding _encoding = Encoding.GetEncoding("ISO-8859-1");
        private StringBuilder responseBuffer;

        #endregion

        #region Constants

        const int ResponseBufferCapacity = 4096;

        #endregion

        #region Properties

        public ConnectionState ConnectionState { get; private set; }

        /// <summary>
        /// If this is not an empty string, then ReceiveDataCallback will only be invoked when the specified delimeter is detected.
        /// The delimeter is usually used to mark the end of a line of text.
        /// </summary>
        public string Delimeter { get; set; }

        /// <summary>
        /// The text encoding to use for this transport.
        /// </summary>
        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

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

        #endregion

        #region Events

        /// <summary>
        /// Event that gets raised when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;

        /// <summary>
        /// Updates the Connected property in the Transport object and invokes the event callback.
        /// </summary>
        /// <param name="eConnectionState">The new state to set.</param>
        protected void RaiseConnectionStateEvent(ConnectionState state)
        {
            RaiseConnectionStateEvent(new ConnectionStateEventArgs(state));
        }

        /// <summary>
        /// Updates the Connected property in the Transport object and invokes the event callback.
        /// </summary>
        /// <param name="eConnectionState">The new state to set.</param>
        /// <param name="message">Optional message to attached to event.</param>
        protected void RaiseConnectionStateEvent(ConnectionState state, string message)
        {
            RaiseConnectionStateEvent(new ConnectionStateEventArgs(state, message));
        }

        /// <summary>
        /// Updates the Connected property in the Transport object and invokes the event callback.
        /// </summary>
        /// <param name="args">The new state to set.</param>
        protected void RaiseConnectionStateEvent(ConnectionStateEventArgs args)
        {
            try
            {
                if (args == null)
                {
                    TraceError("RaiseConnectionStateEvent() args cannot be null.");
                    return;
                }
                else if (args.State != ConnectionState)
                {
                    // update property
                    ConnectionState = args.State;

                    // invoke callback
                    if (ConnectionStateCallback != null)
                        ConnectionStateCallback(this, args);
                    else
                        TraceError("RaiseConnectionStateEvent() ConnectionStateCallback is null.");
                }
            }
            catch (Exception ex)
            {
                TraceException("RaiseConnectionStateEvent() exception caught.", ex);
            }
        }

        /// <summary>
        /// Event that gets raised when the Transport object receives a response.
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;

        protected void RaiseResponseReceivedEvent(string response)
        {
            RaiseResponseReceivedEvent(new ResponseReceivedEventArgs(response));
        }

        /// <summary>
        /// Process the received response looking for any delimeter and invokes the callback method.
        /// </summary>
        /// <param name="args"></param>
        protected void RaiseResponseReceivedEvent(ResponseReceivedEventArgs args)
        {
            try
            {
                // skip over empty replies
                if (args == null)
                {
                    TraceError("RaiseResponseReceivedEvent() args cannot be null.");
                    return;
                }
                else if (args.Response.Length == 0) // zero length response
                {
                    Trace("RaiseResponseReceivedEvent() response of 0 length detected. No action taken");
                    return;
                }
                else if (!string.IsNullOrEmpty(Delimeter)) // look for delimeter
                {
                    if (responseBuffer == null)
                    {
                        responseBuffer = new StringBuilder(args.Response); // create new StringBuilder
                        responseBuffer.Capacity = ResponseBufferCapacity;
                    }
                    else
                        responseBuffer.Append(args.Response); // add incoming string to buffer

                    Trace(string.Format("RaiseResponseReceivedEvent() looking for delimeter in: \"{0}\"", Utilities.ControlCodeString(responseBuffer.ToString())));

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
                            ResponseReceivedCallback(this, new ResponseReceivedEventArgs(chunk));
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
                else // no delimeter present
                {
                    // invoke response callback
                    if (ResponseReceivedCallback != null)
                        ResponseReceivedCallback(this, args);
                    else
                        TraceError("RaiseResponseReceivedEvent() ResponseReceivedCallback is null.");
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
            Trace("Object destructor called.");
            Dispose(false);
        }

        #endregion
    }

    #region Supporting enums and classes
    public enum ConnectionState
    {
        /// <summary>
        /// Not currently connected.
        /// </summary>
        NotConnected,

        /// <summary>
        /// Connection in progress.
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected and ready to send.
        /// </summary>
        Connected,

        /// <summary>
        /// Error detected while attempting to connect. Check message for details.
        /// </summary>
        ErrorConnecting,
    }

    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }

        public ConnectionStateEventArgs(ConnectionState state)
        {
            State = state;
            Message = String.Empty;
        }

        public ConnectionStateEventArgs(ConnectionState state, string message)
        {
            State = state;
            Message = message;
        }
    }

    public class ResponseReceivedEventArgs : EventArgs
    {
        public string Response { get; set; }

        public ResponseReceivedEventArgs(string response)
        {
            Response = response;
        }
    }
    #endregion
}
