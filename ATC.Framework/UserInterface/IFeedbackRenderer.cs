namespace ATC.Framework.UserInterface
{
    public interface IFeedbackRenderer
    {
        /// <summary>
        /// Renders feedback to touch panel.
        /// </summary>
        void Render();

        /// <summary>
        /// Reset any component variables to default values.
        /// </summary>
        void Reset();
    }
}
