using System;

namespace ATC.Framework.Devices
{
    public interface IDisplayDevice : IPowerDevice, IInputDevice, IVolumeDevice { }

    public abstract class DisplayDevice : PowerDevice, IDisplayDevice
    {
        private string _input;
        private int _volume;
        private bool _mute;

        /// <summary>
        /// The currently active input for this display.
        /// </summary>
        public string Input
        {
            get { return _input; }
            protected set
            {
                if (_input != value)
                {
                    _input = value;
                    Trace("Input set to: " + value);

                    // raise event
                    if (InputEventHandler != null)
                    {
                        var args = new InputEventArgs()
                        {
                            Value = value,
                        };
                        InputEventHandler(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// The current volume level for this display.
        /// </summary>
        public int Volume
        {
            get { return _volume; }
            protected set
            {
                if (_volume != value)
                {
                    _volume = value;
                    Trace("Volume set to: " + value);

                    // raise event
                    VolumeEventHandler?.Invoke(this, new VolumeEventArgs() { Value = value });
                }
            }
        }

        public bool Mute
        {
            get => _mute;
            protected set
            {
                if (_mute != value)
                {
                    _mute = value;
                    Trace("Mute set to: " + value);

                    // raise event
                    MuteEventHandler?.Invoke(this, new MuteEventArgs() { Value = value });
                }
            }
        }

        public abstract string[] GetInputs();
        public abstract void SetInput(string value);
        public abstract void SetVolume(int value);
        public abstract void SetMute(bool value);

        public event EventHandler<InputEventArgs> InputEventHandler;
        public event EventHandler<VolumeEventArgs> VolumeEventHandler;
        public event EventHandler<MuteEventArgs> MuteEventHandler;
    }

    public class InputEventArgs : EventArgs
    {
        public string Value { get; set; }
    }

    public class VolumeEventArgs : EventArgs
    {
        public int Value { get; set; }
    }

    public class MuteEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }
}
