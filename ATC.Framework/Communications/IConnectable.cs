namespace ATC.Framework.Communications
{
    public interface IConnectable : ISystemComponent
    {
        ConnectionState ConnectionState { get; }

        bool Connect();
        bool Disconnect();
    }
}