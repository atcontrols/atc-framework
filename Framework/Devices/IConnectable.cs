using ATC.Framework.Communications;

namespace ATC.Framework.Devices
{
    public interface IConnectable
    {
        ConnectionState ConnectionState { get; }

        void Connect();
        void Disconnect();
    }
}