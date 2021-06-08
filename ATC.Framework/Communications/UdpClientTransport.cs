using System;
using System.Net.Sockets;

namespace ATC.Framework.Communications
{
    public class UdpClientTransport : Transport
    {
        private UdpClient client;

        public string Hostname { get; private set; }
        public int Port { get; private set; }

        public UdpClientTransport(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        public override bool Connect()
        {
            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;

                client = new UdpClient();
                client.Connect(Hostname, Port);
                ConnectionState = ConnectionState.Connected;
                return true;
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                return false;
            }
        }

        public override bool Disconnect()
        {
            if (client == null)
                return false;
            else
            {
                Dispose();
                return true;
            }
        }

        public override bool Send(string s)
        {
            if (client == null)
            {
                TraceError("Send() UDP client has not been initialized.");
                return false;
            }

            try
            {
                byte[] bytes = Encoding.GetBytes(s);
                int bytesSent = client.Send(bytes, bytes.Length);
                Trace($"Send() sent {bytesSent} bytes.");
                return true;
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (client != null)
                {
                    Trace($"Dispose() cleaning up resources.");
                    client.Close();
                    client.Dispose();
                    client = null;
                }

                ConnectionState = ConnectionState.NotConnected;
            }
        }
    }
}
