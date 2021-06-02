using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ATC.Framework.Schedule
{
    public class Scheduler : SystemComponent
    {
        #region Fields
        private readonly Dictionary<string, EventData> events;
        private readonly string filePath;
        #endregion

        #region Properties
        /// <summary>
        /// Number of events defined.
        /// </summary>
        public int EventCount
        {
            get { return events.Count; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a Scheduler object using the default file path.
        /// </summary>
        public Scheduler()
            : this(string.Empty) { }

        /// <summary>
        /// Create a Scheduler object.
        /// </summary>
        /// <param name="filePath">The full path of the JSON configuration data file.</param>
        public Scheduler(string filePath)
        {
            // generate default file path
            if (string.IsNullOrEmpty(filePath))
                this.filePath = string.Format(@"{0}/User/schedulerData.json", Directory.GetApplicationRootDirectory());
            else
                this.filePath = filePath;

            // create storage for event data
            events = new Dictionary<string, EventData>();
        }
        #endregion

        #region Public methods
        public void AddEvent(string eventName)
        {
            var eventData = new EventData(eventName)
            {
                TraceEnabled = true,
                ComponentName = "EventData-" + eventName,
            };

            AddEvent(eventData);
        }

        /// <summary>
        /// Attempt to load event data from file.
        /// </summary>
        /// <returns>The number of events loaded.</returns>
        public int LoadData()
        {
            try
            {
                // clear any current items
                if (events.Count > 0)
                {
                    foreach (var item in events.Values)
                        item.DataUpdatedHandler -= DataUpdatedHandler;
                    events.Clear();
                }

                if (File.Exists(filePath))
                {
                    string json = File.ReadToEnd(filePath, Encoding.Default);
                    var loadedData = JsonConvert.DeserializeObject<EventData[]>(json);

                    // fill dictionary with loaded data
                    foreach (var item in loadedData)
                    {
                        item.TraceEnabled = true;
                        item.ComponentName = "EventData-" + item.Name;
                        AddEvent(item);
                    }

                    Trace(string.Format("LoadData() loaded: {0} events from file.", events.Count));

                    return events.Count;
                }
                else
                {
                    TraceError(string.Format("LoadData() file: {0} does not exist.", filePath));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                TraceException("LoadData() exception caught.", ex);
                return 0;
            }
        }

        /// <summary>
        /// Save loaded event data to file.
        /// </summary>
        public void SaveData()
        {
            try
            {
                // convert event data to array
                EventData[] dataArray = new EventData[events.Values.Count];
                events.Values.CopyTo(dataArray, 0);

                // serialize data array
                string json = JsonConvert.SerializeObject(dataArray, Formatting.Indented);

                // save json to file
                lock (filePath)
                {
                    FileStream fileStream = File.Create(filePath);
                    fileStream.Write(json, Encoding.Default);
                    var length = fileStream.Length;
                    fileStream.Close();
                    fileStream.Dispose();

                    Trace(string.Format("SaveData() saved data succesfully. File size {0} bytes.", length));
                }
            }
            catch (Exception ex)
            {
                TraceException("SaveData() exception caught.", ex);
            }
        }

        /// <summary>
        /// Returns true if event is enabled.
        /// </summary>
        /// <param name="eventName">The event to query.</param>
        public bool IsEnabled(string eventName)
        {
            return events[eventName].Enabled;
        }

        public void SetEnabled(string eventName, bool value)
        {
            Trace(string.Format("SetEnabled() setting event: {0} enabled to value: {1}", eventName, value));
            events[eventName].Enabled = value;
        }

        public void ToggleEnabled(string eventName)
        {
            bool value = !IsEnabled(eventName);
            SetEnabled(eventName, value);
        }

        public bool GetDayEnabled(string eventName, DayOfWeek day)
        {
            return events[eventName].GetDayEnabled(day);
        }

        public void SetDayEnabled(string eventName, DayOfWeek day, bool value)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ApplicationException("Event name is null or empty.");

            Trace(string.Format("SetDayEnabled() setting event: {0}, day: {1}, to value: {2}", eventName, day, value));
            events[eventName].SetDayEnabled(day, value);
        }

        public void ToggleDayEnabled(string eventName, DayOfWeek day)
        {
            bool value = !GetDayEnabled(eventName, day);
            SetDayEnabled(eventName, day, value);
        }

        public int GetHour(string eventName)
        {
            return events[eventName].Hour;
        }

        public void SetHour(string eventName, int value)
        {
            Trace(string.Format("SetHour() setting event: {0} hour to value: {1}", eventName, value));
            events[eventName].Hour = value;
        }

        /// <summary>
        /// Increase event hour by 1.
        /// </summary>
        /// <param name="eventName">The event to update.</param>
        public void IncrementHour(string eventName)
        {
            int currentValue = GetHour(eventName);
            int newValue;
            if (currentValue + 1 > EventData.HourMax)
                newValue = EventData.HourMin;
            else
                newValue = currentValue + 1;

            SetHour(eventName, newValue);
        }

        /// <summary>
        /// Decrease event hour by 1.
        /// </summary>
        /// <param name="eventName">The event to update.</param>
        public void DecrementHour(string eventName)
        {
            int currentValue = GetHour(eventName);
            int newValue;
            if (currentValue - 1 < EventData.HourMin)
                newValue = EventData.HourMax;
            else
                newValue = currentValue - 1;

            SetHour(eventName, newValue);
        }

        public int GetMinute(string eventName)
        {
            return events[eventName].Minute;
        }

        public void SetMinute(string eventName, int value)
        {
            Trace(string.Format("SetHour() setting event: {0} minute to value: {1}", eventName, value));
            events[eventName].Minute = value;
        }

        /// <summary>
        /// Increase event minute by 1.
        /// </summary>
        /// <param name="eventName">The event to update.</param>
        public void IncrementMinute(string eventName)
        {
            int currentValue = GetMinute(eventName);
            int newValue;
            if (currentValue + 1 > EventData.MinuteMax)
            {
                newValue = EventData.MinuteMin;
                IncrementHour(eventName);
            }
            else
                newValue = currentValue + 1;

            SetMinute(eventName, newValue);
        }

        /// <summary>
        /// Decrease event minute by 1.
        /// </summary>
        /// <param name="eventName">The event to update.</param>
        public void DecrementMinute(string eventName)
        {
            int currentValue = GetMinute(eventName);
            int newValue;
            if (currentValue - 1 < EventData.MinuteMin)
            {
                newValue = EventData.MinuteMax;
                DecrementHour(eventName);
            }
            else
                newValue = currentValue - 1;

            SetMinute(eventName, newValue);
        }
        #endregion

        #region Private methods
        private void AddEvent(EventData data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            events.Add(data.Name, data);
            data.DataUpdatedHandler += new EventHandler<EventDataEventArgs>(LocalDataUpdatedHandler);
            data.EventDueHandler += new EventHandler<EventDataEventArgs>(LocalEventDueHandler);

            Trace("AddEvent() added event: " + data.Name);
        }
        #endregion

        #region Public events
        public event EventHandler<EventDataEventArgs> DataUpdatedHandler;
        public event EventHandler<EventDataEventArgs> EventDueHandler;
        #endregion

        #region Local object event handlers
        private void LocalDataUpdatedHandler(object sender, EventDataEventArgs e)
        {
            if (DataUpdatedHandler != null)
                DataUpdatedHandler(this, e);
            else
                TraceWarning("LocalDataUpdatedHandler() no data updated handler registered.");
        }

        private void LocalEventDueHandler(object sender, EventDataEventArgs e)
        {
            if (EventDueHandler != null)
                EventDueHandler(this, e);
            else
                TraceWarning("LocalEventDueHandler() no event due handler registered.");
        }
        #endregion
    }
}
