using ATC.Framework;
using System;

namespace TemplateSystem
{
    public class SystemConfig : SystemComponent, ICloneable
    {
        #region Properties

        public int Passcode { get; set; }
        public string RoomName { get; set; }
        public bool SystemClockEnabled { get; set; }

        #endregion

        private static SystemConfig lastLoadedConfig;

        #region Constructor

        public SystemConfig()
        {
            Passcode = 1988;
            RoomName = "New Room";
            SystemClockEnabled = true;
        }

        #endregion;

        #region Public methods

        public bool Load()
        {
            SystemConfig config = ConfigManager.Read<SystemConfig>();
            if (config == null)
            {
                TraceWarning("Load() error reading file.");
                return false;
            }

            lastLoadedConfig = config;

            Passcode = config.Passcode;
            RoomName = config.RoomName;
            SystemClockEnabled = config.SystemClockEnabled;

            return true;
        }

        public bool Save()
        {
            bool result = ConfigManager.Write<SystemConfig>(this);
            if (result)
                lastLoadedConfig = this;

            return result;
        }

        /// <summary>
        /// Returns true if the specified object is equal to this object.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool Equals(SystemConfig o)
        {
            if (o == null)
                return false;
            else if
            (
                Passcode == o.Passcode &&
                RoomName == o.RoomName &&
                SystemClockEnabled == o.SystemClockEnabled
            )
                return true;
            else
                return false;
        }

        public object Clone()
        {
            var clone = new SystemConfig();

            clone.Passcode = Passcode;
            clone.RoomName = RoomName;
            clone.SystemClockEnabled = SystemClockEnabled;

            return clone;
        }

        /// <summary>
        /// Returns true if this SystemConfig is not equal to the last loaded system config.
        /// </summary>
        /// <returns></returns>
        public bool ChangesDetected()
        {
            return !Equals(lastLoadedConfig);
        }

        /// <summary>
        /// Returns an object with the last loaded values.
        /// </summary>
        /// <returns></returns>
        public void RevertChanges()
        {
            Passcode = lastLoadedConfig.Passcode;
            RoomName = lastLoadedConfig.RoomName;
            SystemClockEnabled = lastLoadedConfig.SystemClockEnabled;
        }

        #endregion
    }
}
