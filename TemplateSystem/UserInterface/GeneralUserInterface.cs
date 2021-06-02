using ATC.Framework;
using ATC.Framework.UserInterface;
using ATC.Framework.UserInterface.Standard;
using System;

namespace TemplateSystem.UserInterface
{
    public class GeneralUserInterface : UserInterfaceComponent
    {
        #region Fields

        private readonly ControlSystem controlSystem;
        private readonly SystemConfig systemConfig;
        private readonly SourceManager sourceManager;

        private IStandardUserInterface standardUi;

        private Category _category;

        #endregion

        #region Join definitions

        // main popups
        private readonly DigitalJoin PopupMainSourceSelect = new DigitalJoin(ProjectJoin.PopupMainSourceSelect);
        private readonly DigitalJoin PopupMainMicLevels = new DigitalJoin(ProjectJoin.PopupMainMicLevels);
        private readonly DigitalJoin PopupMainLaptop = new DigitalJoin(ProjectJoin.PopupMainLaptop);
        private readonly DigitalJoin PopupMainWireless = new DigitalJoin(ProjectJoin.PopupMainWireless);
        private readonly DigitalJoin PopupMainTelevision = new DigitalJoin(ProjectJoin.PopupMainTelevision);

        // buttons
        private readonly DigitalJoin SystemStartupButton = new DigitalJoin(ProjectJoin.SystemStartupButton);
        private readonly DigitalJoin PreviousPopupButton = new DigitalJoin(ProjectJoin.PreviousPopupButton);
        private readonly DigitalJoin PreviousPopupVisible = new DigitalJoin(ProjectJoin.PreviousPopupVisible);

        #endregion

        #region Properties

        private Category Category
        {
            get { return _category; }
            set
            {
                _category = value;
                Trace("Category set to: " + value);
                Render();
            }
        }

