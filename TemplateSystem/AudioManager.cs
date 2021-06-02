using ATC.Framework;
using System;

namespace TemplateSystem
{
    public class AudioManager : SystemComponent
    {
        #region Fields
        private readonly ControlSystem controlSystem;
        private LevelRange[] levelRanges;
        #endregion

        #region Constants
        // source audio
        public const int SourceAudioMax = 100;
        public const int SourceAudioMin = 0;
        public const int SourceAudioDefault = 50;
        public const int SourceAudioIncrement = 5;

        // microphones
        public const int MicLevelMax = 12;
        public const int MicLevelMin = 0;
        public const int MicLevelDefault = 6;
        public const int MicLevelIncrement = 1;
        #endregion

        #region Constructor
        public AudioManager(ControlSystem controlSystem)
        {
            try
            {
                this.controlSystem = controlSystem;

                // create level range objects
                levelRanges = new LevelRange[] {
                    new LevelRange(SourceAudioMin, SourceAudioMax, SourceAudioIncrement),
                    new LevelRange(MicLevelMin, MicLevelMax, MicLevelIncrement),
                    new LevelRange(MicLevelMin, MicLevelMax, MicLevelIncrement)
                };

                // add callbacks for level range objects
                foreach (var levelRange in levelRanges)
                    levelRange.EventHandler += new EventHandler<LevelRangeEventArgs>(LevelRangeEventHandler);
            }
            catch (Exception ex)
            {
                TraceException("AudioManager() exception caught.", ex);
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Set default levels for all ranges.
        /// </summary>
        public void SetDefaults()
        {
            SetLevel(AudioChannel.SourceAudio, SourceAudioDefault);
            SetMute(AudioChannel.SourceAudio, false);

            SetLevel(AudioChannel.HandheldMic, MicLevelDefault);
            SetMute(AudioChannel.HandheldMic, false);

            SetLevel(AudioChannel.LapelMic, MicLevelDefault);
            SetMute(AudioChannel.LapelMic, false);
        }

        /// <summary>
        /// Get the current level of the specified audio channel.
        /// </summary>
        /// <param name="channel">The channel to get</param>
        /// <returns>The value of the current channel</returns>
        public int GetLevel(AudioChannel channel)
        {
            try
            {
                var index = GetIndex(channel);
                return levelRanges[index].Level;
            }
            catch (Exception ex)
            {
                TraceException("GetLevel() exception caught.", ex);
                return 0;
            }
        }

        /// <summary>
        /// Set the level of the specified audio channel
        /// </summary>
        /// <param name="channel">The channel to set</param>
        /// <param name="value">The value to set</param>
        public void SetLevel(AudioChannel channel, int value)
        {
            try
            {
                var index = GetIndex(channel);
                levelRanges[index].Level = value;
            }
            catch (Exception ex)
            {
                TraceException("SetLevel() exception caught.", ex);
            }
        }

        public void IncrementLevel(AudioChannel channel)
        {
            var index = GetIndex(channel);
            levelRanges[index].Increment();
        }

        public void DecrementLevel(AudioChannel channel)
        {
            var index = GetIndex(channel);
            levelRanges[index].Decrement();
        }

        /// <summary>
        /// Get the current level of the specified audio channel.
        /// </summary>
        /// <param name="channel">The channel to get</param>
        /// <returns>The value of the current channel</returns>
        public bool GetMute(AudioChannel channel)
        {
            try
            {
                var index = (int)channel;
                return levelRanges[index].Mute;
            }
            catch (Exception ex)
            {
                TraceException("GetMute() exception caught.", ex);
                return false;
            }
        }

        /// <summary>
        /// Set the mute state of the specified audio channel
        /// </summary>
        /// <param name="channel">The channel to set</param>
        /// <param name="value">The value to set</param>
        public void SetMute(AudioChannel channel, bool value)
        {
            try
            {
                var index = (int)channel;
                levelRanges[index].Mute = value;
            }
            catch (Exception ex)
            {
                TraceException("SetMute() exception caught.", ex);
            }
        }

        public void ToggleMute(AudioChannel channel)
        {
            var index = (int)channel;
            levelRanges[index].Mute = !levelRanges[index].Mute;
        }
        #endregion

        #region Private methods
        private int GetIndex(AudioChannel channel)
        {
            return (int)channel;
        }

        private AudioChannel GetAudioChannel(LevelRange levelRange)
        {
            if (levelRange == levelRanges[(int)AudioChannel.SourceAudio])
                return AudioChannel.SourceAudio;
            else if (levelRange == levelRanges[(int)AudioChannel.HandheldMic])
                return AudioChannel.HandheldMic;
            else
                return AudioChannel.LapelMic;
        }
        #endregion

        #region Events
        private void LevelRangeEventHandler(object sender, LevelRangeEventArgs e)
        {
            // raise public event
            if (AudioChannelUpdatedHandler != null)
            {
                var channel = GetAudioChannel((LevelRange)sender); // get channel from sender object
                AudioChannelUpdatedHandler(this, new AudioChannelUpdatedEventArgs(channel, e.Level, e.Mute));
            }
            else
                TraceWarning("LevelRangeEventHandler() event handler not defined.");
        }

        public event EventHandler<AudioChannelUpdatedEventArgs> AudioChannelUpdatedHandler;
        #endregion
    }

    public enum AudioChannel
    {
        SourceAudio,
        HandheldMic,
        LapelMic,
    }

    public class AudioChannelUpdatedEventArgs : EventArgs
    {
        public AudioChannel Channel { get; private set; }
        public int Level { get; private set; }
        public bool Mute { get; private set; }

        public AudioChannelUpdatedEventArgs(AudioChannel channel, int level, bool mute)
        {
            Channel = channel;
            Level = level;
            Mute = mute;
        }
    }
}
