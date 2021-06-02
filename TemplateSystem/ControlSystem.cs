using ATC.Framework;
using ATC.Framework.Debugging;
using ATC.Framework.Devices;
using ATC.Framework.Nexus;
using ATC.Framework.UserInterface.Standard;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using System;
using TemplateSystem.Devices;
using TemplateSystem.UserInterface;

namespace TemplateSystem
{
    public class ControlSystem : ExtendedControlSystem
    {
        #region Fields

        private readonly Sequence systemPowerOnSequence = new Sequence();
        private readonly Sequence systemPowerOffSequence = new Sequence();

        //  system components
        private SystemConfig systemConfig;
        private SourceManager sourceManager;
        private AudioManager audioManager;
        private StandardUserInterfaceManager<Tsw760> uiManager1;
        private StandardUserInterfaceManager<XpanelForSmartGraphics> uiManager2;
        private NexusSystemAgent nsa;
        private ProjectorDevice projector;
        private ScreenDevice screen;
        private SwitcherDevice switcher;

        #endregion

        #region Constants

        // system specifics
        private const uint Panel1IpId = 0x03;
        private const uint Panel2IpId = 0x04;
        private const string PanelProjectName = "Template-Tsw760";

        #endregion

        #region Properties

        public SystemState SystemState { get; private set; }

        public bool SystemPower
        {
            get { return SystemState == SystemState.PowerUp; }
            set { SetSystemPower(value); }
        }

        /// <summary>
        /// System is busy if system is powering up or powering down.
        /// </summary>
        public bool SystemBusy
        {
            get { return SystemState != SystemState.PowerUp && SystemState != SystemState.PowerDown; }
        }

        // program inforation
        public string ProgramName { get; private set; }
        public string ProgramVersion { get; private set; }

        #endregion

        #region System initialization
        /// <summary>
        /// ControlSystem constructor - this is the entry point for the code.
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                // set tracer options
                Tracer.OutputStackTrace = true;
                Tracer.OutputMemoryLog = true;
                Tracer.OutputErrorLog = true;
                Tracer.ShowCurrentTime = true;

                // print startup message to console
                TraceLine(LineType.Blankline);
                TraceInfo("ControlSystem() constructor running.");

                // add initialization events
                AddInitEvent("LoadConfig", 500);
                AddInitEvent("CreateDevices", 1000);
                AddInitEvent("CreateUserInterfaces", 1500);

                // set power on sequence events
                systemPowerOnSequence.AddEvent("Start", 0);
                systemPowerOnSequence.AddEvent("AudioDefaults", 4000);
                systemPowerOnSequence.AddEvent("Finish", 5000);
                systemPowerOnSequence.EventCallback += new EventHandler<SequenceEventArgs>(SystemPowerOnSequenceCallback);

                // set power off sequence events
                systemPowerOffSequence.AddEvent("Start", 0);
                systemPowerOffSequence.AddEvent("Finish", 5000);
                systemPowerOffSequence.EventCallback += new EventHandler<SequenceEventArgs>(SystemPowerOffSequenceCallback);