        /// <summary>
        /// Returns true if previous button should be visible.
        /// </summary>
        private bool PreviousButtonVisible
        {
            get
            {
                if (standardUi.IsMenuPopupActive)
                    return false;
                else if (standardUi.Page == Page.Settings)
                    return false;
                else if (Category == Category.SourceSelect && sourceManager.Source != Source.None)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region Constructor

        public GeneralUserInterface()
        {
            controlSystem = GetComponent<ControlSystem>();
            systemConfig = GetComponent<SystemConfig>();
            sourceManager = GetComponent<SourceManager>();

            AddJoinListener(SystemStartupButton);
            AddJoinListener(PreviousPopupButton);

            AddSmartObjectListener(ProjectJoin.MenuSmartObjectId);
            AddSmartObjectListener(ProjectJoin.SourceSelectSmartObjectId);

            // add event handlers
            sourceManager.SourceUpdatedHandler += new EventHandler<SourceUpdatedEventArgs>(SourceUpdatedHandler);

            // set default values
            Reset();
        }

        #endregion

        #region Panel event handlers

        protected override void Initialize()
        {
            // get standard ui reference
            if (Manager is IStandardUserInterface)
                standardUi = (IStandardUserInterface)GetComponent(Manager.ComponentName);
        }

        protected override void OnlineStateHandler(bool panelOnline)
        {
            if (panelOnline)
            {
                // set page based on system state
                switch (controlSystem.SystemState)
                {
                    case SystemState.PowerDown:
                        standardUi.Page = Page.Start;
                        break;
                    case SystemState.PowerUp:
                        standardUi.Page = Page.Main;
                        break;
                    case SystemState.PoweringUp:
                    case SystemState.PoweringDown:
                        standardUi.Page = Page.Wait;
                        break;
                }
            }
        }

        protected override void DigitalJoinHandler(DigitalJoin join)
        {
            if (join.Value != DigitalJoinState.Pressed)
                return;

            switch (join.Number)
            {
                case ProjectJoin.SystemStartupButton:
                    controlSystem.SystemPower = true;
                    break;
                case ProjectJoin.PreviousPopupButton:
                    PreviousButtonHandler();
                    break;
            }
        }

        protected override void SmartObjectBoolHandler(uint smartObjectId, uint number, string name, DigitalJoinState state)
        {
            switch (smartObjectId)
            {
                case ProjectJoin.MenuSmartObjectId:
                    MenuHandler(number, state);
                    break;
                case ProjectJoin.SourceSelectSmartObjectId:
                    SourceSelectHandler(number, state);
                    break;
            }
        }

        /// <summary>
        /// Handles menu button events.
        /// </summary>
        /// <param name="button">The menu item that was pressed / released.</param>
        /// <param name="pressed">True if button pressed.</param>
        private void MenuHandler(uint number, DigitalJoinState state)
        {
            try
            {
                if (state == DigitalJoinState.Pressed)
                {
                    // close menu before dealing with button press
                    standardUi.IsMenuPopupActive = false;

                    // perform selected menu item action
                    switch (number)
                    {
                        case 1:
                            Category = Category.SourceSelect;
                            break;
                        case 2:
                            Category = Category.MicLevels;
                            break;
                        case 3:
                            standardUi.ActivatePasscodePopup("Enter correct passcode to access settings", PasscodeEnteredHandler);
                            break;
                        case 4:
                            standardUi.ActivatePromptPopup("Are you sure you wish to power down the system?", "Power Down", "Continue Using", ShutdownPromptReponseHandler);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException("MenuHandler() exception caught.", ex);
            }
        }

        /// <summary>
        /// Handles source select Smart Object
        /// </summary>
        /// <param name="button"></param>
        /// <param name="pressed"></param>
        private void SourceSelectHandler(uint number, DigitalJoinState state)
        {
            try
            {
                if (state == DigitalJoinState.Pressed)
                {
                    // perform selected menu item action
                    switch (number)
                    {
                        case 1: // laptop
                            sourceManager.Source = Source.Laptop;
                            break;
                        case 2: // wireless
                            sourceManager.Source = Source.Wireless;
                            break;
                        case 3: // television
                            sourceManager.Source = Source.Television;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException("SourceSelectHandler() exception caught.", ex);
            }
        }

        /// <summary>
        /// Previous popup button handler.
        /// </summary>
        private void PreviousButtonHandler()
        {
            try
            {
                if (sourceManager.Source != Source.None)
                    sourceManager.Source = Source.None;
            }
            catch (Exception ex)
            {
                TraceException("PreviousPopupButtonHandler() exception caught.", ex);
            }
        }

        #endregion

        #region Feedback update methods

        public override void Render()
        {
            try
            {
                SetJoin(PreviousPopupVisible, PreviousButtonVisible);

                if (standardUi.Page == Page.Main)
                {
                    standardUi.TopBarText = GenerateTopBarText();
                    standardUi.IsMenuButtonVisible = true;

                    // main popups
                    SetJoin(PopupMainSourceSelect, Category == Category.SourceSelect && sourceManager.Source == Source.None);
                    SetJoin(PopupMainMicLevels, Category == Category.MicLevels);
                    SetJoin(PopupMainLaptop, Category == Category.SourceSelect && sourceManager.Source == Source.Laptop);
                    SetJoin(PopupMainWireless, Category == Category.SourceSelect && sourceManager.Source == Source.Wireless);
                    SetJoin(PopupMainTelevision, Category == Category.SourceSelect && sourceManager.Source == Source.Television);
                }
            }
            catch (Exception ex)
            {
                TraceException("FeedbackUpdate() exception caught.", ex);
            }
        }

        public override void Reset()
        {
            Trace("Reset() resetting class variables.");

            _category = Category.SourceSelect;
        }

        #endregion

        #region Private methods

        private void ShutdownPromptReponseHandler(PromptResponse response)
        {
            if (response == PromptResponse.Yes)
                controlSystem.SystemPower = false;

            Render();
        }

        private void PasscodeEnteredHandler(int passcode)
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

        private string GenerateTopBarText()
        {
            switch (Category)
            {
                case Category.SourceSelect:
                    switch (sourceManager.Source)
                    {
                        case Source.Laptop: return "Laptop via HDMI Cable";
                        case Source.Wireless: return "Wireless Presentation";
                        case Source.Television: return "Free-to-air Television";
                        case Source.None: return "Source Selection";
                        default: return "Unhandled Source";
                    }
                case Category.MicLevels: return "Mic Levels";
                default: return "Unhandled Category";
            }
        }

        private void SourceUpdatedHandler(object sender, SourceUpdatedEventArgs e)
        {
            Trace("SourceUpdatedHandler() called. New source: " + e.Source);
            Render();
        }

        #endregion
    }

    public enum Category
    {
        SourceSelect,
        MicLevels,
    }
}
