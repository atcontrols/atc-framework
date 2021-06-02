using System;

namespace ATC.Framework
{
    /// <summary>
    /// DateTime workaround for missing millisecond in .NET 3.5 CF DateTime class.
    /// </summary>
    public static class DateTimeMilliseconds
    {
        private static readonly DateTime startDate = DateTime.Now;
        private static readonly int startCount = Environment.TickCount;

        public static DateTime Now
        {
            get
            {
                return startDate.AddMilliseconds(Environment.TickCount - startCount);
            }
        }
    }
}
