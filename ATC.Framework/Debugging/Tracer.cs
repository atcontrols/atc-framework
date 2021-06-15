using Crestron.SimplSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ATC.Framework.Debugging
{
    public interface ITracer
    {
        // properties
        bool Enabled { get; set; }
        string Name { get; set; }

        // methods
        void Trace(string message);
        void TraceLine(LineType lineType);
        void TraceInfo(string message);
        void TraceWarning(string message);
        void TraceError(string message);
        void TraceException(string message, Exception ex);
        void TraceException(Exception ex, string methodName, string message = null);
    }

    public class Tracer : ITracer
    {
        #region Fields

        private static readonly List<LogEntry> logEntries = new List<LogEntry>();
        private static int _consoleWidth = 80;
        private static int _memoryLogEntryMaximum = 4000;
        private static bool _outputCrestronConsole = true;

        #endregion

        #region Properties

        public bool Enabled { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Set to true to output to the Crestron Console (enabled by default).
        /// </summary>
        public static bool OutputCrestronConsole
        {
            get { return _outputCrestronConsole; }
            set { _outputCrestronConsole = value; }
        }

        /// <summary>
        /// Set to true to output trace statements to the processor's internal error log.
        /// </summary>
        public static bool OutputErrorLog { get; set; }

        /// <summary>
        /// Set to true to output exceptions full stack trace to the console.
        /// </summary>
        public static bool OutputStackTrace { get; set; }

        /// <summary>
        /// Set to true to output log items to internal list.
        /// </summary>
        public static bool OutputMemoryLog { get; set; }

        /// <summary>
        /// The maximum number of entries the memory log can hold.
        /// </summary>
        public static int MemoryLogEntryMaximum
        {
            get { return _memoryLogEntryMaximum; }
            set
            {
                if (value > 0)
                    _memoryLogEntryMaximum = value;
            }
        }

        /// <summary>
        /// How wide (in characters) the console is.
        /// </summary>
        public static int ConsoleWidth
        {
            get { return _consoleWidth; }
            set
            {
                if (value > 0)
                    _consoleWidth = value;
            }
        }

        /// <summary>
        /// Show the current time (when output to the Crestron Console).
        /// </summary>
        public static bool ShowCurrentTime { get; set; }

        #endregion

        #region Trace methods

        public void Trace(string message)
        {
            if (Enabled)
                OutputMessage(Name, message, Category.Default);
        }

        public void TraceLine(LineType lineType)
        {
            if (!Enabled)
                return;

            string line = string.Empty;

            switch (lineType)
            {
                case LineType.Starline:
                    for (int i = 0; i < ConsoleWidth; i++)
                        line += "*";
                    break;
                case LineType.Dashline:
                    for (int i = 0; i < ConsoleWidth; i++)
                        line += "-";
                    break;
            }

            OutputMessage(Name, line, Category.Raw);
        }

        public void TraceInfo(string message)
        {
            if (Enabled)
                OutputMessage(Name, message, Category.Info);
        }

        public void TraceWarning(string message)
        {
            if (Enabled)
                OutputMessage(Name, message, Category.Warning);
        }

        public void TraceError(string message)
        {
            if (Enabled)
                OutputMessage(Name, message, Category.Error);
        }

        public void TraceException(string message, Exception ex)
        {
            string outputMessage = string.Format("{0}\r\nException message: \"{1}\"", message, ex.Message);
            if (ex.InnerException != null)
                outputMessage += string.Format("\r\nInner exception message: \"{0}\"", ex.InnerException.Message);

            // exceptions are always outputted
            OutputMessage(Name, outputMessage, Category.Exception);

            if (OutputStackTrace)
            {
                PrintLine("Stack trace below:\r\n" + ex.StackTrace);
                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.StackTrace))
                    PrintLine("Inner exception stack trace below:\r\n" + ex.StackTrace);
            }

            if (OutputErrorLog)
                ErrorLog.Exception(message, ex);
        }

        public void TraceException(Exception ex, string methodName, string message = null)
        {
            string outputMessage = $"{methodName}() Type: {ex.GetType().Name}";

            // append message parameter
            if (!string.IsNullOrEmpty(message))
                outputMessage += $", Message: \"{message}\"";

            // append exception message
            outputMessage += $"\r\nException message: \"{ex.Message}\"";

            // append inner exception message (if it exists)
            if (ex.InnerException != null)
                outputMessage += $"\r\nInner exception - Type: {ex.InnerException.GetType().Name}, Message: \"{ex.InnerException.Message}\"";

            // exceptions are always outputted
            OutputMessage(Name, outputMessage, Category.Exception);

            if (OutputStackTrace)
            {
                PrintLine("Stack trace below:\r\n" + ex.StackTrace);
                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.StackTrace))
                    PrintLine("Inner exception stack trace below:\r\n" + ex.StackTrace);
            }

            if (OutputErrorLog)
                ErrorLog.Exception(message, ex);
        }

        #endregion

        #region Output methods

        /// <summary>
        /// Output message as-is directly to Crestron Console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void PrintLine(string message)
        {
            OutputMessage(string.Empty, message, Category.Raw);
        }

        /// <summary>
        /// Output message as-is directly to Crestron Console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Array of objects to convert to strings and log.</param>
        public static void PrintLine(string message, params object[] args)
        {
            try
            {
                string completeMessage = message;

                // concatenate all strings
                foreach (object o in args)
                    completeMessage = completeMessage + " " + o.ToString();

                PrintLine(completeMessage);
            }
            catch (Exception ex)
            {
                PrintLine("PrintLine() exception occurred: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends the message to all configured outputs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private static void OutputMessage(string componentName, string message, Category category)
        {
            try
            {
                if (string.IsNullOrEmpty(componentName))
                    componentName = "UnnamedComponent";

                StringBuilder sb = new StringBuilder();

                if (category == Category.Raw || category == Category.Line)
                    sb.Append(message); // raw messages bypass formatting
                else
                {
                    // current time
                    if (ShowCurrentTime)
                    {
                        DateTime dt = DateTime.Now;
                        sb.AppendFormat("{0:D2}:{1:D2}:{2:D2}.{3:D3} ", dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
                    }

                    // component name
                    sb.AppendFormat("[{0}] ", componentName);

                    // category
                    if (category != Category.Default)
                        sb.AppendFormat("({0}) ", category);

                    // message
                    sb.Append(message);
                }

                // output to crestron console
                if (OutputCrestronConsole)
                    CrestronConsole.PrintLine(sb.ToString());

                // output to memory log (if enabled);
                if (OutputMemoryLog)
                {
                    // ignore special line types
                    if (category == Category.Line)
                        return;

                    // ensure list stays below maximum
                    if (logEntries.Count >= MemoryLogEntryMaximum)
                    {
                        if (OutputErrorLog)
                            ErrorLog.Warn("The memory log is at or over capacity. Clear log entries to resume storing log entries.");
                        return;
                    }

                    // create new log entry
                    var logEntry = new LogEntry()
                    {
                        Component = componentName,
                        Category = category,
                        DateTime = DateTime.Now,
                        Message = message,
                    };

                    logEntries.Add(logEntry);
                }

                // output to error log (exceptions handled in TraceException method)
                if (OutputErrorLog)
                {
                    switch (category)
                    {
                        case Category.Info: ErrorLog.Notice(message); break;
                        case Category.Warning: ErrorLog.Warn(message); break;
                        case Category.Error: ErrorLog.Error(message); break;
                    }
                }

                // output to DebugConsole (if connected)
                if (DebugConsoleClient.Connected)
                    DebugConsoleClient.Send(category, componentName, message);
            }
            catch (Exception ex)
            {
                PrintLine("TraceInfo() exception occurred: " + ex.Message);

                if (OutputErrorLog)
                    ErrorLog.Exception("TraceInfo() exception occurred.", ex);
            }
        }

        #endregion

        #region Memory log methods

        public static LogEntry[] GetLogEntries()
        {
            return logEntries.ToArray();
        }

        public static int GetLogEntryCount()
        {
            return logEntries.Count;
        }

        public static void ClearLogEntries()
        {
            logEntries.Clear();
        }

        #endregion
    }
}
