using System;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Targets {
    /// <summary>
    /// Handles ordering stuff around by right-clicking.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public partial class Targeting : Element<GameUI>, ISmartHandler {
        public sealed override bool IsNotSaved => true;

        // === Events

        public new static class Events {
            public static readonly Event<IOrderable, TargetChange> ReceivedNewTarget = new(nameof(ReceivedNewTarget));
            public static readonly Event<ITargetable, TargetChange> BecameTarget = new(nameof(BecameTarget));
            public static readonly Event<IOrderable, TargetChange> HoveredNewTarget = new(nameof(HoveredNewTarget));
            public static readonly Event<ITargetable, TargetChange> BecameHoveredTarget = new(nameof(BecameHoveredTarget));
        }

        // === Switching targets
        
        public void RetargetSelected(ITargetable target, UIMouseButtonEvent evt) =>
            RetargetSelected(target, evt.Position, evt.Modifiers);

        public void RetargetSelected(ITargetable target, UIPosition position = new UIPosition(), EventModifiers modifiers = EventModifiers.None, bool isDrag = false) {
            if (World.Only<Selection>().Selected is IOrderable subject) {
                TargetChange order = new TargetChange(subject, target, position, modifiers, isDrag);
                if (subject.AcceptTarget(order)) {
                    subject.Trigger(Events.ReceivedNewTarget, order);
                    target?.Trigger(Events.BecameTarget, order);
                }
            }
        }

        public bool HoverTargetable(ITargetable target, UIEPointTo pointTo = null) => HoverTargetable(target, pointTo?.Position ?? new UIPosition());

        public bool HoverTargetable(ITargetable target, UIPosition position) {
            if (World.Only<Selection>().Selected is IOrderable subject) {
                TargetChange order = new TargetChange(subject, target, position, EventModifiers.None);
                if (subject.AcceptTargetHover(order)) {
                    subject.Trigger(Events.HoveredNewTarget, order);
                    target?.Trigger(Events.BecameHoveredTarget, order);
                    return true;
                }
            }
            return false;
        }

        // === UI handling

        public UIResult BeforeDelivery(UIEvent evt) => UIResult.Ignore;
        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            if (handler is ITargetableView view && World.Only<Selection>().Selected is IOrderable) {
                ITargetable targetable = view.GenericTarget as ITargetable;
                if (targetable == null) {
                    throw new InvalidCastException($"Target of ITargetableView must implement ITargetable! View type: {view.GetType()}, target type: {view.GenericTarget.GetType()}");
                }
                if (evt is UIEMouseDown md && md.IsLeft) {
                    RetargetSelected(targetable, md);
                    return UIResult.Accept;
                }

                if (evt is UIEPointTo pointTo && HoverTargetable(targetable, pointTo)) {
                    return UIResult.Accept;
                }
            }

            return World.Only<Selection>().AfterHandlingBy(handler, evt);
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) => delivery;
    }
}