using ATC.Framework;
using ATC.Framework.UserInterface;
using System;

namespace TemplateSystem.UserInterface
{
    public class AudioUserInterface : UserInterfaceComponent
    {
        #region Fields

        private readonly AudioManager audioManager;

        #endregion

        #region Join definitions
        // digital joins
        private readonly DigitalJoin VolumeUpButton = new DigitalJoin(ProjectJoin.VolumeUpButton);
        private readonly DigitalJoin VolumeDownButton = new DigitalJoin(ProjectJoin.VolumeDownButton);
        private readonly DigitalJoin VolumeMuteButton = new DigitalJoin(ProjectJoin.VolumeMuteButton);

        // analog joins
        private readonly AnalogJoin VolumeBar = new AnalogJoin(ProjectJoin.VolumeBar);
        private readonly AnalogJoin HandheldMicBar = new AnalogJoin(ProjectJoin.HandheldMicBar);
        private readonly AnalogJoin LapelMicBar = new AnalogJoin(ProjectJoin.LapelMicBar);
        #endregion

        #region Constants
        // mic button smartobject indices
        private const int MicUpIndex = 1;
        private const int MicMuteIndex = 2;
        private const int MicDownIndex = 3;
        #endregion

        #region Initialization

        public AudioUserInterface()
        {
            audioManager = SystemComponent.GetComponent<AudioManager>();
        }

        protected override void Initialize()
        {
            // add joins to be handled by this component
            AddJoinListener(VolumeUpButton, 200, 100, VolumeButtonHandler);
            AddJoinListener(VolumeDownButton, 200, 100, VolumeButtonHandler);
            AddJoinListener(VolumeMuteButton, VolumeButtonHandler);
            AddJoinListener(VolumeBar, VolumeBarHandler);

            // add smart objects ids
            AddSmartObjectListener(ProjectJoin.HandheldMicSmartObjectId);
            AddSmartObjectListener(ProjectJoin.LapelMicSmartObjectId);

            // add event callbacks for audio manager
            audioManager.AudioChannelUpdatedHandler += new EventHandler<AudioChannelUpdatedEventArgs>(AudioChannelUpdatedHandler);
        }
        #endregion

        #region Panel event handlers

        /// <summary>
        /// Handles volume up, down and mute buttons.
        /// </summary>
        /// <param name="join"></param>
        private void VolumeButtonHandler(DigitalJoin join)
        {
            if (join.Value == DigitalJoinState.Pressed)
            {
                switch (join.Number)
                {
                    case ProjectJoin.VolumeUpButton: audioManager.IncrementLevel(AudioChannel.SourceAudio); break;
                    case ProjectJoin.VolumeDownButton: audioManager.DecrementLevel(AudioChannel.SourceAudio); break;
                    case ProjectJoin.VolumeMuteButton: audioManager.ToggleMute(AudioChannel.SourceAudio); break;
                }
            }
            else if (join.Value == DigitalJoinState.Held)
            {
                switch (join.Number)
                {
                    case ProjectJoin.VolumeUpButton: audioManager.IncrementLevel(AudioChannel.SourceAudio); break;
                    case ProjectJoin.VolumeDownButton: audioManager.DecrementLevel(AudioChannel.SourceAudio); break;
                }
            }
        }

        protected void VolumeBarHandler(AnalogJoin join)
        {
            int scaledValue = Utilities.RangeScaler(join.Value, UInt16.MaxValue, UInt16.MinValue, AudioManager.SourceAudioMax, AudioManager.SourceAudioMin);
            audioManager.SetLevel(AudioChannel.SourceAudio, scaledValue);
        }

        protected override void SmartObjectBoolHandler(uint smartObjectId, uint number, string name, DigitalJoinState state)
        {
            switch (smartObjectId)
            {
                case ProjectJoin.HandheldMicSmartObjectId:
                case ProjectJoin.LapelMicSmartObjectId:
                    MicButtonHandler(smartObjectId, number, state);
                    break;
            }
        }

