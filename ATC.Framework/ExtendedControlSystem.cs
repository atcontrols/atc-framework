using ATC.Framework.Debugging;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro;
using System;

namespace ATC.Framework
{
    public abstract class ExtendedControlSystem : CrestronControlSystem, ISystemComponent
    {
        #region Fields

        private readonly Tracer tracer;
        private readonly Sequence initSequence;

        #endregion

        #region Properties

        public bool TraceEnabled
        {
            get { return tracer.Enabled; }
            set { tracer.Enabled = value; }
        }

        public string ComponentName
        {
            get { return tracer.Name; }
            set { tracer.Name = value; }
        }

        public TraceLevel TraceLevel { get; set; }


        /// <summary>
        /// The ATC Framework version extracted from the assembly.
        /// </summary>
        public string FrameworkVersion { get; private set; }

        #endregion

        #region Constructor and initialization

        public ExtendedControlSystem()
            : base()
        {
            tracer = new Tracer() { Name = "ControlSystem" };

            // enable tracing by default
            TraceEnabled = true;

            initSequence = new Sequence();
            initSequence.EventCallback += new EventHandler<SequenceEventArgs>(InitSequenceCallback);

            // add this control system to component cache
            SystemComponent.AddComponent(this);
        }

        public override void InitializeSystem()
        {
            // get running program information
            AssemblyName asn = Assembly.GetExecutingAssembly().GetName();
            FrameworkVersion = string.Format("{0}.{1}.{2}.{3}", asn.Version.Major, asn.Version.Minor, asn.Version.Build, asn.Version.Revision);

            Trace(string.Format("InitializeSystem() ATC Framework version: {0}", FrameworkVersion));

            initSequence.Start();
        }

        protected virtual void InitSequenceCallback(object sender, SequenceEventArgs e) { }

        #endregion

        #region Protected methods

        protected void AddInitEvent(string name, long delay)
        {
            initSequence.AddEvent(name, delay);
        }

        #endregion

        #region Trace methods

        /// <summary>
        /// Outputs the specified message to the console.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void Trace(string message)
        {
            tracer.Trace(message);
        }

        /// <summary>
        /// Outputs the specified message to the console (if specified level is greater than or equal to component TraceLevel)
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The level at which to log</param>
        protected void Trace(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.Trace(message);
        }

        /// <summary>
        /// Output a full line of the specified type to the console. Useful for formattting.
        /// </summary>
        /// <param name="lineType"></param>
        protected void TraceLine(LineType lineType)
        {
            tracer.TraceLine(lineType);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceInfo(string message)
        {
            tracer.TraceInfo(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level at which to log.</param>
        protected void TraceInfo(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceInfo(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceWarning(string message)
        {
            tracer.TraceWarning(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level at which to log.</param>
        protected void TraceWarning(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceWarning(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceError(string message)
        {
            tracer.TraceError(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level at which to log.</param>
        protected void TraceError(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceError(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="methodName">The name of the method in which the exception occurred.</param>
        /// <param name="ex">The exception to log.</param>
        protected void TraceException(string methodName, Exception ex)
        {
            string message = $"{methodName}() exception type: {ex.GetType()}";
            tracer.TraceException(message, ex);
        }

        #endregion
    }
}
