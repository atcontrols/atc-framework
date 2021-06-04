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

        public override Task<bool> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public override bool Disconnect()
        {
            try
            {
                if (client == null)
                {
                    TraceError("Disconnect() client has not been initialized.");
                    return false;
                }

                client.Close();
                client = null;
                ConnectionState = ConnectionState.NotConnected;

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Disconnect() exception caught.", ex);
                return false;
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

        public override Task<bool> SendAsync(string s)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }
    }
}
