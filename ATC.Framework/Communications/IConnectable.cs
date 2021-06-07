namespace ATC.Framework.Communications
{
    public interface IConnectable : ISystemComponent
    {
        ConnectionState ConnectionState { get; }

        void Connect();
        void Disconnect();
    }
}