using System;
using System.Threading.Tasks;

namespace ATC.Framework.Communications
{
    public interface ITransport
    {
        // properties
        ConnectionState ConnectionState { get; }

        // public methods
        bool Connect();
        Task<bool> ConnectAsync();
        bool Disconnect();
        bool Send(string s);
        Task<bool> SendAsync(string s);

        // events
        event EventHandler<ConnectionStateEventArgs> ConnectionStateCallback;
        event EventHandler<ResponseReceivedEventArgs> ResponseReceivedCallback;
    }
}
