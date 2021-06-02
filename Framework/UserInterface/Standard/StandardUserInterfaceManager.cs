using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using System;

namespace ATC.Framework.UserInterface.Standard
{
    public sealed class StandardUserInterfaceManager<TPanel> : UserInterfaceManager<TPanel>, IStandardUserInterface
        where TPanel : BasicTriListWithSmartObject
    {
        #region Fields

        private readonly StandardUserInterfaceComponent standardUi;

        #endregion

        #region Properties

        public override bool TraceEnabled
        {
            get { return base.TraceEnabled; }
            set
            {
                standardUi.TraceEnabled = value;
                base.TraceEnabled = value;
            }
        }

        /// <summary>
        /// The join offset to apply to the standard user interface component.
        /// </summary>
        public uint JoinOffset
        {
            get { return standardUi.JoinOffset; }
            set { standardUi.JoinOffset = value; }
        }

        /// <summary>
        /// Get or set the currently active Page.
        /// </summary>
        public Page Page
        {
            get { return standardUi.Page; }
            set { standardUi.Page = value; }
        }

        /// <summary>
        /// Top Bar popup visibility
        /// </summary>
        public bool IsTopBarVisible
        {
            get { return standardUi.IsTopBarVisible; }
            set { standardUi.IsTopBarVisible = value; }
        }

        /// <summary>
        /// Top Bar popup text
        /// </summary>
        public string TopBarText
        {
            get { return standardUi.TopBarText; }
            set { standardUi.TopBarText = value; }
        }

        /// <summary>
        /// Menu button state and menu popup visibility.
        /// </summary>
        public bool IsMenuPopupActive
        {
            get { return standardUi.IsMenuPopupActive; }
            set { standardUi.IsMenuPopupActive = value; }
        }

        /// <summary>
        /// How long (in milliseconds) the menu popup should stay open before automatically closing.
        /// </summary>
        public long MenuPopupTimeout
        {
            get { return standardUi.MenuPopupTimeout; }
            set { standardUi.MenuPopupTimeout = value; }
        }

        /// <summary>
        /// Menu button visibility (not the menu popup).
        /// </summary>
        public bool IsMenuButtonVisible
        {
            get { return standardUi.IsMenuButtonVisible; }
            set { standardUi.IsMenuButtonVisible = value; }
        }

        /// <summary>
        /// Returns true if the notification popup is currently visible / active.
        /// </summary>
        public bool IsNotificationPopupActive
        {
            get { return standardUi.IsNotificationPopupActive; }
        }

        /// <summary>
        /// Returns true if prompt popup is currently active.
        /// </summary>
        public bool IsPromptPopupActive
        {
            get { return standardUi.IsPromptPopupActive; }
        }

        /// <summary>
        /// Date and time visibility.
        /// </summary>
        public bool IsDateTimeVisible
        {
            get { return standardUi.IsDateTimeVisible; }
            set { standardUi.IsDateTimeVisible = value; }
        }

        public bool IsPasscodePopupActive
        {
            get { return standardUi.IsPasscodePopupActive; }
        }

        #endregion

        #region Constructor

        public StandardUserInterfaceManager(TPanel panel)
            : base(panel)
        {
            standardUi = new StandardUserInterfaceComponent()
            {
                ComponentName = "StandardUi",
                Manager = this,
            };
        }

        #endregion

        #region Public methods

        public override bool Register(string sgdPath)
        {
            bool result = base.Register(sgdPath);
            if (result)
                standardUi.InvokeInitialize();

            return result;
        }

        public override bool Register(ISmartObject otherPanel)
        {
            bool result = base.Register(otherPanel);

            if (result)
                standardUi.InvokeInitialize();

            return result;
        }

        public override void Render()
        {
            standardUi.Render();
            base.Render();
        }

        public override void Reset()
        {
            standardUi.Reset();
            base.Reset();
        }

        /// <summary>
        /// Activates the notifcation popup for the default duration with the default title and specified text.
        /// </summary>
        /// <param name="notificationText">Notification text to display.</param>
        /// <returns>True on success, false on fail.</returns>
        public bool ActivateNotificationPopup(string notificationText)
        {
            return standardUi.ActivateNotificationPopup(notificationText);
        }

        /// <summary>
        /// Activates the notifcation popup for the default duration with the specified title and text.
        /// </summary>
        /// <param name="titleText">Title of the notification popup.</param>
        /// <param name="notificationText">Notification text to display.</param>
        /// <returns>True on success, false on fail.</returns>
        public bool ActivateNotificationPopup(string titleText, string notificationText)
        {
            return standardUi.ActivateNotificationPopup(titleText, notificationText);
        }

        /// <summary>
        /// Deactivate / close any active notification popup.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public bool DeactivateNotificationPopup()
        {
            return standardUi.DeactivateNotificationPopup();
        }

        public void ActivatePasscodePopup(string instructionsText, Action<int> responseHandler)
        {
            standardUi.ActivatePasscodePopup(instructionsText, responseHandler);
        }

        public void ActivatePasscodePopup(string instructionsText, bool showEntered, Action<int> responseHandler)
        {
            standardUi.ActivatePasscodePopup(instructionsText, showEntered, responseHandler);
        }

        public void SetWaitPageSequences(Sequence startupSequence, Sequence shutdownSequence)
        {
            standardUi.SetWaitPageSequences(startupSequence, shutdownSequence);
        }

        public void ActivatePromptPopup(string questionText, Action<PromptResponse> responseHandler)
        {
            standardUi.ActivatePromptPopup(questionText, responseHandler);
        }

        public void ActivatePromptPopup(string questionText, string yesText, string noText, Action<PromptResponse> responseHandler)
        {
            standardUi.ActivatePromptPopup(questionText, yesText, noText, responseHandler);
        }

        public void DeactivatePromptPopup()
        {
            standardUi.DeactivatePromptPopup();
        }

        #endregion

        #region Panel event handlers

        protected override void OnlineEventHandler(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            standardUi.OnlineEventHandler(args);
            base.OnlineEventHandler(currentDevice, args);
        }

        protected override void SigChangeHandler(BasicTriList device, SigEventArgs args)
        {
            // check first if standard ui can handle this
            bool eventHandled = standardUi.SigEventHandler(args);
            if (eventHandled) return;

            // let base class try and handle it
            base.SigChangeHandler(device, args);
        }

        protected override void SmartObjectHandler(GenericBase device, SmartObjectEventArgs args)
        {
            // check first if standard ui can handle this
            bool eventHandled = standardUi.SmartObjectEventHandler(args);
            if (eventHandled) return;

            // let base class try and handle it
            base.SmartObjectHandler(device, args);
        }

        #endregion
    }
}