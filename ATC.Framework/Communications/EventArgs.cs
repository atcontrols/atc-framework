using System;

namespace ATC.Framework.Communications
{
    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }

    public class ResponseReceivedEventArgs : EventArgs
    {
        public string Response { get; set; }
    }
}
