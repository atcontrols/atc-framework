using Crestron.SimplSharp;
using Newtonsoft.Json;
using System;

namespace ATC.Framework.Schedule
{
    internal class EventData : SystemComponent
    {
        #region Fields
        private int _hour, _minute;
        private bool _enabled;
        private readonly bool[] days;
        private CTimer timer;
        #endregion

        #region Constants
        internal const int HourMin = 0;
        internal const int HourMax = 23;
        internal const int MinuteMin = 0;
        internal const int MinuteMax = 59;
        private const int NumberOfDays = 7;
        #endregion

        #region Properties
        /// <summary>
        /// The name of the event.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Whether or not this event is enabled.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    RaiseDataUpdatedEvent();
                    SetTimer();
                }
            }
        }

        /// <summary>
        /// The hour on which this event should fire (24 hour format).
        /// </summary>
        public int Hour
        {
            get { return _hour; }
            set
            {
                if (value < HourMin || value > HourMax)
                    throw new ArgumentOutOfRangeException("value", string.Format("Hour needs to be between {0} and {1}", HourMin, HourMax));

                if (_hour != value)
                {
                    _hour = value;
                    RaiseDataUpdatedEvent();
                    SetTimer();
                }
            }
        }

        /// <summary>
        /// The minute on which this event should fire.
        /// </summary>
        public int Minute
        {
            get { return _minute; }
            set
            {
                if (value < MinuteMin || value > MinuteMax)
                    throw new ArgumentOutOfRangeException("value", string.Format("Minute needs to be between {0} and {1}", MinuteMin, MinuteMax));

                if (_minute != value)
                {
                    _minute = value;
                    RaiseDataUpdatedEvent();
                    SetTimer();
                }
            }
        }
        #endregion

        #region Constructor
        public EventData(string name)
            : this(name, false, null, HourMin, MinuteMin) { }

        [JsonConstructor]
        public EventData(string name, bool enabled, bool[] days, int hour, int minute)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty.", "name");
            else if (days == null)
                days = new bool[NumberOfDays];
            else if (days.Length != NumberOfDays)
                throw new ArgumentException("Invalid length for array", "daysEnabled");
            else if (hour < HourMin || hour > HourMax)
                throw new ArgumentOutOfRangeException("hour", string.Format("Hour needs to be between {0} and {1}", HourMin, HourMax));
            else if (minute < MinuteMin || hour > MinuteMax)
                throw new ArgumentOutOfRangeException("minute", string.Format("Minute needs to be between {0} and {1}", MinuteMin, MinuteMax));

            // assign days array
            this.days = days;

            // set properties
            Name = name;
            Hour = hour;
            Minute = minute;
            Enabled = enabled; // this property should be set last to create timer after other data has been set
        }
        #endregion

        #region Public methods
        public bool GetDayEnabled(DayOfWeek day)
        {
            var index = (int)day;
            return days[index];
        }

        public void SetDayEnabled(DayOfWeek day, bool value)
        {
            var index = (int)day;
            if (days[index] != value)
            {
                days[index] = value;
                RaiseDataUpdatedEvent();
                SetTimer();
            }
        }
        #endregion

        #region Private methods
        private void SetTimer()
        {
            // clean up any existing timer
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
                Trace("SetTimer() stopped existing timer.");
            }

            // create new timer
            if (Enabled)
            {
                // calculate due time
                DateTime eventTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Hour, Minute, 0);
                long dueTime = (eventTime - DateTime.Now).Ticks / TimeSpan.TicksPerMillisecond;
                if (dueTime < 0)
                    dueTime += 86400 * 1000; // due time occurs in the past, so add another day to it

                // create timer
                Trace(string.Format("SetTimer() due time calculated as: {0}ms, ({1} seconds)", dueTime, dueTime / 1000));
                timer = new CTimer(TimerCallback, dueTime);
            }
        }

        private void TimerCallback(object o)
        {
            int todayIndex = (int)DateTime.Now.DayOfWeek;
            bool todayEnabled = days[todayIndex];

            if (todayEnabled)
            {
                Trace("TimerCallback() event due, raising event.");
                RaiseEventDueEvent();
            }
            else
                Trace("TimerCallback() called but event is not enabled today.");

            // start another timer for tomorrow's event
            SetTimer();
        }
        #endregion

        #region Events
        private void RaiseDataUpdatedEvent()
        {
            if (DataUpdatedHandler != null)
                DataUpdatedHandler(this, new EventDataEventArgs(Name));
        }

        private void RaiseEventDueEvent()
        {
            if (EventDueHandler != null)
                EventDueHandler(this, new EventDataEventArgs(Name));
        }

        internal event EventHandler<EventDataEventArgs> DataUpdatedHandler;

        internal event EventHandler<EventDataEventArgs> EventDueHandler;
        #endregion
    }

    public class EventDataEventArgs : EventArgs
    {
        public string Name { get; private set; }

        public EventDataEventArgs(string name)
        {
            Name = name;
        }
    }
}
