using System;

namespace ATC.Framework.Devices
{
    public interface IVolumeDevice
    {
        int Volume { get; }
        bool Mute { get; }

        void SetVolume(int value);
        void SetMute(bool value);

        event EventHandler<VolumeEventArgs> VolumeEventHandler;
        event EventHandler<MuteEventArgs> MuteEventHandler;
    }
}
