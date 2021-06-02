namespace ATC.Framework.UserInterface.Standard
{
    public static class StandardJoin
    {
        // sounds
        internal static readonly DigitalJoin SoundAlertPlay = new DigitalJoin(6);
        internal static readonly DigitalJoin SoundAlertStop = new DigitalJoin(7);

        // pages
        internal static readonly DigitalJoin PageStart = new DigitalJoin(100);
        internal static readonly DigitalJoin PageWait = new DigitalJoin(101);
        internal static readonly DigitalJoin PageMain = new DigitalJoin(102);
        internal static readonly DigitalJoin PageSettings = new DigitalJoin(103);

        // notification popup
        internal static readonly DigitalJoin NotificationPopup = new DigitalJoin(110);
        internal static readonly DigitalJoin NotificationDismissButton = new DigitalJoin(111);
        internal static readonly SerialJoin NotificationTitle = new SerialJoin(110);
        internal static readonly SerialJoin NotificationText = new SerialJoin(111);

        // prompt popup
        internal static readonly DigitalJoin PromptPopup = new DigitalJoin(120);
        internal static readonly DigitalJoin PromptYesButton = new DigitalJoin(121);
        internal static readonly DigitalJoin PromptNoButton = new DigitalJoin(122);
        internal static readonly SerialJoin PromptQuestionText = new SerialJoin(120);
        internal static readonly SerialJoin PromptYesText = new SerialJoin(121);
        internal static readonly SerialJoin PromptNoText = new SerialJoin(122);

        // top bar / date and time
        internal static readonly DigitalJoin TopBarVisible = new DigitalJoin(130);
        internal static readonly DigitalJoin DateTimeVisible = new DigitalJoin(131);

        internal static readonly SerialJoin TopBarText = new SerialJoin(130);
        internal static readonly SerialJoin DateText = new SerialJoin(131);
        internal static readonly SerialJoin TimeText = new SerialJoin(132);

        // passcode popup
        internal static readonly DigitalJoin PasscodePopup = new DigitalJoin(140);
        internal static readonly DigitalJoin PasscodeInstructionVisible = new DigitalJoin(141);
        internal static readonly DigitalJoin PasscodeBackspaceButton = new DigitalJoin(142);
        internal static readonly DigitalJoin PasscodeBackspaceVisible = new DigitalJoin(143);
        internal static readonly DigitalJoin PasscodeDismissButton = new DigitalJoin(144);
        internal static readonly SerialJoin PasscodeText = new SerialJoin(140);
        internal static readonly SerialJoin PasscodeInstructionText = new SerialJoin(141);
        internal static readonly uint PasscodeSmartObjectId = 140;

        // menu
        internal static readonly DigitalJoin MenuButton = new DigitalJoin(150);
        internal static readonly DigitalJoin MenuVisible = new DigitalJoin(151);

        // wait page
        internal static readonly AnalogJoin WaitPageProgressBar = new AnalogJoin(160);
        internal static readonly SerialJoin WaitPageSystemStateText = new SerialJoin(160);
        internal static readonly SerialJoin WaitPageTimeRemainingText = new SerialJoin(161);
    }
}
