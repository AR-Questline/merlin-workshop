using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Targets
{
    /// <summary>
    /// Represents an order made in the UI by right clicking.
    /// </summary>
    public class TargetChange {
        // === Information

        public IOrderable ordered;
        public ITargetable targeted;
        [UnityEngine.Scripting.Preserve] public UIPosition position;
        [UnityEngine.Scripting.Preserve] public EventModifiers keyModifiers;
        [UnityEngine.Scripting.Preserve] public bool isDrag;

        // === Constructors

        public TargetChange(IOrderable ordered, ITargetable targeted, UIPosition position, EventModifiers keyModifiers, bool isDrag = false) {
            this.ordered = ordered;
            this.targeted = targeted;
            this.position = position;
            this.keyModifiers = keyModifiers;
            this.isDrag = isDrag;
        }

        public override string ToString() => $"UIOrderEvent:{ordered}->{targeted}";
    }
}
