using System;

namespace ATC.Framework.Devices
{
    public interface IScreenDevice : IDevice
    {
        /// <summary>
        /// The current position of the screen.
        /// </summary>
        ScreenPosition Position { get; }

        void Up();
        void Down();
        void Stop();

        event EventHandler<ScreenPositionEventArgs> ScreenPositionHandler;
    }

    public abstract class ScreenDevice : Device, IScreenDevice
    {
        private ScreenPosition _position;

        public ScreenPosition Position
        {
            get { return _position; }
            protected set
            {
                if (_position != value)
                {
                    _position = value;
                    Trace("State has been set to: " + value);

                    if (ScreenPositionHandler != null)
                    {
                        var args = new ScreenPositionEventArgs()
                        {
                            Value = value,
                        };
                        ScreenPositionHandler(this, args);
                    }
                }
            }
        }

        public abstract void Up();
        public abstract void Down();
        public abstract void Stop();

        public event EventHandler<ScreenPositionEventArgs> ScreenPositionHandler;
    }

    public enum ScreenPosition
    {
        /// <summary>
        /// The screen is in an unknown position.
        /// </summary>
        Unknown,

        /// <summary>
        /// The screen is stopped at the top.
        /// </summary>
        AtTop,

        /// <summary>
        /// The screen is stopped at the bottom.
        /// </summary>
        AtBottom,

        /// <summary>
        /// The screen has been manually stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The screen is moving towards to the top.
        /// </summary>
        MovingTop,

        /// <summary>
        /// The screen is moving towards the bottom.
        /// </summary>
        MovingBottom,
    }

    public class ScreenPositionEventArgs : EventArgs
    {
        public ScreenPosition Value { get; set; }
    }
}