                // add console commands
                CrestronConsole.AddNewConsoleCommand(ConsoleSystemPowerHandler, "syspower", "System power (true/false)", ConsoleAccessLevelEnum.AccessProgrammer);
            }
            catch (Exception e)
            {
                TraceException("ControlSystem() exception occurred:", e);
            }
        }

        public override void InitializeSystem()
        {
            base.InitializeSystem();

            // get running program information
            AssemblyName asn = Assembly.GetExecutingAssembly().GetName();
            ProgramName = asn.Name;
            var assembly = Assembly.GetExecutingAssembly();
            ProgramVersion = string.Format("{0}.{1}.{2}.{3}", asn.Version.Major, asn.Version.Minor, asn.Version.Build, asn.Version.Revision);
        }

        /// <summary>
        /// This method gets invoked by the base ExtendedControlSystem classes. Add events to it by calling AddInitEvent in the constructor.
        /// </summary>
        /// <param name="sender">The sequence that initiated this event.</param>
        /// <param name="e">The event arguments</param>
        protected override void InitSequenceCallback(object sender, SequenceEventArgs e)
        {
            TraceInfo(string.Format("InitSequenceCallback() executing event: {0} - {1}", e.Id, e.Name));

            switch (e.Id)
            {
                case 0: // load system config
                    systemConfig = new SystemConfig();
                    systemConfig.Load();
                    break;

                case 1: // create various devices

                    // mock devices
                    projector = new MockProjector();
                    screen = new MockScreen();
                    switcher = new MockSwitcher();

                    // create nexus system agent
                    nsa = CreateNexusSystemAgent();

                    // source manager
                    sourceManager = new SourceManager(nsa, projector) { TraceEnabled = true };

                    // audio manager
                    audioManager = new AudioManager(this) { TraceEnabled = true };
                    break;

                case 2: // create ui managers and components
                    var panel1 = new Tsw760(Panel1IpId, this) { Description = "Tabletop Panel" };
                    uiManager1 = CreateUserInterfaceManager(panel1, "UiManager1", null);

                    var panel2 = new XpanelForSmartGraphics(Panel2IpId, this) { Description = "Virtual Panel" };
                    uiManager2 = CreateUserInterfaceManager(panel2, "UiManager2", panel1);
                    break;
            }
        }

        /// <summary>
        /// Create a user interface manager along with all of its components.
        /// </summary>
        /// <param name="panel">The Crestron panel type to manage.</param>
        private StandardUserInterfaceManager<T> CreateUserInterfaceManager<T>(T panel, string componentName, ISmartObject otherPanel)
            where T : BasicTriListWithSmartObject
        {
            try
            {
                // create new standard ui manager
                StandardUserInterfaceManager<T> uiManager = new StandardUserInterfaceManager<T>(panel)
                {
                    ComponentName = componentName,
                    TraceEnabled = true,
                };

                // create ui components
                uiManager.CreateComponent<GeneralUserInterface>();
                uiManager.CreateComponent<SettingsUserInterface>();
                uiManager.CreateComponent<AudioUserInterface>();

                // add power sequences
                uiManager.SetWaitPageSequences(systemPowerOnSequence, systemPowerOffSequence);

                // register panel with control system
                if (otherPanel == null)
                {
                    var sgdPath = string.Format("{0}\\PanelDesign\\{1}.sgd", Directory.GetApplicationDirectory(), PanelProjectName);
                    uiManager.Register(sgdPath);
                }
                else
                    uiManager.Register(otherPanel);

                return uiManager;
            }
            catch (Exception ex)
            {
                TraceException("CreatePanelManager() exception caught.", ex);
                return null;
            }
        }

        private NexusSystemAgent CreateNexusSystemAgent()
        {
            // create the nexus system agent
            nsa = new NexusSystemAgent()
            {
                //ApiUrl = "https://192.168.1.6:8443", // specify alternate API URL for local testing
                CompanyId = "atc",
                GroupId = "frasers-group",
                SystemId = "template-system",
                Secret = "EeUKMx03sUCDBdx2fusYS1zm", // system secret is acquired from Nexus Web Client
                TraceEnabled = false, // enabling this will cause a logging loop if logging is also enabled
                TraceLevel = TraceLevel.Standard,
                LoggingEnabled = true, // send system logs to Nexus
            };
            nsa.AddDevice(projector, screen, switcher); // add devices to be monitored
            nsa.State.Status = "Freshly started";

            return nsa;
        }

        #endregion

        #region System Power

        private void SetSystemPower(bool value)
        {
            try
            {
                // abort if system is currently busy
                if (SystemBusy)
                {
                    TraceError("SystemPowerSet() called but system is currently busy.");
                    return;
                }

                if (value)
                {
                    Trace("SystemPowerSet() turning system on.");
                    systemPowerOnSequence.Start();
                }
                else
                {
                    Trace("SystemPowerSet() turning system off.");
                    systemPowerOffSequence.Start();
                }
            }
            catch (Exception e)
            {
                TraceException("SystemPowerSet() exception caught.", e);
            }
        }

        private void SystemPowerOnSequenceCallback(object sender, SequenceEventArgs args)
        {
            Trace(string.Format("SystemPowerOnSequenceCallback() called for event. Id: {0}, name: \"{1}\"", args.Id, args.Name));

            switch (args.Id)
            {
                case 0: // start
                    SystemState = SystemState.PoweringUp;
                    uiManager1.Page = Page.Wait;
                    uiManager2.Page = Page.Wait;
                    projector.SetPower(true);
                    projector.SetInput("HDMI1");
                    screen.Down();
                    nsa.State.Status = "System powering up";
                    break;
                case 1: // audio defaults
                    audioManager.SetDefaults();
                    break;
                case 2: // finish
                    SystemState = SystemState.PowerUp;
                    uiManager1.Page = Page.Main;
                    uiManager2.Page = Page.Main;
                    nsa.State.Powered = true;
                    nsa.State.Status = "System powered up";
                    break;
                default:
                    TraceError(string.Format("SystemPowerOnSequenceCallback() unhandled event. Id: {0}, name: \"{1}\"", args.Id, args.Name));
                    break;
            }
        }

        private void SystemPowerOffSequenceCallback(object sender, SequenceEventArgs args)
        {
            Trace(string.Format("SystemPowerOffSequenceCallback() called for event. Id: {0}, name: \"{1}\"", args.Id, args.Name));

            switch (args.Id)
            {
                case 0: // start
                    SystemState = SystemState.PoweringDown;
                    uiManager1.Page = Page.Wait;
                    uiManager2.Page = Page.Wait;
                    projector.SetPower(false);
                    screen.Up();
                    nsa.State.Status = "System powering down";
                    break;
                case 1: // finish
                    SystemState = SystemState.PowerDown;
                    uiManager1.Reset(); // standard UI reset sets page to Start
                    uiManager2.Reset();
                    nsa.State.Powered = false;
                    nsa.State.Status = "System powered down";
                    break;
                default:
                    TraceError(string.Format("SystemPowerOffSequenceCallback() unhandled event. Id: {0}, name: \"{1}\"", args.Id, args.Name));
                    break;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Restart the currently running program.
        /// </summary>
        public void ProgramRestart()
        {
            Trace("RestartProgram() sending program restart for slot: " + ProgramNumber);
            string command = string.Format("progres -p:{0}", ProgramNumber);
            string response = string.Empty;
            CrestronConsole.SendControlSystemCommand(command, ref response);
        }

        #endregion

        #region Console command handlers

        private void ConsoleSystemPowerHandler(string input)
        {
            try
            {
                bool value = Boolean.Parse(input);
                SetSystemPower(value);
            }
            catch (Exception ex)
            {
                TraceException("ConsoleSystemPowerHandler() exception caught.", ex);
            }
        }

        #endregion
    }
}
