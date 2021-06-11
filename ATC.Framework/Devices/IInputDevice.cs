using System;

namespace ATC.Framework.Devices
{
    public interface IInputDevice
    {
        string Input { get; }

        string[] GetInputs();
        void SetInput(string value);

        event EventHandler<InputEventArgs> InputEventHandler;
    }
}
