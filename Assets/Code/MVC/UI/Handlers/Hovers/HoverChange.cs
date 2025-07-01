namespace Awaken.TG.MVC.UI.Handlers.Hovers
{
    /// <summary>
    /// Represents an object being hovered or dehovered by mouse.
    /// </summary>
    public class HoverChange 
    {
        // === Event data

        /// <summary>
        /// The view being hovered/dehovered.
        /// </summary>
        public View View { get; }

        /// <summary>
        /// The new hover state for the object.
        /// </summary>
        public bool Hovered { get; }

        // === Constructors

        public HoverChange(View view, bool hovered) {
            View = view;
            Hovered = hovered;
        }
    }
}
