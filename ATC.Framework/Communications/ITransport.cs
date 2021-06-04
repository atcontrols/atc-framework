using System;

namespace ATC.Framework.Communications
{
    public interface ITransport
    {
        // properties
        ConnectionState ConnectionState { get; }

        // public methods
        void Connect();
        void Disconnect();
        void Send(string s);

        // events
        event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;
        event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;
    }
}
