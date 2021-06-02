using System;

namespace ATC.Framework.Communications
{
    public interface ITransport
    {
        // properties
        ConnectionState ConnectionState { get; }

        // public methods
        bool Connect();
        bool Disconnect();
        bool Send(string s);

        // events
        event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;
        event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;
    }
}
