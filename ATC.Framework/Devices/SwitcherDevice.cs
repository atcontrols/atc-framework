namespace ATC.Framework.Devices
{
    public interface ISwitcherDevice : IDevice
    {
        /// <summary>
        /// The number of inputs this switcher has.
        /// </summary>
        int InputCount { get; }

        /// <summary>
        /// The number of outputs this switcher has.
        /// </summary>
        int OutputCount { get; }

        void Switch(int input, int output);
        int GetInput(int output);
    }

    public abstract class SwitcherDevice : Device, ISwitcherDevice
    {
        public int InputCount { get; protected set; }
        public int OutputCount { get; protected set; }

        public abstract void Switch(int input, int output);
        public abstract int GetInput(int output);
    }
}
