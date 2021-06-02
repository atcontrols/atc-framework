using System;

namespace ATC.Framework.Devices
{
    public interface IPowerDevice : IDevice
    {
        /// <summary>
        /// The current state of power.
        /// </summary>
        Power Power { get; }

        event EventHandler<PowerEventArgs> PowerEventHandler;

        void SetPower(bool value);
    }

    public abstract class PowerDevice : Device, IPowerDevice
    {
        #region Fields
        private Power _power;
        #endregion

        #region Properties
        public Power Power
        {
            get { return _power; }
            protected set
            {
                if (_power != value)
                {
                    _power = value;
                    Trace("Power set to: " + value);

                    // raise event
                    if (PowerEventHandler != null)
                    {
                        var args = new PowerEventArgs()
                        {
                            Value = value,
                        };
                        PowerEventHandler(this, args);
                    }
                }
            }
        }
        #endregion

        #region Public methods
        public abstract void SetPower(bool value);
        #endregion

        #region Virtual methods
        protected override void Reset()
        {
            base.Reset();
            Power = Power.Unknown;
        }
        #endregion

        #region Events
        public event EventHandler<PowerEventArgs> PowerEventHandler;
        #endregion
    }

    public enum Power
    {
        Unknown,
        On,
        Off,
        Warming,
        Cooling,
    }

    public class PowerEventArgs : EventArgs
    {
        public Power Value { get; set; }
    }
}
