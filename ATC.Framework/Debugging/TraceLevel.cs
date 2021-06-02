namespace ATC.Framework.Debugging
{
    public enum TraceLevel
    {
        /// <summary>
        /// Tracing is disabled and will not output anywhere.
        /// </summary>
        Disabled,

        /// <summary>
        /// Standard level of Tracing.
        /// </summary>
        Standard,

        /// <summary>
        /// Extra level of Tracing (useful in debugging situations).
        /// </summary>
        Extended,
    }
}
