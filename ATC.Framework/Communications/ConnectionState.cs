namespace ATC.Framework.Communications
{
    public enum ConnectionState
    {
        /// <summary>
        /// Not currently connected.
        /// </summary>
        NotConnected,

        /// <summary>
        /// Connection in progress.
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected and ready to send.
        /// </summary>
        Connected,

        /// <summary>
        /// In the process of disconnecting.
        /// </summary>
        Disconnecting,
    }
}

