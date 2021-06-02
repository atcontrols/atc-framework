using System;

namespace ATC.Framework.UserInterface.Standard
{
    internal class Prompt
    {
        public string QuestionText { get; private set; }
        public string YesText { get; private set; }
        public string NoText { get; private set; }

        private readonly Action<PromptResponse> responseHandler;

        public Prompt(string questionText, Action<PromptResponse> responseHandler)
            : this(questionText, string.Empty, string.Empty, responseHandler)
        {
        }

        public Prompt(string questionText, string yesText, string noText, Action<PromptResponse> responseHandler)
        {
            QuestionText = questionText;
            YesText = yesText;
            NoText = noText;

            this.responseHandler = responseHandler;
        }

        public void InvokeReponseHandler(PromptResponse response)
        {
            if (responseHandler != null)
                responseHandler(response);
        }
    }

    public enum PromptResponse
    {
        Yes,
        No,
    }
}
