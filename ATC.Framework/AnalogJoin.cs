using ATC.Framework.Debugging;
using Crestron.SimplSharpPro.DeviceSupport;
using System;

namespace ATC.Framework
{
    public delegate void AnalogJoinHander(AnalogJoin join);

    public class AnalogJoin : Join<ushort>
    {
        internal AnalogJoinHander Handler { get; set; }

        public AnalogJoin(uint number)
            : base(number, JoinType.Analog) { }

        public static void SetJoin(BasicTriList device, AnalogJoin join, ushort value)
        {
            try
            {
                device.UShortInput[join.Number].UShortValue = value;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("AnalogJoin.SetJoin() exception caught." + ex.Message);
            }
        }

        public static ushort GetJoin(BasicTriList device, AnalogJoin join)
        {
            try
            {
                return device.BooleanInput[join.Number].UShortValue;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("AnalogJoin.GetJoin() exception caught." + ex.Message);
                return 0;
            }
        }
    }
}
