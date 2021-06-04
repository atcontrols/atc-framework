using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ATC.Framework.Communications
{
    public class UdpClientTransport : Transport
    {
        private readonly UdpClient client = new UdpClient();

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
                client.Connect(Hostname, Port);
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
            }
        }

        public override void Disconnect()
        {
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                TraceException("Disconnect() exception caught.", ex);
            }
        }

        public override void Send(string s)
        {
            try
            {
                byte[] bytes = Encoding.GetBytes(s);
                client.SendAsync(bytes, bytes.Length);
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
                Disconnect();
            }
        }
    }
}
