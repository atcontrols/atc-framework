using ATC.Framework.Devices;
using System;

namespace TemplateSystem.Devices
{
    public class MockScreen : ScreenDevice
    {
        public MockScreen()
        {
            Details = new DeviceDetails()
            {
                Name = "Test Screen",
                Description = "A simulated projection screen",
                Manufacturer = "ShonkyCo",
                Model = "ShonkyScreen 500",
            };

            Online = true;
        }

        public override void Up()
        {
            Position = ScreenPosition.AtTop;
        }

        public override void Down()
        {
            Position = ScreenPosition.AtBottom;
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}