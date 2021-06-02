using ATC.Framework.Debugging;
using ATC.Framework.Devices;
using System;

namespace ATC.Framework.Nexus
{
    public class NexusSystemAgent : PollerComponent, IDisposable
    {
        #region Fields
        private IRequestManager requestManager;
        private DeviceManager deviceManager;
        private string _apiUrl, _companyId, _groupId, _systemId, _secret;
        #endregion

        #region Properties
        /// <summary>
        /// The address of the API server (default: https://api.nexus.atcontrols.com.au)
        /// </summary>
        public string ApiUrl
        {
            get { return _apiUrl; }
            set
            {
                _apiUrl = value;
                CreateManagers();
            }
        }

        /// <summary>
        /// The Nexus Company ID that the system belongs to.
        /// </summary>
        public string CompanyId
        {
            get { return _companyId; }
            set
            {
                _companyId = value;
                CreateManagers();
            }
        }

        /// <summary>
        /// The Nexus Group ID that the system belongs to.
        /// </summary>
        public string GroupId
        {
            get { return _groupId; }
            set
            {
                _groupId = value;
                CreateManagers();
            }
        }

        /// <summary>
        /// The Nexus System ID that is associated with the system.
        /// </summary>
        public string SystemId
        {
            get { return _systemId; }
            set
            {
                _systemId = value;
                CreateManagers();
            }
        }

        /// <summary>
        /// The system secret (password) to enable writing to the system.
        /// </summary>
        public string Secret
        {
            get { return _secret; }
            set
            {
                _secret = value;
                CreateManagers();
            }
        }

        /// <summary>
        /// Returns true if required properties have been set correctly.
        /// </summary>
        public bool Validated
        {
            get { return ValidateProperties(); }
        }

        public SystemState State { get; private set; }

        /// <summary>
        /// If set to true, the memory log from the Tracer class will be automatically sent to Nexus.
        /// </summary>
        public bool LoggingEnabled { get; set; }

        public override bool TraceEnabled
        {
            get
            {
                return base.TraceEnabled;
            }
            set
            {
                base.TraceEnabled = value;
                if (ManagersDefined())
                {
                    requestManager.TraceEnabled = value;
                    deviceManager.TraceEnabled = value;
                }
            }
        }

        public override TraceLevel TraceLevel
        {
            get
            {
                return base.TraceLevel;
            }
            set
            {
                base.TraceLevel = value;
                if (ManagersDefined())
                {
                    requestManager.TraceLevel = value;
                    deviceManager.TraceLevel = value;
                }
            }
        }
        #endregion

        #region Constants
        private const string ApiUrlDefault = "https://api.nexus.atcontrols.com.au";
        #endregion

        #region Constructor
        public NexusSystemAgent()
            : this(null, null, null, null, ApiUrlDefault) { }

        public NexusSystemAgent(string companyId, string groupId, string systemId, string secret)
            : this(companyId, groupId, systemId, secret, ApiUrlDefault) { }

        public NexusSystemAgent(string companyId, string groupId, string systemId, string secret, string apiUrl)
        {
            CompanyId = companyId;
            GroupId = groupId;
            SystemId = systemId;
            Secret = secret;
            ApiUrl = apiUrl;

            CreateManagers();

            State = new SystemState();
            State.UpdateHandler += new Action<SystemState>(StateUpdateHandler);

            // automatically enable polling
            PollingEnabled = true;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Add a device to be monitored and its state sent to Nexus.
        /// </summary>
        /// <param name="device"></param>
        public void AddDevice(IDevice device)
        {
            deviceManager.AddDevice(device);
        }

        /// <summary>
        /// Add an array of devices to be monitored and their states be sent to Nexus.
        /// </summary>
        /// <param name="devices"></param>
        public void AddDevice(params IDevice[] devices)
        {
            foreach (Device device in devices)
                AddDevice(device);
        }

        public void Dispose()
        {
            requestManager.Dispose();
        }
        #endregion

        #region Private methods
        private bool ValidateProperties()
        {
            if (string.IsNullOrEmpty(CompanyId))
                return false;
            else if (string.IsNullOrEmpty(GroupId))
                return false;
            else if (string.IsNullOrEmpty(SystemId))
                return false;
            else if (string.IsNullOrEmpty(Secret))
                return false;
            else if (string.IsNullOrEmpty(ApiUrl) || !(ApiUrl.StartsWith("http://") || ApiUrl.StartsWith("https://")))
                return false;

            return true;
        }

        private void CreateManagers()
        {
            if (!ValidateProperties())
                return;

            Trace("CreateManagers() creating manager objects.");

            // dispose of previous request manager
            if (requestManager != null)
                requestManager.Dispose();

            // create request manager based on http protocol
            if (ApiUrl.StartsWith("http://"))
                requestManager = new HttpRequestManager(CompanyId, GroupId, SystemId, Secret, ApiUrl);
            else
                requestManager = new HttpsRequestManager(CompanyId, GroupId, SystemId, Secret, ApiUrl);

            deviceManager = new DeviceManager(requestManager);

            // keep trace properties in sync
            TraceEnabled = TraceEnabled;
            TraceLevel = TraceLevel;
        }

        private bool ManagersDefined()
        {
            return requestManager != null &&
                deviceManager != null;
        }

        private void SendSystemStateUpdate()
        {
            string url = string.Format("{0}/system/{1}/{2}/{3}/state", ApiUrl, CompanyId, GroupId, SystemId);
            requestManager.SendRequest(HttpMethod.Patch, url, State);
        }

        private void SendLogEntries()
        {
            var count = Tracer.GetLogEntryCount();
            if (count <= 0)
                return;

            string url = string.Format("{0}/log/{1}/{2}/{3}", ApiUrl, CompanyId, GroupId, SystemId);
            LogEntry[] entries = Tracer.GetLogEntries();

            requestManager.SendRequest(HttpMethod.Post, url, entries);
            Trace(string.Format("SendLogEntries() sent {0} log entries.", count));
            Tracer.ClearLogEntries();
        }
        #endregion

        #region Event handlers
        protected override void PollingCallback(object o)
        {
            if (LoggingEnabled)
            {
                if (!Tracer.OutputMemoryLog)
                {
                    TraceWarning("PollingCallback() logging is enabled but the Tracer class does not have memory logging enabled. No action taken.");
                    return;
                }

                SendLogEntries();
            }

            deviceManager.SendDevices();
        }

        private void StateUpdateHandler(SystemState state)
        {
            SendSystemStateUpdate();
        }
        #endregion
    }
}
