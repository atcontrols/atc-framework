using System;

namespace ATC.Framework.Devices
{
    public interface IConferenceCodecDevice : IPowerDevice
    {
        bool MicMute { get; }
        bool CameraMute { get; }
        bool CallActive { get; }
        bool ContentActive { get; }

        void SetMicMute(bool value);
        void ToggleMicMute();
        void SetCameraMute(bool value);
        void ToggleCameraMute();
        void StartContent();
        void StopContent();

        event EventHandler<MicMuteEventArgs> MicMuteEventHandler;
        event EventHandler<CameraMuteEventArgs> CameraMuteEventHandler;
        event EventHandler<CallActiveEventArgs> CallActiveEventHandler;
        event EventHandler<ContentActiveEventArgs> ContentActiveEventHandler;
    }

    public abstract class ConferenceCodecDevice : PowerDevice, IConferenceCodecDevice
    {
        #region Fields

        private bool _callActive, _micMute, _cameraMute, _contentActive;

        #endregion

        #region Properties

        /// <summary>
        /// Local microphone mute (privacy) state.
        /// </summary>
        public bool MicMute
        {
            get { return _micMute; }
            protected set
            {
                if (_micMute != value)
                {
                    _micMute = value;
                    Trace("MicMute set to: " + value);

                    // raise event
                    if (MicMuteEventHandler != null)
                        MicMuteEventHandler(this, new MicMuteEventArgs() { Value = value });
                }
            }
        }

        public bool CameraMute
        {
            get { return _cameraMute; }
            protected set
            {
                if (_cameraMute != value)
                {
                    _cameraMute = value;
                    Trace("CameraMute set to: " + value);

                    // raise event
                    if (CameraMuteEventHandler != null)
                        CameraMuteEventHandler(this, new CameraMuteEventArgs() { Value = value });
                }
            }
        }

        public bool CallActive
        {
            get { return _callActive; }
            protected set
            {
                if (_callActive != value)
                {
                    _callActive = value;
                    Trace("CallActive set to: " + value);

                    // raise event
                    if (CallActiveEventHandler != null)
                        CallActiveEventHandler(this, new CallActiveEventArgs() { Value = value });
                }
            }
        }

        public bool ContentActive
        {
            get { return _contentActive; }
            protected set
            {
                if (_contentActive != value)
                {
                    _contentActive = value;
                    Trace("ContentActive set to: " + value);

                    // raise event
                    if (ContentActiveEventHandler != null)
                        ContentActiveEventHandler(this, new ContentActiveEventArgs() { Value = value });
                }
            }
        }

        #endregion

        #region Methods

        public abstract void SetMicMute(bool value);
        public abstract void ToggleMicMute();
        public abstract void SetCameraMute(bool value);
        public abstract void ToggleCameraMute();
        public abstract void StartContent();
        public abstract void StopContent();

        #endregion

        #region Events

        public event EventHandler<MicMuteEventArgs> MicMuteEventHandler;
        public event EventHandler<CameraMuteEventArgs> CameraMuteEventHandler;
        public event EventHandler<CallActiveEventArgs> CallActiveEventHandler;
        public event EventHandler<ContentActiveEventArgs> ContentActiveEventHandler;

        #endregion
    }

    public class MicMuteEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }

    public class CameraMuteEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }

    public class CallActiveEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }

    public class ContentActiveEventArgs : EventArgs
    {
        public bool Value { get; set; }
    }
}
