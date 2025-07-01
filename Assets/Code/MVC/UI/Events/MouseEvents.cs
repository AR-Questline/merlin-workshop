using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.UI.Handlers.Drags;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Events
{
    /// <summary>
    /// Base class for UIEvents related to mouse clicks/releases.
    /// </summary>
    public abstract class UIMouseButtonEvent : UIEvent {
        public int Button { get; set; } = 0;
        public EventModifiers Modifiers { get; set; } = EventModifiers.None;

        [UnityEngine.Scripting.Preserve] public bool IsLeft => Button == 0;
        [UnityEngine.Scripting.Preserve] public bool IsRight => Button == 1;
        [UnityEngine.Scripting.Preserve] public bool IsMiddle => Button == 2;
    }

    /// <summary>
    /// Sent when a mouse button is pressed down over an object.
    /// </summary>
    public class UIEMouseDown : UIMouseButtonEvent, ISubmit {
        /// <summary>
        /// Converts this event into a drag attempt.
        /// </summary>
        public UIResult TransformIntoDrag(IUIAware handler, IEnumerable<IUIAware> dragTargets = null) {
            GameUI.Element<Dragging>().InitiateDrag(handler, this, dragTargets ?? Enumerable.Empty<IUIAware>());
            return UIResult.Prevent;
        }
    }

    /// <summary>
    /// Sent when a mouse button is held down over an object.
    /// This event is not sent in the first frame of button pressed, but on every following frame.
    /// </summary>
    public class UIEMouseHeld : UIMouseButtonEvent {
    }

    public class UIEMouseLongHeld : UIEMouseHeld {
    }

    /// <summary>
    /// Sent when a mouse button is released over an object AND it was earlier pressed over an object. This means that
    /// you only get this event if the user both pressed and released the button over you.
    /// </summary>
    public class UIEMouseUp : UIMouseButtonEvent {        
    }

    public class UIEMouseUpLong : UIEMouseUp {
    }

    /// <summary>
    /// Sent when mouse scroll wheel is moved
    /// </summary>
    public class UIEMouseScroll : UIMouseButtonEvent {
        public float Value { get; set; }
    }

    /// <summary>
    /// Sent on second UIEMouseDown performed within short amount of time 
    /// </summary>
    public class UIEMouseDoubleClick : UIMouseButtonEvent {
    }

    /// <summary>
    /// All drag events include the start position in addition to everything else.
    /// </summary>
    public abstract class UIEDrag : UIMouseButtonEvent {
        public UIPosition StartPosition { [UnityEngine.Scripting.Preserve] get; set; }
        public UIPosition PreviousPosition { get; set; }
        [UnityEngine.Scripting.Preserve] public IUIAware Dragged { get; set; }
        public IUIAware CurrentTarget { get; set; }

        // === Helpers
    
        public T TargetModel<T>() where T : class {
            return (CurrentTarget as IView)?.GenericTarget as T;
        }
    }

    /// <summary>
    /// Sent to object that might be dragged, to actually ask it if it wants to be dragged. 
    /// </summary>
    public class UIETryStartDrag : UIEDrag { }
    
    /// <summary>
    /// Sent when the drag is definitely started - it was accepted and the user has moved the mouse far enough that
    /// we know it's actually a drag.
    /// </summary>
    public class UIEStartDrag : UIEDrag { }

    /// <summary>
    /// Sent during the drag to notify the object of its new position.
    /// </summary>
    public class UIEDraggedTo : UIEDrag { }

    /// <summary>
    /// Sent when the mouse is released at the end of a drag.
    /// </summary>
    public class UIEEndDrag : UIEDrag {
        public bool consumeMouseUpEvent = true;
    }

    /// <summary>
    /// Sent after drop target accepted drag drop
    /// </summary>
    public class UIEDroppedOn: UIEDrag { }

    /// <summary>
    /// Sent to all potential drag targets on drag init
    /// </summary>
    public class UIEStartDragTarget : UIEDrag { }

    /// <summary>
    /// Sent to all drag targets on drag finish
    /// </summary>
    public class UIEEndDragTarget : UIEDrag { }

    /// <summary>
    /// Sent every frame to current drag target
    /// </summary>
    public class UIEHoveredByDrag : UIEDrag { }

    public class UIEDroppedOntoByDrag : UIEDrag { }

    /// <summary>
    /// Delivered each frame as long as the object is being pointed to by the mouse. Accepting this event means that you'd like to receive hover.
    /// </summary>
    public class UIEPointTo : UIEvent, IHover {
    }
}
