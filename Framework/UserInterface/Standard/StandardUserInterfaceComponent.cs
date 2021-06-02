using Crestron.SimplSharp;
using System;

namespace ATC.Framework.UserInterface.Standard
{
    internal class StandardUserInterfaceComponent : UserInterfaceComponent, IStandardUserInterface
    {
        #region Component methods
        protected override void Initialize()
        {
            // pages
            AddJoinListener(StandardJoin.PageStart);
            AddJoinListener(StandardJoin.PageWait);
            AddJoinListener(StandardJoin.PageMain);
            AddJoinListener(StandardJoin.PageSettings);

            // notification popup
            AddJoinListener(StandardJoin.NotificationDismissButton);

            // prompt popup
            AddJoinListener(StandardJoin.PromptYesButton);
            AddJoinListener(StandardJoin.PromptNoButton);

            // passcode popup
            AddJoinListener(StandardJoin.PasscodeBackspaceButton);
            AddJoinListener(StandardJoin.PasscodeDismissButton);

            // top bar / menu
            AddJoinListener(StandardJoin.MenuButton);

            // add smart objects
            AddSmartObjectListener(StandardJoin.PasscodeSmartObjectId);
        }

        protected override void OnlineStateHandler(bool panelOnline)
        {
            Trace("OnlineStateHandler() panel online: " + panelOnline);
            if (panelOnline)
            {
                Render();
                dateTimeTimer = new CTimer(OneSecondCallback, null, 0, 1000); // create a timer than invokes a callback every second
            }
            else
            {
                if (dateTimeTimer != null)
                {
                    dateTimeTimer.Stop();
                    dateTimeTimer.Dispose();
                    dateTimeTimer = null;
                }
            }
        }

        protected override void DigitalJoinHandler(DigitalJoin join)
        {
            if (join.Value == DigitalJoinState.Pressed)
            {
                // page joins                
                if (join == StandardJoin.PageStart || join == StandardJoin.PageWait || join == StandardJoin.PageMain || join == StandardJoin.PageSettings)
                {
                    PageJoinHandler(join);
                }

                // notification dismiss button
                else if (join == StandardJoin.NotificationDismissButton)
                {
                    DeactivateNotificationPopup();
                }

                // prompt popup buttons
                else if (join == StandardJoin.PromptYesButton || join == StandardJoin.PromptNoButton)
                {
                    PromptButtonHandler(join);
                }

                else if (join == StandardJoin.PasscodeDismissButton)
                    IsPasscodePopupActive = false;
                else if (join == StandardJoin.PasscodeBackspaceButton)
                    PasscodeBackspaceButtonHandler();
                else if (join == StandardJoin.MenuButton)
                    IsMenuPopupActive = !IsMenuPopupActive;
            }
        }

        protected override void SmartObjectBoolHandler(uint smartObjectId, uint number, string name, DigitalJoinState state)
        {
            if (smartObjectId == StandardJoin.PasscodeSmartObjectId && state == DigitalJoinState.Pressed)
                PasscodeKeypadHandler(name);
        }

        public override void Render()
        {
            PageRender();
            TopBarRender();
            MenuRender();
            NotificationPopupRender();
            PromptPopupRender();
            PasscodeRender();
            WaitPageRender();
        }

        public override void Reset()
        {
            Page = Page.Start;
        }
        #endregion

        #region Page
        protected Page page = Page.Start;

        public Page Page
        {
            get { return page; }
            set
            {
                page = value;
                Trace("Page set to: " + value);
                PageRender();
            }
        }

        private void PageJoinHandler(DigitalJoin join)
        {
            bool pageUpdated = false;

            if (join == StandardJoin.PageStart)
            {
                page = Page.Start;
                pageUpdated = true;
            }
            else if (join == StandardJoin.PageWait)
            {
                page = Page.Wait;
                pageUpdated = true;
            }
            else if (join == StandardJoin.PageMain)
            {
                page = Page.Main;
                pageUpdated = true;
            }
            else if (join == StandardJoin.PageSettings)
            {
                page = Page.Settings;
                pageUpdated = true;
            }

            // show page updated message
            if (pageUpdated)
            {
                Trace("PageJoinHandler() page set to: " + page.Name);
                Manager.Render();
            }
        }

