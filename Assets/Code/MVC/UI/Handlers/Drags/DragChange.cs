namespace Awaken.TG.MVC.UI.Handlers.Drags
{
    /// <summary>
    /// Represents an object being dragged by mouse.
    /// </summary>
    public class DragChange 
    {
        // === Event data

        /// <summary>
        /// The view being dragged/stopped drag.
        /// </summary>
        public IView DraggedView { [UnityEngine.Scripting.Preserve] get; }

        /// <summary>
        /// The new drag state for the object.
        /// </summary>
        public bool Dragged { [UnityEngine.Scripting.Preserve] get; }

        /// <summary>
        /// Currently hovered drag target.
        /// </summary>
        public IUIAware HoveredTarget { [UnityEngine.Scripting.Preserve] get; }

        // === Constructors

        public DragChange(IView draggedView, bool dragged, IUIAware hoveredTarget) {
            DraggedView = draggedView;
            Dragged = dragged;
            HoveredTarget = hoveredTarget;
        }
    }
}
