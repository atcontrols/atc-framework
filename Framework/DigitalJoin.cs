using ATC.Framework.Debugging;
using Crestron.SimplSharpPro.DeviceSupport;
using System;

namespace ATC.Framework
{
    public delegate void DigitalJoinHandler(DigitalJoin join);

    public enum DigitalJoinState
    {
        Pressed,
        Held,
        Released,
    }

    internal class DigitalJoinHoldParameters
    {
        /// <summary>
        /// How long (in milliseconds) should the button be held for.
        /// </summary>
        public long HoldTime { get; set; }

        /// <summary>
        /// How often (in milliseconds) should the event repeat while the button is held.
        /// </summary>
        public long RepeatTime { get; set; }
    }

    public class DigitalJoin : Join<DigitalJoinState>
    {
        internal DigitalJoinHandler Handler { get; set; }
        internal DigitalJoinHoldParameters HoldParams { get; set; }

        public DigitalJoin(uint number)
            : base(number, JoinType.Digital) { }

        public static void SetJoin(BasicTriList device, DigitalJoin join, bool value)
        {
            try
            {
                device.BooleanInput[join.Number].BoolValue = value;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DigitalJoin.SetJoin() exception caught." + ex.Message);
            }
        }

        public static bool GetJoin(BasicTriList device, DigitalJoin join)
        {
            try
            {
                return device.BooleanInput[join.Number].BoolValue;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DigitalJoin.GetJoin() exception caught." + ex.Message);
                return false;
            }
        }

        public static void ToggleJoin(BasicTriList device, DigitalJoin join)
        {
            try
            {
                device.BooleanInput[join.Number].BoolValue = !device.BooleanInput[join.Number].BoolValue;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DigitalJoin.ToggleJoin() exception caught." + ex.Message);
            }
        }

        public static void PulseJoin(BasicTriList device, DigitalJoin join)
        {
            try
            {
                device.BooleanInput[join.Number].Pulse();
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("DigitalJoin.PulseJoin() exception caught." + ex.Message);
            }
        }
    }
}