        protected virtual void PageRender()
        {
            // page joins
            SetJoin(Page.Start.Join, Page == Page.Start);
            SetJoin(Page.Wait.Join, Page == Page.Wait);
            SetJoin(Page.Main.Join, Page == Page.Main);
            SetJoin(Page.Settings.Join, Page == Page.Settings);
        }
        #endregion

        #region Top Bar
        private bool topBarVisible = true;
        public bool IsTopBarVisible
        {
            get { return topBarVisible; }
            set
            {
                topBarVisible = value;
                TopBarRender();
            }
        }

        private string topBarText = string.Empty;
        public string TopBarText
        {
            get { return topBarText; }
            set
            {
                if (value == null)
                    topBarText = string.Empty;
                else
                    topBarText = value;
                TopBarRender();
            }
        }

        private void TopBarRender()
        {
            SetJoin(StandardJoin.TopBarVisible, topBarVisible);
            SetJoin(StandardJoin.TopBarText, topBarText);
        }
        #endregion

        #region Menu
        private long menuPopupTimeout = 5000;
        public long MenuPopupTimeout
        {
            get { return menuPopupTimeout; }
            set { menuPopupTimeout = value; }
        }

        private CTimer menuPopupTimer;

        public bool IsMenuPopupActive
        {
            get { return menuPopupTimer != null; }
            set
            {
                if (value)
                    MenuPopupTimerCreate();
                else
                    MenuPopupTimerDestroy();
            }
        }

        private bool menuVisible = true;
        public bool IsMenuButtonVisible
        {
            get { return menuVisible; }
            set
            {
                menuVisible = value;
                MenuRender();
            }
        }

        private void MenuRender()
        {
            SetJoin(StandardJoin.MenuButton, IsMenuPopupActive);
            SetJoin(StandardJoin.MenuVisible, IsMenuButtonVisible);
        }

        private void MenuPopupTimerCreate()
        {
            menuPopupTimer = new CTimer(MenuPopupTimeoutCallback, MenuPopupTimeout);
            MenuRender();
        }

        private void MenuPopupTimerDestroy()
        {
            if (menuPopupTimer != null)
            {
                menuPopupTimer.Stop();
                menuPopupTimer.Dispose();
                menuPopupTimer = null;

                MenuRender();
            }
        }

        private void MenuPopupTimeoutCallback(object o)
        {
            IsMenuPopupActive = false;
        }
        #endregion

        #region Notification popup
        private CTimer notificationPopupTimer;
        private const long NotificationPopupTimeoutDuration = 5000;
        private string notificationTitle = string.Empty;
        private string notificationText = string.Empty;

        /// <summary>
        /// Returns true if the notification popup is currently visible / active.
        /// </summary>
        public bool IsNotificationPopupActive
        {
            get { return notificationPopupTimer != null; }
        }

        /// <summary>
        /// Activates the notifcation popup for the default duration with the default title and specified text.
        /// </summary>
        /// <param name="text">Notification text to display.</param>
        /// <returns>True on success, false on fail.</returns>
        public bool ActivateNotificationPopup(string text)
        {
            return ActivateNotificationPopup("Notification", text);
        }

