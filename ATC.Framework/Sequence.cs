using Crestron.SimplSharp;
using System;
using System.Collections.Generic;

namespace ATC.Framework
{
    public class Sequence : SystemComponent
    {
        #region Fields

        private CTimer eventTimer, progressTimer;
        private readonly List<SequenceEventArgs> events = new List<SequenceEventArgs>();
        private int currentEventId, progressTimerStep;

        #endregion

        #region Constants

        private const long ProgressUpdateRateDefault = 100;

        #endregion

        #region Events

        public event EventHandler<SequenceEventArgs> EventCallback;
        public event EventHandler<SequenceProgressEventArgs> ProgressCallback;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the sequence is currently running.
        /// </summary>
        public bool IsActive
        {
            get { return eventTimer != null; }
        }

        /// <summary>
        /// Returns total duration of sequence.
        /// </summary>
        public long TotalTime
        {
            get
            {
                if (events == null || events.Count == 0)
                    return 0;
                else
                    return events[events.Count - 1].Delay;
            }
        }

        /// <summary>
        /// How frequently (in milliseconds) progress updates events will be raised.
        /// </summary>
        public long ProgressUpdateRate { get; set; }

        #endregion

        #region Constructor

        public Sequence()
        {
            ProgressUpdateRate = ProgressUpdateRateDefault;
        }

        #endregion

        #region Public methods
        public bool Start()
        {
            try
            {
                if (events.Count == 0)
                {
                    TraceError("Start() cannot start as there no events added.");
                    return false;
                }

                // stop any active event timer
                if (eventTimer != null)
                {
                    eventTimer.Stop();
                    eventTimer.Dispose();
                    eventTimer = null;
                }

                // create timer for first event
                currentEventId = 0;
                long dueTime = events[currentEventId].Delay;
                eventTimer = new CTimer(TimerCallback, dueTime);

                // stop any active progress timer
                if (progressTimer != null)
                {
                    progressTimer.Stop();
                    progressTimer.Dispose();
                    progressTimer = null;
                }

                // create progress timer
                if (TotalTime > 0 && ProgressCallback != null)
                {
                    progressTimerStep = 0;
                    progressTimer = new CTimer(ProgressUpdate, null, 0, ProgressUpdateRate);
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceException("Start() exception caught.", ex);
                return false;
            }
        }

        public int AddEvent(string name, long delay)
        {
            try
            {
                // check that event doesnt occur before previously defined event
                if (delay < TotalTime)
                {
                    TraceError("AddEvent() cannot add an event that occurs before an already added event.");
                    return -1;
                }

                // add the event
                int id = events.Count;
                SequenceEventArgs sequenceEvent = new SequenceEventArgs(id, name, delay);
                events.Add(sequenceEvent);

                Trace(String.Format("AddEvent() added event. Id: {0}, name: \"{1}\" with delay: {2}.", id, name, delay));

                return id;
            }
            catch (Exception ex)
            {
                TraceException("AddEvent() exception caught.", ex);
                return -1;
            }
        }
        #endregion

        #region Internal methods
        private void TimerCallback(object o)
        {
            try
            {
                // get current event
                var currentEvent = events[currentEventId];
                if (currentEvent == null)
                {
                    TraceError("TimerCallback() current event is null. Cannot procede.");
                    return;
                }

                Trace(string.Format("TimerCallback() called for event. Id: {0}, name: \"{1}\"", currentEvent.Id, currentEvent.Name));
                RaiseEvent(currentEvent);

                // cleanup timer
                if (eventTimer != null)
                {
                    eventTimer.Stop();
                    eventTimer.Dispose();
                    eventTimer = null;
                }

                // check if another timer is needed
                if (currentEventId + 1 < events.Count)
                {
                    var nextEvent = events[++currentEventId];
                    var dueTime = nextEvent.Delay - currentEvent.Delay;
                    eventTimer = new CTimer(TimerCallback, dueTime);
                }
            }
            catch (Exception ex)
            {
                TraceException("TimerCallback() exception caught.", ex);
                return;
            }
        }

        private void ProgressUpdate(object o)
        {
            long elapsedTime = ProgressUpdateRate * progressTimerStep++;
            SequenceProgressEventArgs pe = new SequenceProgressEventArgs(elapsedTime, TotalTime);
            RaiseEvent(pe);

            // stop timer when total duration reached
            if (elapsedTime >= TotalTime)
            {
                progressTimer.Stop();
                progressTimer.Dispose();
                progressTimer = null;
            }
        }

        private void RaiseEvent(SequenceEventArgs args)
        {
            try
            {
                if (EventCallback != null)
                    EventCallback(this, args);
                else
                    TraceError("RaiseEvent() event callback method is null.");
            }
            catch (Exception ex)
            {
                TraceException("RaiseEvent() sequence event - exception caught.", ex);
            }
        }

        private void RaiseEvent(SequenceProgressEventArgs args)
        {
            try
            {
                if (ProgressCallback != null)
                    ProgressCallback(this, args);
                else
                    TraceError("RaiseEvent() progress callback method is null.");
            }
            catch (Exception ex)
            {
                TraceException("RaiseEvent() progress event - exception caught.", ex);
            }
        }
        #endregion
    }

    public class SequenceEventArgs : EventArgs
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public long Delay { get; private set; }

        public SequenceEventArgs(int id, string name, long delay)
        {
            Id = id;
            Name = name;
            Delay = delay;
        }
    }

    public class SequenceProgressEventArgs : EventArgs
    {
        /// <summary>
        /// How many milliseconds have passed since sequence started.
        /// </summary>
        public long ElapsedTime { get; private set; }

        /// <summary>
        /// How many milliseconds the sequence has in total duration.
        /// </summary>
        public long TotalTime { get; private set; }

        /// <summary>
        /// Percentage of progress (current / total)
        /// </summary>
        public int Percentage { get; private set; }

        /// <summary>
        /// Percentage of progress (current / total) converted to Uint16 limits.
        /// </summary>
        public ushort Uint16Percentage { get; private set; }

        public SequenceProgressEventArgs(long elapsed, long total)
        {
            ElapsedTime = elapsed;
            TotalTime = total;
            Percentage = (int)Utilities.RangeScaler(elapsed, total, 0, 100, 0);
            Uint16Percentage = (ushort)Utilities.RangeScaler(elapsed, total, 0, UInt16.MaxValue, UInt16.MinValue);
        }
    }
}
