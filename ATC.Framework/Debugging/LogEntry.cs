using System;

namespace ATC.Framework.Debugging
{
    public class LogEntry
    {
        /// <summary>
        /// The name of the component that created the log entry.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// The type of the message.
        /// </summary>
        public Category Category { get; set; }

        /// <summary>
        /// Timestamp for when the message was created.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// The message contents.
        /// </summary>
        public string Message { get; set; }

        public LogEntry()
        {
            DateTime = DateTime.Now;
        }


        public override string ToString()
        {
            return string.Format("[{0}] ({1}) {2}", Component, Category, Message);
        }
    }
}
