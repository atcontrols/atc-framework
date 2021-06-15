using System;
using System.IO;
using System.Text;

namespace ATC.Framework.Communications
{
    public interface ITransport : IConnectable
    {
        // properties
        bool AutoConnect { get; set; }
        string Delimeter { get; set; }
        Encoding Encoding { get; set; }

        // methods        
        bool Send(string s);

        // events
        event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;
        event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;
    }

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

        #region Public methods

        /// <summary>
        /// Instruct the transport to connect.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public abstract bool Connect();

        /// <summary>
        /// Instruct the transport to disconnect.
        /// </summary>
        public abstract bool Disconnect();

        /// <summary>
        /// Send the specified string via the transport.
        /// </summary>
        /// <param name="s">The string to send.</param>
        public abstract bool Send(string s);

        #endregion

        /// <summary>
        /// Attempt to read from the specified stream continuously.
        /// </summary>
        /// <param name="stream"></param>
        protected void ReadStream(Stream stream)
        {
            byte[] buffer = new byte[4096];

            try
            {
                int bytesRead;
                while (stream.CanRead && (bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // read stream into buffer
                    Trace($"ReadStream() received {bytesRead} bytes.");

                    // decode byte array
                    string response = Encoding.GetString(buffer, 0, bytesRead);
                    ParseResponse(response);
                }

                TraceWarning("ReadStream() stream is not readable or 0 bytes read.");
                Dispose();
            }
            catch (Exception ex)
            {
                TraceException("ReadStream() exception caught.", ex);
                Dispose();
            }
        }

        protected void ReadBuffer(byte[] buffer, int bufferLength)
        {
            try
            {
                Trace($"ReadBuffer() attempting to decode {bufferLength} bytes.");

                // decode byte array
                string response = Encoding.GetString(buffer, 0, bufferLength);
                ParseResponse(response);
            }
            catch (Exception ex)
            {
                TraceException("ReadBuffer() exception caught.", ex);
                Dispose();
            }
        }

        protected virtual void ParseResponse(string response)
        {
            if (string.IsNullOrEmpty(Delimeter)) // no delimeter present
            {
                // invoke response callback
                RaiseResponseReceivedEvent(response);
            }
            else // look for delimeter
            {
                try
                {
                    // add to buffer
                    if (responseBuffer == null)
                        responseBuffer = new StringBuilder(response) { Capacity = ResponseBufferCapacity };
                    else
                        responseBuffer.Append(response); // add incoming string to buffer

                    Trace($"RaiseResponseReceivedEvent() looking for delimeter in: \"{responseBuffer.ToString().ToControlCodeString()}\"");

                    // process buffer while looking for the delimeter
                    var index = responseBuffer.ToString().IndexOf(Delimeter);
                    var count = 0;
                    while (index != -1)
                    {
                        count++;

                        var length = index + Delimeter.Length;
                        Trace(string.Format("RaiseResponseReceivedEvent() delimeter found at index: {0}, length: {1}, count: {2}", index, length, count));

                        // remove chunk from buffer
                        string chunk = responseBuffer.ToString().Substring(0, length);
                        responseBuffer.Remove(0, length);
                        Trace($"RaiseResponseReceivedEvent() extracted chunk: \"{chunk.ToControlCodeString()}\", length: {chunk.Length}, buffer size: {responseBuffer.Length}");

                        // invoke response callback
                        RaiseResponseReceivedEvent(chunk);

                        // get next index
                        index = responseBuffer.ToString().IndexOf(Delimeter);
                    }

                    // report status
                    string message = count == 0 ?
                        $"delimeter not found in buffer at this stage" :
                        $"finished processing {count} chunks";

                    Trace($"ProcessBytes() {message}. Buffer size: {responseBuffer.Length}");
                }
                catch (Exception ex)
                {
                    TraceException(ex, nameof(ParseResponse), "Error occurred while parsing response.");
                    responseBuffer = null;
                }
            }
        }

        #region Events

        /// <summary>
        /// Event that gets raised when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;

        private void RaiseConnectionStateEvent(ConnectionState state)
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
        private void RaiseResponseReceivedEvent(string response)
        {
            try
            {
                if (ResponseReceivedCallback != null)
                    ResponseReceivedCallback(this, new ResponseReceivedEventArgs() { Response = response });
                else
                    TraceError("RaiseResponseReceivedEvent() ResponseReceivedCallback is null.");
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
