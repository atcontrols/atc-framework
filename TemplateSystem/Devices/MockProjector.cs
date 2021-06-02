using ATC.Framework.Devices;
using Crestron.SimplSharp;
using System;

namespace TemplateSystem.Devices
{
    public class MockProjector : ProjectorDevice
    {
        private readonly CTimer timer;

        public MockProjector()
        {
            Details = new DeviceDetails()
            {
                Name = "Test Projector",
                Description = "A simulated projector",
                Manufacturer = "ShonkyCo",
                Model = "ShonkyProj 2000",
            };

            Online = true;
            LampHours = 100;

            timer = new CTimer(TimerCallback, null, 10000, 10000); // update lamp hours every 10 seconds
        }

        public override string[] GetInputs()
        {
            throw new NotImplementedException();
        }

        public override void SetInput(string input)
        {
            Trace("SetInput() setting input to: " + input);
            Input = input;
        }

        public override void SetPower(bool value)
        {
            Power = value ? Power.On : Power.Off;
        }

        private void TimerCallback(object o)
        {
            // increment lamphours
            LampHours++;
        }
    }
}
