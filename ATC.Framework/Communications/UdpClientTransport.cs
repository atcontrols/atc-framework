using System;
using System.Net.Sockets;
using System.Threading.Tasks;

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

        public override void Connect()
        {
            try
            {
                Trace($"Connect() attempting connection to: {Hostname} on port: {Port}");
                ConnectionState = ConnectionState.Connecting;

                client = new UdpClient();
                client.Connect(Hostname, Port);
                ConnectionState = ConnectionState.Connected;
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
            }
        }       

        public override void Disconnect()
        {
            Dispose();
        }

        public override void Send(string s)
        {
            if (client == null)
            {
                TraceError("Send() UDP client has not been initialized.");
                return;
            }

            try
            {
                byte[] bytes = Encoding.GetBytes(s);
                int bytesSent = client.Send(bytes, bytes.Length);
                Trace($"Send() sent {bytesSent} bytes.");
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
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
