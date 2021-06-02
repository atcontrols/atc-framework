using ATC.Framework.Devices;
using Crestron.SimplSharp;
using System;

namespace TemplateSystem.Devices
{
    public class MockSwitcher : SwitcherDevice
    {
        private readonly CTimer timer;

        public MockSwitcher()
        {
            Details = new DeviceDetails()
            {
                Name = "Test Switcher",
                Description = "A simulated 8x8 matrix switcher",
                Manufacturer = "Extron",
                Model = "EXT-8X4-HDMI",
            };

            Online = true;
            InputCount = 8;
            OutputCount = 4;

            timer = new CTimer(TimerCallback, null, 10000, 10000);
        }

        public override void Switch(int input, int output)
        {
            throw new NotImplementedException();
        }

        public override int GetInput(int output)
        {
            throw new NotImplementedException();
        }

        private void TimerCallback(object o)
        {
            // toggle online status
            Online = !Online;
        }
    }
}
