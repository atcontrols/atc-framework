using System;

namespace ATC.Framework.Communications
{
    public sealed class TelnetTransport : TcpClientTransport
    {
        private bool _negotiated = false;

        public const int PortDefault = 23;

        /// <summary>
        /// Telnet negotiation has been completed.
        /// </summary>
        public bool Negotiated
        {
            get => _negotiated;
            private set
            {
                if (_negotiated != value)
                {
                    _negotiated = value;
                    Trace($"Negotiated set to: {value}");
                    NegotiatedHandler?.Invoke(value);
                }
            }
        }

        #region Constructor

        public TelnetTransport(string hostname)
            : base(hostname, PortDefault) { }

        public TelnetTransport(string hostname, int port)
            : base(hostname, port) { }

        #endregion

        protected override void ParseResponse(string response)
        {
            // check for incoming telnet negotation string
            if (response.StartsWith("\xFF") && !Negotiated)
            {
                Trace($"ParseResponse() received negotiation line: \"{response.ToHexString()}\"");

                // construct reply
                string reply = response
                    .Replace('\xFB', '\xFE') // replace will with don't
                    .Replace('\xFD', '\xFC'); // replace do with won't

                // send response to server
                Trace($"ParseResponse() sending negotiation response: \"{response.ToHexString()}\"");
                Send(reply);
            }
            else
            {
                Negotiated = true;
                base.ParseResponse(response);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Negotiated = false;
            }
        }

        public event Action<bool> NegotiatedHandler;
    }
}