        /// <summary>
        /// Activates the notifcation popup for the default duration with the specified title and text.
        /// </summary>
        /// <param name="title">Title of the notification popup.</param>
        /// <param name="text">Notification text to display.</param>
        /// <returns>True on success, false on fail.</returns>
        public bool ActivateNotificationPopup(string title, string text)
        {
            try
            {
                if (!IsNotificationPopupActive)
                {
                    Trace(String.Format("NotificationPopupActivate() title: \"{0}\", text: \"{1}\"", title, text));

                    // create new timer
                    notificationPopupTimer = new CTimer(NotificationPopupTimeoutHandler, NotificationPopupTimeoutDuration);

                    // set text
                    notificationTitle = title;
                    notificationText = text;

                    // update panel                    
                    PulseJoin(StandardJoin.SoundAlertPlay);
                    NotificationPopupRender();

                    return true;
                }
                else
                {
                    Trace("NotificationPopupActivate() notification popup is already active.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("NotificationPopupActivate() exception caught.", ex);
                return false;
            }
        }

        /// <summary>
        /// Deactivate / close any active notification popup.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public bool DeactivateNotificationPopup()
        {
            try
            {
                if (IsNotificationPopupActive)
                {
                    Trace("NotificationPopupDeactivate() deactivating notification popup.");

                    // stop and cleanup timer
                    if (notificationPopupTimer != null)
                    {
                        notificationPopupTimer.Stop();
                        notificationPopupTimer.Dispose();
                        notificationPopupTimer = null;
                    }

                    // set text
                    notificationTitle = string.Empty;
                    notificationText = string.Empty;

                    // update panel
                    PulseJoin(StandardJoin.SoundAlertStop);
                    NotificationPopupRender();

                    return true;
                }
                else
                {
                    Trace("NotificationPopupDeactivate() notification popup is not active.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("NotificationPopupDeactivate() exception caught.", ex);
                return false;
            }
        }

        private void NotificationPopupRender()
        {
            SetJoin(StandardJoin.NotificationPopup, IsNotificationPopupActive);
            SetJoin(StandardJoin.NotificationTitle, notificationTitle);
            SetJoin(StandardJoin.NotificationText, notificationText);
        }

        private void NotificationPopupTimeoutHandler(object o)
        {
            Trace("NotificationPopupTimeoutHandler() timeout reached.");
            DeactivateNotificationPopup();
        }
        #endregion

        #region Prompt popup
        private Prompt prompt;

        public bool IsPromptPopupActive
        {
            get { return prompt != null; }
        }

        public void ActivatePromptPopup(string questionText, Action<PromptResponse> responseHandler)
        {
            ActivatePromptPopup(questionText, string.Empty, string.Empty, responseHandler);
        }

        public void ActivatePromptPopup(string questionText, string yesText, string noText, Action<PromptResponse> responseHandler)
        {
            if (IsPromptPopupActive)
            {
                TraceError("PromptPopupActivate() another prompt popup is already active. No action taken.");
            }
            else
            {
                if (responseHandler == null)
                {
                    TraceError("PromptPopupActivate() response handler is null.");
                    return;
                }
                Trace(string.Format("PromptPopupActivate() activation prompt popup. Question: \"{0}\", yes text: \"{1}\", no text: \"{2}\"", questionText, yesText, noText));
                prompt = new Prompt(questionText, yesText, noText, responseHandler);
                Render();
            }
        }

        public void DeactivatePromptPopup()
        {
            if (IsPromptPopupActive)
            {
                Trace("PrompPopupDeactivate() deactivating prompt popup.");
                prompt = null;
                Render();
            }
            else
                TraceWarning("PrompPopupDeactivate() called but prompt popup is not active.");
        }

        /// <summary>
        /// Handles prompt popup's Yes and No buttons and dispatches correct actions.
        /// </summary>
        /// <param name="join">The button digital join number.</param>
        private void PromptButtonHandler(DigitalJoin join)
        {
            if (prompt == null)
            {
                TraceError("PromptButtonHandler() called but prompt is null. No action taken.");
                return;
            }

            if (join == StandardJoin.PromptYesButton)
            {
                Trace("PromptButtonHandler() invoking yes response.");
                prompt.InvokeReponseHandler(PromptResponse.Yes);
            }
            else if (join == StandardJoin.PromptNoButton)
            {
                Trace("PromptButtonHandler() invoking no response.");
                prompt.InvokeReponseHandler(PromptResponse.No);
            }
            else
            {
                TraceError("PromptButtonHandler() called but no join regonised.");
            }

            DeactivatePromptPopup();
        }

        /// <summary>
        /// Updates text on the panel relating to the prompt popup.
        /// </summary>
        private void PromptPopupRender()
        {
            if (prompt == null)
            {
                SetJoin(StandardJoin.PromptPopup, false);
                SetJoin(StandardJoin.PromptQuestionText, string.Empty);
                SetJoin(StandardJoin.PromptYesText, string.Empty);
                SetJoin(StandardJoin.PromptNoText, string.Empty);
            }
            else
            {
                SetJoin(StandardJoin.PromptPopup, true);
                SetJoin(StandardJoin.PromptQuestionText, prompt.QuestionText);
                SetJoin(StandardJoin.PromptYesText, prompt.YesText);
                SetJoin(StandardJoin.PromptNoText, prompt.NoText);
            }
        }
        #endregion

        #region Date and time
        private CTimer dateTimeTimer;

        private bool dateTimeVisible;
        public bool IsDateTimeVisible
        {
            get { return dateTimeVisible; }
            set
            {
                dateTimeVisible = value;
                DateAndTimeRender();
            }
        }

        private void OneSecondCallback(object o)
        {
            DateAndTimeRender();
        }

        protected virtual void DateAndTimeRender()
        {
            SetJoin(StandardJoin.DateTimeVisible, IsDateTimeVisible);

            if (IsDateTimeVisible)
            {
                // update date string
                string date = String.Format("{0}, {1}/{2}/{3}", DateTime.Now.DayOfWeek, DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
                SetJoin(StandardJoin.DateText, date);

                // update time string
                string time = DateTime.Now.ToLongTimeString();
                SetJoin(StandardJoin.TimeText, time);
            }
        }
        #endregion

        #region Passcode popup
        private string passcodeEntered = string.Empty;
        private bool passcodeShowEntered;
        private string passcodeInstructionsText;
        private const int PasscodeMaxLength = 6;
        private Action<int> passcodeResponseHandler;

        private bool passcodePopupActive;
        public bool IsPasscodePopupActive
        {
            get { return passcodePopupActive; }
            private set
            {
                passcodePopupActive = value;
                PasscodeRender();
            }
        }

        public void ActivatePasscodePopup(string instructionsText, Action<int> responseHandler)
        {
            ActivatePasscodePopup(instructionsText, false, responseHandler);
        }

        public void ActivatePasscodePopup(string instructionsText, bool showEntered, Action<int> responseHandler)
        {
            passcodeInstructionsText = instructionsText;
            passcodeShowEntered = showEntered;
            passcodeResponseHandler = responseHandler;
            passcodeEntered = string.Empty;

            // show passcode popup
            IsPasscodePopupActive = true;
        }

        private void PasscodeClearEntered()
        {
            passcodeEntered = string.Empty;
            PasscodeRender();
        }

        private void PasscodeKeypadHandler(string name)
        {
            switch (name)
            {
                case "0":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    if (passcodeEntered.Length < PasscodeMaxLength)
                    {
                        passcodeEntered = passcodeEntered + name;
                        PasscodeRender();
                    }
                    break;
                case "Misc_1": // clear button
                    PasscodeClearEntered();
                    break;
                case "Misc_2": // ok button

                    if (passcodeEntered == string.Empty)
                    {
                        ActivateNotificationPopup("Invalid input", "No passcode entered.");
                        return;
                    }

                    // convert string to int
                    int passcode = Int32.Parse(passcodeEntered);

                    // invoke response handler
                    if (passcodeResponseHandler != null)
                        passcodeResponseHandler(passcode);

                    // clear passcode and close popup
                    PasscodeClearEntered();
                    IsPasscodePopupActive = false;

                    break;
                default:
                    Trace("PasscodeKeypadHandler() unhandled button: " + name);
                    break;
            }
        }

        private void PasscodeBackspaceButtonHandler()
        {
            if (passcodeEntered.Length > 0)
            {
                passcodeEntered = passcodeEntered.Substring(0, passcodeEntered.Length - 1);
                PasscodeRender();
            }
        }

        private void PasscodeRender()
        {
            SetJoin(StandardJoin.PasscodePopup, IsPasscodePopupActive);
            SetJoin(StandardJoin.PasscodeBackspaceVisible, passcodeEntered.Length > 0);

            // passcode text
            if (passcodeShowEntered)
                SetJoin(StandardJoin.PasscodeText, passcodeEntered);
            else
                SetJoin(StandardJoin.PasscodeText, PasscodeFormatString(passcodeEntered));

            // passcode instructions
            bool passcodeInstructionsVisible = passcodeEntered == string.Empty;
            SetJoin(StandardJoin.PasscodeInstructionVisible, passcodeInstructionsVisible);
            SetJoin(StandardJoin.PasscodeInstructionText, passcodeInstructionsText);
        }

        private string PasscodeFormatString(string passcode)
        {
            try
            {
                string returnString = String.Empty;

                if (passcode == null || passcode.Length == 0)
                    returnString = String.Empty;
                else if (passcode.Length > 0)
                    foreach (var c in passcode)
                        returnString += "*";

                return returnString;
            }
            catch (Exception e)
            {
                TraceException("FormatPasscodeString() exception caught: ", e);
                return String.Empty;
            }
        }
        #endregion

        #region Wait page
        private ushort waitPageProgressValue = 0;
        private string waitPageSystemState = string.Empty;
        private string waitPageTimeRemaining = string.Empty;

        private Sequence waitPageStartupSequence, waitPageShutdownSequence;

        public void SetWaitPageSequences(Sequence startupSequence, Sequence shutdownSequence)
        {
            if (startupSequence == null || shutdownSequence == null)
            {
                TraceError("WaitPageSetSequences() error setting sequences - cannot be null.");
                return;
            }

            Trace("WaitPageSetSequences() setting startup and shutdown sequences.");

            waitPageStartupSequence = startupSequence;
            waitPageShutdownSequence = shutdownSequence;

            // add progress callbacks
            waitPageStartupSequence.ProgressCallback += new EventHandler<SequenceProgressEventArgs>(WaitPageSequenceProgressCallback);
            waitPageShutdownSequence.ProgressCallback += new EventHandler<SequenceProgressEventArgs>(WaitPageSequenceProgressCallback);
        }

        private void WaitPageSequenceProgressCallback(object sender, SequenceProgressEventArgs e)
        {
            // system state
            if (sender == waitPageStartupSequence)
                waitPageSystemState = "Starting up";
            else if (sender == waitPageShutdownSequence)
                waitPageSystemState = "Shutting down";
            else
            {
                TraceError("WaitPageSequenceProgressCallback() unhandled sender. Aborting.");
                return;
            }

            // progress bar value
            waitPageProgressValue = e.Uint16Percentage;

            // time remaining text
            int secondsRemaining = (int)((e.TotalTime - e.ElapsedTime + 900) / 1000);
            waitPageTimeRemaining = String.Format("{0} seconds remaining.", secondsRemaining);

            WaitPageRender();
        }

        /// <summary>
        /// Updates text and wait bar on wait page.
        /// </summary>
        private void WaitPageRender()
        {
            SetJoin(StandardJoin.WaitPageSystemStateText, waitPageSystemState);
            SetJoin(StandardJoin.WaitPageProgressBar, waitPageProgressValue);
            SetJoin(StandardJoin.WaitPageTimeRemainingText, waitPageTimeRemaining);
        }
        #endregion
    }
}
