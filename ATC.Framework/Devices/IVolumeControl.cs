namespace ATC.Framework.Devices
{
    public interface IVolumeControl
    {
        int Volume { get; }

        void SetVolume(int value);
    }
}
