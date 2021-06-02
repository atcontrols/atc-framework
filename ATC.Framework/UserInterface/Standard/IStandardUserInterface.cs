using System;

namespace ATC.Framework.UserInterface.Standard
{
    public interface IStandardUserInterface
    {
        // page
        Page Page { get; set; }

        // top bar
        bool IsTopBarVisible { get; set; }
        string TopBarText { get; set; }

        // menu
        bool IsMenuPopupActive { get; set; }
        long MenuPopupTimeout { get; set; }
        bool IsMenuButtonVisible { get; set; }

        // notification popup        
        bool IsNotificationPopupActive { get; }
        bool ActivateNotificationPopup(string notificationText);
        bool ActivateNotificationPopup(string titleText, string notificationText);
        bool DeactivateNotificationPopup();

        // prompt popup
        bool IsPromptPopupActive { get; }
        void ActivatePromptPopup(string questionText, Action<PromptResponse> responseHandler);
        void ActivatePromptPopup(string questionText, string yesText, string noText, Action<PromptResponse> responseHandler);
        void DeactivatePromptPopup();

        // date / time
        bool IsDateTimeVisible { get; set; }

        // passcode popup
        bool IsPasscodePopupActive { get; }
        void ActivatePasscodePopup(string instructionsText, Action<int> responseHandler);
        void ActivatePasscodePopup(string instructionsText, bool showEntered, Action<int> responseHandler);

        // wait page
        void SetWaitPageSequences(Sequence startupSequence, Sequence shutdownSequence);
    }
}