        /// <summary>
        /// Handles microphone up, down and mute buttons.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="pressed"></param>
        private void MicButtonHandler(uint smartObjectID, uint number, DigitalJoinState state)
        {
            try
            {
                if (state == DigitalJoinState.Pressed)
                {
                    // determine audio channel
                    AudioChannel channel;
                    switch (smartObjectID)
                    {
                        case ProjectJoin.HandheldMicSmartObjectId: channel = AudioChannel.HandheldMic; break;
                        case ProjectJoin.LapelMicSmartObjectId: channel = AudioChannel.LapelMic; break;
                        default:
                            TraceError("MicButtonHandler() unhandled SmartObject ID: " + smartObjectID);
                            return;
                    }

                    // perform selected menu item action
                    switch (number)
                    {
                        case MicUpIndex: // level up
                            audioManager.IncrementLevel(channel);
                            break;
                        case MicMuteIndex: // mute
                            audioManager.ToggleMute(channel);
                            break;
                        case MicDownIndex: // level down
                            audioManager.DecrementLevel(channel);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException("MicButtonHandler() exception caught.", ex);
            }
        }
        #endregion

        #region Feedback update methods
        public override void Render()
        {
            try
            {
                RenderVolumeBar();
                RenderMicLevels();
            }
            catch (Exception ex)
            {
                TraceException("FeedbackUpdate() exception caught.", ex);
            }
        }

        private void RenderVolumeBar()
        {
            // scale volume from control system and update touch panel
            var volumeBarValue = (ushort)Utilities.RangeScaler(audioManager.GetLevel(AudioChannel.SourceAudio), AudioManager.SourceAudioMax, AudioManager.SourceAudioMin, UInt16.MaxValue, UInt16.MinValue);
            if (audioManager.GetMute(AudioChannel.SourceAudio))
                SetJoin(VolumeBar, UInt16.MinValue); // show minimum on bar when muted
            else
                SetJoin(VolumeBar, volumeBarValue);

            // mute button feedback
            SetJoin(VolumeMuteButton, audioManager.GetMute(AudioChannel.SourceAudio));
        }

        private void RenderMicLevels()
        {
            // handheld mic
            ushort handheldValue = (ushort)Utilities.RangeScaler(audioManager.GetLevel(AudioChannel.HandheldMic), AudioManager.MicLevelMax, AudioManager.MicLevelMin, UInt16.MaxValue, UInt16.MinValue);
            SetJoin(HandheldMicBar, handheldValue);
            SetItemSelected(ProjectJoin.HandheldMicSmartObjectId, MicMuteIndex, audioManager.GetMute(AudioChannel.HandheldMic));

            // lapel level bar
            ushort lapelValue = (ushort)Utilities.RangeScaler(audioManager.GetLevel(AudioChannel.LapelMic), AudioManager.MicLevelMax, AudioManager.MicLevelMin, UInt16.MaxValue, UInt16.MinValue);
            SetJoin(LapelMicBar, lapelValue);
            SetItemSelected(ProjectJoin.LapelMicSmartObjectId, MicMuteIndex, audioManager.GetMute(AudioChannel.LapelMic));
        }
        #endregion

        #region Callback methods
        private void AudioChannelUpdatedHandler(object sender, AudioChannelUpdatedEventArgs e)
        {
            Trace(string.Format("AudioChannelUpdatedHandler() channel: {0}, level: {1}, mute: {2}", e.Channel, e.Level, e.Mute));

            // auto mute and unmute
            if (e.Channel == AudioChannel.SourceAudio)
            {
                if (e.Level == AudioManager.SourceAudioMin && !e.Mute)
                    audioManager.SetMute(AudioChannel.SourceAudio, true); // auto mute on minimum level
                else if (e.Level > AudioManager.SourceAudioMin && e.Mute)
                    audioManager.SetMute(AudioChannel.SourceAudio, false); // auto unmute above minimum level
            }

            Render();
        }
        #endregion
    }
}
