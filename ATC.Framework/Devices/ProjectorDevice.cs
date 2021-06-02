using System;

namespace ATC.Framework.Devices
{
    public interface IProjectorDevice : IDisplayDevice
    {
        /// <summary>
        /// The number of hours that the lamp / LED has been active.
        /// </summary>
        uint LampHours { get; }

        event EventHandler<LampHoursEventArgs> LampHoursEventHandler;
    }

    public abstract class ProjectorDevice : DisplayDevice, IProjectorDevice
    {
        private uint _lampHours;

        public uint LampHours
        {
            get { return _lampHours; }
            protected set
            {
                if (_lampHours != value)
                {
                    _lampHours = value;
                    Trace("LampHours set to: " + value);

                    // raise event
                    if (LampHoursEventHandler != null)
                    {
                        var args = new LampHoursEventArgs()
                        {
                            Value = value,
                        };
                        LampHoursEventHandler(this, args);
                    }
                }
            }
        }

        public event EventHandler<LampHoursEventArgs> LampHoursEventHandler;
    }

    public class LampHoursEventArgs : EventArgs
    {
        public uint Value { get; set; }
    }
}
