using ATC.Framework.Debugging;
using Crestron.SimplSharpPro.DeviceSupport;
using System;

namespace ATC.Framework
{
    public delegate void SerialJoinHandler(SerialJoin join);

    public class SerialJoin : Join<string>
    {
        internal SerialJoinHandler Handler { get; set; }

        public SerialJoin(uint number)
            : base(number, JoinType.Serial) { }

        public static void SetJoin(BasicTriList device, SerialJoin join, string value)
        {
            try
            {
                device.StringInput[join.Number].StringValue = value;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("SerialJoin.SetJoin() exception caught." + ex.Message);
            }
        }

        public static string GetJoin(BasicTriList device, SerialJoin join)
        {
            try
            {
                return device.BooleanInput[join.Number].StringValue;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("SerialJoin.GetJoin() exception caught." + ex.Message);
                return String.Empty;
            }
        }
    }
}
