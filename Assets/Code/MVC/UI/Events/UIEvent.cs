using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.MVC.UI.Events
{
    /// <summary>
    /// Base class for all UIEvents, providing basic functionality related to event propagation
    /// in the interaction stack.
    /// </summary>
    public abstract class UIEvent : IEvent {

        // === State

        public GameUI GameUI { get; set; }
        public UIPosition Position { get; set; }

        // === Queries

        /// <summary>
        /// Returns all handlers that will receive this event after a given object.
        /// Used to peek into the interaction stack to modify one's own behavior based
        /// on items below, for example to allow the hero to be selected even if he/she
        /// is covered by a location collider.
        /// </summary>
        public IEnumerable<IUIAware> ItemsBelow(IUIAware me) {
            return GameUI.MouseInteractionStack.SkipWhile(i => i != me).Skip(1);
        }
    }

    public interface IEvent {
        UIPosition Position { get; }
        IEnumerable<IUIAware> ItemsBelow(IUIAware me);
    }
    public interface ISubmit : IEvent { }
    public interface IHover : IEvent { }
}
