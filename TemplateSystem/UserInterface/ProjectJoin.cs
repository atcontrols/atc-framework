namespace TemplateSystem.UserInterface
{
    /// <summary>
    /// This class contains all project specific join number definitions.
    /// </summary>
    public static class ProjectJoin
    {
        // main page popups
        public const uint PopupMainSourceSelect = 20;
        public const uint PopupMainMicLevels = 21;
        public const uint PopupMainLaptop = 22;
        public const uint PopupMainWireless = 23;
        public const uint PopupMainTelevision = 24;

        // general buttons
        public const uint SystemStartupButton = 30;
        public const uint PreviousPopupButton = 31;
        public const uint PreviousPopupVisible = 32;

        // volume buttons
        public const uint VolumeUpButton = 40;
        public const uint VolumeDownButton = 41;
        public const uint VolumeMuteButton = 42;

        // analog joins
        public const uint VolumeBar = 1;
        public const uint HandheldMicBar = 3;
        public const uint LapelMicBar = 4;

        // smart object ids
        public const uint MenuSmartObjectId = 1;
        public const uint SourceSelectSmartObjectId = 2;
        public const uint HandheldMicSmartObjectId = 3;
        public const uint LapelMicSmartObjectId = 4;

        // settings page - digital
        public const uint SettingsEnterButton = 200;
        public const uint SettingsExitButton = 201;
        public const uint ChangePasscodeButton = 202;
        public const uint RestartProgramButton = 203;
        public const uint SaveChangesButton = 204;
        public const uint SaveChangesEnable = 205;
        public const uint SystemClockToggle = 206;

        // settings page - serial
        public const uint ProgramName = 200;
        public const uint ProgramVersion = 201;
        public const uint RoomNameIn = 202;
        public const uint RoomNameOut = 203;
    }
}
