using System;

namespace ATC.Framework.Devices
{
    public interface IVolumeDevice
    {
        int Volume { get; }

        void SetVolume(int value);

        event EventHandler<VolumeEventArgs> VolumeEventHandler;
    }
}
