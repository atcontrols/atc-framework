using ATC.Framework;
using ATC.Framework.UserInterface;
using ATC.Framework.UserInterface.Standard;
using System;

namespace TemplateSystem.UserInterface
{
    public class SettingsUserInterface : UserInterfaceComponent
    {
        #region Fields

        private IStandardUserInterface standardUi;
        private readonly ControlSystem controlSystem;
        private readonly SystemConfig systemConfig;
        private bool passcodeChangeActive;

        #endregion

        #region Join definitions

        // digital joins
        private readonly DigitalJoin SettingsEnterButton = new DigitalJoin(ProjectJoin.SettingsEnterButton);
        private readonly DigitalJoin SettingsExitButton = new DigitalJoin(ProjectJoin.SettingsExitButton);
        private readonly DigitalJoin ChangePasscodeButton = new DigitalJoin(ProjectJoin.ChangePasscodeButton);
        private readonly DigitalJoin RestartProgramButton = new DigitalJoin(ProjectJoin.RestartProgramButton);
        private readonly DigitalJoin SaveChangesButton = new DigitalJoin(ProjectJoin.SaveChangesButton);
        private readonly DigitalJoin SaveChangesEnable = new DigitalJoin(ProjectJoin.SaveChangesEnable);
        private readonly DigitalJoin SystemClockToggle = new DigitalJoin(ProjectJoin.SystemClockToggle);

        // serial joins
        private readonly SerialJoin ProgramName = new SerialJoin(ProjectJoin.ProgramName);
        private readonly SerialJoin ProgramVersion = new SerialJoin(ProjectJoin.ProgramVersion);
        private readonly SerialJoin RoomNameIn = new SerialJoin(ProjectJoin.RoomNameIn);
        private readonly SerialJoin RoomNameOut = new SerialJoin(ProjectJoin.RoomNameOut);

        #endregion

        #region Constructor

        public SettingsUserInterface()
        {
            controlSystem = SystemComponent.GetComponent<ControlSystem>();
            systemConfig = SystemComponent.GetComponent<SystemConfig>();
        }

        protected override void Initialize()
        {
            // get standard ui reference
            if (Manager is IStandardUserInterface)
                standardUi = (IStandardUserInterface)GetComponent(Manager.ComponentName);

            // add joins to be handled by this component
            AddJoinListener(SettingsEnterButton);
            AddJoinListener(SettingsExitButton);
            AddJoinListener(ChangePasscodeButton);
            AddJoinListener(SaveChangesButton);
            AddJoinListener(RestartProgramButton);
            AddJoinListener(SystemClockToggle);
            AddJoinListener(RoomNameOut, RoomNameOutHandler);
        }

        #endregion

        #region Panel event handlers

        protected override void DigitalJoinHandler(DigitalJoin join)
        {
            if (join.Value != DigitalJoinState.Pressed)
                return;

            switch (join.Number)
            {
                case ProjectJoin.SettingsEnterButton:
                    passcodeChangeActive = false;
                    standardUi.ActivatePasscodePopup("Enter correct passcode to access settings", PasscodeEnteredHandler);
                    break;
                case ProjectJoin.SettingsExitButton:
                    systemConfig.RevertChanges();
                    SettingsExit();
                    break;
                case ProjectJoin.ChangePasscodeButton:
                    passcodeChangeActive = true;
                    standardUi.ActivatePasscodePopup("Enter new passcode", true, PasscodeEnteredHandler);
                    break;
                case ProjectJoin.SaveChangesButton:
                    if (systemConfig.Save())
                        standardUi.ActivateNotificationPopup("System configuration saved successfully.");
                    else
                        standardUi.ActivateNotificationPopup("Error saving system configuration.");
                    SettingsExit();
                    break;
                case ProjectJoin.RestartProgramButton:
                    standardUi.ActivatePromptPopup("Are you sure you wish to restart the program?", "Restart", "Continue Using", RestartProgramReponseHandler);
                    break;
                case ProjectJoin.SystemClockToggle:
                    systemConfig.SystemClockEnabled = !systemConfig.SystemClockEnabled;
                    Render();
                    break;
            }
        }

        protected void RoomNameOutHandler(SerialJoin join)
        {
            systemConfig.RoomName = join.Value;
            Trace("RoomNameOutHandler() Room name set to: " + join.Value);
            Render();
        }

        #endregion

        #region Feedback update methods

        public override void Render()
        {
            // top bar
            if (standardUi.Page == Page.Settings)
            {
                standardUi.TopBarText = "Settings";
                standardUi.IsMenuButtonVisible = false;
            }

            SetJoin(SaveChangesEnable, systemConfig.ChangesDetected());
            SetJoin(SystemClockToggle, systemConfig.SystemClockEnabled);
            SetJoin(RoomNameIn, systemConfig.RoomName);

            // program information
            SetJoin(ProgramName, controlSystem.ProgramName);
            SetJoin(ProgramVersion, controlSystem.ProgramVersion);

            // date and time visiblity
            standardUi.IsDateTimeVisible = systemConfig.SystemClockEnabled;
        }

        #endregion

        private void SettingsExit()
        {
            if (controlSystem.SystemPower)
                standardUi.Page = Page.Main;
            else
                standardUi.Page = Page.Start;
        }

        #region Standard UI callbacks

        private void RestartProgramReponseHandler(PromptResponse response)
        {
            if (response == PromptResponse.Yes)
                controlSystem.ProgramRestart();
        }

        private void PasscodeEnteredHandler(int passcode)
        {
            if (passcodeChangeActive)
            {
                if (systemConfig.Passcode == passcode)
                    standardUi.ActivateNotificationPopup("Passcode Changed", "Passcode entered is the same as current passcode. No changes made.");
                else
                {
                    systemConfig.Passcode = passcode;
                    standardUi.ActivateNotificationPopup("Passcode Changed", String.Format("System passcode has been changed to: {0}. Save changes to make it permanent.", passcode));
                    Render();
                }
            }
            else
            {
                if (passcode == systemConfig.Passcode)
                {
                    Trace("PasscodeEnteredHandler() system passcode matched.");
                    standardUi.Page = Page.Settings;
                }
                else
                {
                    Trace("PasscodeEnteredHandler() system passcode did not match.");
                    standardUi.ActivateNotificationPopup("Incorrect Passcode", "An incorrect passcode has been entered.");
                }
            }
        }

        #endregion
    }
}
