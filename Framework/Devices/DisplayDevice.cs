using System;

namespace ATC.Framework.Devices
{
    public interface IDisplayDevice : IPowerDevice
    {
        /// <summary>
        /// The current input this display device is on.
        /// </summary>
        string Input { get; }

        string[] GetInputs();
        void SetInput(string input);

        event EventHandler<InputEventArgs> InputEventHandler;
    }

    public abstract class DisplayDevice : PowerDevice, IDisplayDevice
    {
        private string _input;

        /// <summary>
        /// The currently active input for this display.
        /// </summary>
        public string Input
        {
            get { return _input; }
            protected set
            {
                if (_input != value)
                {
                    _input = value;
                    Trace("Input set to: " + value);

                    // raise event
                    if (InputEventHandler != null)
                    {
                        var args = new InputEventArgs()
                        {
                            Value = value,
                        };
                        InputEventHandler(this, args);
                    }
                }
            }
        }

        public abstract string[] GetInputs();
        public abstract void SetInput(string input);

        public event EventHandler<InputEventArgs> InputEventHandler;
    }

    public class InputEventArgs : EventArgs
    {
        public string Value { get; set; }
    }
}
