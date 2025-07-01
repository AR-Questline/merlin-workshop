using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI.Handlers.Selections
{
    /// <summary>
    /// Represents an object being selected or deselected.
    /// </summary>
    public class SelectionChange : UIEvent {
        // === Event data

        /// <summary>
        /// The object being (de)selected.
        /// </summary>
        public IModel Target { get; }

        /// <summary>
        /// The new selection state for the object.
        /// </summary>
        public bool Selected { get; }

        // === Constructors

        public SelectionChange(IModel target, bool selected) {
            this.Target = target;
            this.Selected = selected;
        }
    }
}
