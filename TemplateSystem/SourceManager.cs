using ATC.Framework;
using ATC.Framework.Devices;
using ATC.Framework.Nexus;
using System;

namespace TemplateSystem
{
    public class SourceManager : SystemComponent
    {
        private readonly NexusSystemAgent nsa;
        private readonly IProjectorDevice projector;
        private Source source = Source.None;

        public Source Source
        {
            get { return source; }
            set
            {
                source = value;
                string message = "Displaying source: " + value;
                Trace(message);
                nsa.State.Status = message;
                SourceUpdatedNotify(value);

                // set input on projecto
                switch (value)
                {
                    case Source.Laptop: projector.SetInput("HDMI1"); break;
                    case Source.Wireless: projector.SetInput("HDMI2"); break;
                    case Source.Television: projector.SetInput("HDMI3"); break;
                }
            }
        }

        public SourceManager(NexusSystemAgent nsa, IProjectorDevice projector)
        {
            this.nsa = nsa;
            this.projector = projector;
        }

        public event EventHandler<SourceUpdatedEventArgs> SourceUpdatedHandler;

        private void SourceUpdatedNotify(Source source)
        {
            if (SourceUpdatedHandler != null)
                SourceUpdatedHandler(this, new SourceUpdatedEventArgs(source));
            else
                TraceWarning("SourceUpdatedNotify() there is no event handler defined.");
        }
    }

    public class SourceUpdatedEventArgs : EventArgs
    {
        public Source Source { get; private set; }

        public SourceUpdatedEventArgs(Source source)
        {
            Source = source;
        }
    }

    public enum Source
    {
        None,
        Laptop,
        Wireless,
        Television,
    }
}
