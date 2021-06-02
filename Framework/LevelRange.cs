using System;

namespace ATC.Framework
{
    public class LevelRange : SystemComponent
    {
        #region Fields
        private int level;
        private bool mute;
        #endregion

        #region Constants
        private const int MinimumDefault = 0;
        private const int MaximumDefault = 100;
        private const int StepDefault = 1;
        #endregion

        #region Properties
        public int Minimum { get; private set; }
        public int Maximum { get; private set; }
        public int Step { get; private set; }
        public int Level
        {
            get { return level; }
            set { SetLevel(value, true); }
        }
        public bool Mute
        {
            get { return mute; }
            set { SetMute(value, true); }
        }
        #endregion

        #region Events
        public event EventHandler<LevelRangeEventArgs> EventHandler;

        private void RaiseEvent()
        {
            if (EventHandler != null)
                EventHandler(this, new LevelRangeEventArgs(level, mute));
            else
                TraceWarning("RaiseEvent() this is no event handler defined.");
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create new LevelRange object with all default properties.
        /// </summary>
        public LevelRange()
            : this(MinimumDefault, MaximumDefault, StepDefault, MinimumDefault, false) { }

        /// <summary>
        /// Create new LevelRange object with specific properties.
        /// </summary>
        /// <param name="minimum">The minimum value of the range.</param>
        /// <param name="maximum">The maximum value of the range.</param>
        /// <param name="step">How much each invocation of Increment() or Decrement() should adjust the level by.</param>
        public LevelRange(int minimum, int maximum, int step)
            : this(minimum, maximum, step, minimum, false) { }

        /// <summary>
        /// Create new LevelRange object with all propeties specified.
        /// </summary>
        /// <param name="minimum">The minimum value of the range.</param>
        /// <param name="maximum">The maximum value of the range.</param>
        /// <param name="step">How much each invocation of Increment() or Decrement() should adjust the level by.</param>
        /// <param name="initialLevel">The starting level (must be within minimum and maximum).</param>
        /// <param name="traceEnabled">Set to true to enable debugging.</param>
        public LevelRange(int minimum, int maximum, int step, int initialLevel, bool traceEnabled)
        {
            TraceEnabled = traceEnabled;

            // validate minimum and maximum
            if (minimum >= maximum)
            {
                TraceWarning("Constructor() minimum is greater than or equal to maximum.");
                minimum = MinimumDefault;
                maximum = MaximumDefault;
            }

            // validate step
            if (step == 0)
            {
                TraceWarning("Constructor() step should not be 0.");
                step = StepDefault;
            }

            // validate initalLevel
            if (initialLevel > maximum || initialLevel < minimum)
            {
                TraceWarning("Constructor() initial level does not fall between maximum and minimum.");
                initialLevel = minimum;
            }

            Minimum = minimum;
            Maximum = maximum;
            Step = step;
            Level = initialLevel;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Increase the value of Level by the value of Step (will raise an event).
        /// </summary>
        public void Increment()
        {
            SetLevel(level + Step, true);
        }

        /// <summary>
        /// Decrease the value of Level by the value of Step (will raise an event).
        /// </summary>
        public void Decrement()
        {
            SetLevel(level - Step, true);
        }

        /// <summary>
        /// Invert the mute state (will raise an event).
        /// </summary>
        public void MuteToggle()
        {
            SetMute(!Mute);
        }

        /// <summary>
        /// Sets the internal level to the specified value if it is within bounds (will raise an event).
        /// </summary>
        /// <param name="value">The new value to set</param>
        public void SetLevel(int value)
        {
            SetLevel(value, true);
        }

        /// <summary>
        /// Sets the internal level to the specified value if it is within bounds.
        /// </summary>
        /// <param name="value">The new value to set</param>
        /// <param name="raiseEvent">Set to true to raise an event callback on change.</param>
        public void SetLevel(int value, bool raiseEvent)
        {
            int newLevel;

            // check bounds
            if (value > Maximum)
                newLevel = Maximum;
            else if (value < Minimum)
                newLevel = Minimum;
            else
                newLevel = value;

            // update variable
            if (level != newLevel)
            {
                level = newLevel;
                Trace("SetLevel() level set to: " + newLevel);

                if (raiseEvent)
                    RaiseEvent();
            }
        }

        /// <summary>
        /// Uses RangeScaler to scale current level to specified minimum and maximum values.
        /// </summary>
        /// <param name="min">The minimum of the range to scale to.</param>
        /// <param name="max">The maximum of the range to scale to.</param>
        /// <returns>The scaled value.</returns>
        public int GetLevelScaled(int min, int max)
        {
            return Utilities.RangeScaler(Level, Maximum, Minimum, max, min);
        }

        /// <summary>
        /// Set Level using scaled maximum and minimum settings (will raise an event).
        /// </summary>
        /// <param name="valueToScale">The value to scale.</param>
        /// <param name="initialMin">The minimum of the value to scale.</param>
        /// <param name="initialMax">The maximum of the value to scale.</param>
        public void SetLevelScaled(int valueToScale, int initialMin, int initialMax)
        {
            SetLevelScaled(valueToScale, initialMin, initialMax, true);
        }

        /// <summary>
        /// Set Level using scaled maximum and minimum settings.
        /// </summary>
        /// <param name="valueToScale">The value to scale.</param>
        /// <param name="initialMin">The minimum of the value to scale.</param>
        /// <param name="initialMax">The maximum of the value to scale.</param>
        /// <param name="raiseEvent">Set to true to raise an event, false if no event is desired.</param>
        public void SetLevelScaled(int valueToScale, int initialMin, int initialMax, bool raiseEvent)
        {
            int scaledValue = Utilities.RangeScaler(valueToScale, initialMax, initialMin, Maximum, Minimum);
            SetLevel(scaledValue, raiseEvent);
        }

        /// <summary>
        /// Set the Mute property to the specified value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetMute(bool value)
        {
            SetMute(value, true);
        }

        /// <summary>
        /// Set the Mute property to the specified value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="raiseEvent">Set to true to raise an event, false if no event is desired.</param>
        public void SetMute(bool value, bool raiseEvent)
        {
            if (mute != value)
            {
                mute = value;
                Trace("SetMute() level set to: " + value);

                if (raiseEvent)
                    RaiseEvent();
            }
        }
        #endregion
    }

    public class LevelRangeEventArgs : EventArgs
    {
        public int Level { get; private set; }
        public bool Mute { get; private set; }

        public LevelRangeEventArgs(int level, bool mute)
        {
            Level = level;
            Mute = mute;
        }
    }
}
