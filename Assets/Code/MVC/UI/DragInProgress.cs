using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI {
    /// <summary>
    /// Represents the currently active drag operation. Only one drag is active at a time.
    /// </summary>
    public class DragInProgress {
        // === Constants

        /// <summary>
        /// How many pixels the mouse must move before we trigger a drag rised to the squere.
        /// </summary>
        public const float DragThresholdSqr = 4f;

        // === State

        public IUIAware Handler { get; }
        public IUIAware CurrentTarget { get; set; }
        public int Button { get; }
        public bool Triggered { get; private set; }

        UIPosition _startPosition;
        UIPosition _currentPosition;
        UIPosition _previousPosition;

        List<IUIAware> _potentialTargets;

        // === Constructors

        public DragInProgress(IUIAware handler, int button, UIPosition startPosition, IEnumerable<IUIAware> targets) {
            Handler = handler;
            Button = button;
            _startPosition = startPosition;
            _currentPosition = startPosition;
            _previousPosition = startPosition;
            Triggered = false;

            _potentialTargets = targets.ToList();
        }

        // === Operations

        public IEnumerable<EventToDeliver> Update(UIPosition position) {
            _previousPosition = _currentPosition;
            _currentPosition = position;
            if (!Triggered) {
                float distanceSqr = (_currentPosition.screen - _startPosition.screen).sqrMagnitude;
                // check if should start drag
                if (RewiredHelper.IsGamepad || distanceSqr >= DragThresholdSqr) {
                    Triggered = true;

                    // start drag
                    yield return CreateEventForHandler(new UIEStartDrag(), Handler);
                    foreach (var target in _potentialTargets) {
                        yield return CreateEventForHandler(new UIEStartDragTarget(), target);
                    }
                }
            } else {
                // continue drag
                yield return CreateEventForHandler(new UIEDraggedTo(), Handler);
                yield return CreateEventForStack(new UIEHoveredByDrag(), _potentialTargets, UIContext.Mouse);
            }
        }

        public IEnumerable<EventToDeliver> End(UIPosition position) {
            _previousPosition = _currentPosition;
            _currentPosition = position;
            if (Triggered) {
                yield return CreateEventForHandler(new UIEEndDrag(), Handler);
                foreach (var target in _potentialTargets) {
                    yield return CreateEventForHandler(new UIEEndDragTarget(), target);
                }

                if (CurrentTarget != null) {
                    yield return CreateEventForHandler(new UIEDroppedOntoByDrag(), CurrentTarget);
                }
                yield return CreateEventForHandler(new UIEDroppedOn(), Handler);
            }
        }

        // === Event to deliver

        public struct EventToDeliver {
            public UIEvent Event { get; set; }
            public Action<List<IUIAware>> ModifyStack { get; set; }
            public UIContext Context { get; set; }

            public UIEventDelivery Deliver(GameUI gameUI) {
                List<IUIAware> stack = new();
                gameUI.DetermineInteractionStack(gameUI.MousePosition, Context, stack);
                ModifyStack?.Invoke(stack);

                Event.GameUI = gameUI;
                return gameUI.DeliverEvent(Event, null, stack);
            }
        }

        // === Creation helpers

        EventToDeliver CreateEventForHandler(UIEDrag evt, IUIAware target) {
            EventToDeliver evtToDeliver = CreateBaseEventToDeliver(evt, UIContext.None);
            evtToDeliver.ModifyStack = stack => stack.Add(target);
            return evtToDeliver;
        }

        EventToDeliver CreateEventForStack(UIEDrag evt, IEnumerable<IUIAware> targets, UIContext context) {
            EventToDeliver evtToDeliver = CreateBaseEventToDeliver(evt, context);
            evtToDeliver.ModifyStack = stack => {
                var targetsSet = targets.ToHashSet();
                for (int i = stack.Count-1; i >= 0; i--) {
                    if (!targetsSet.Contains(stack[i])) {
                        stack.RemoveAt(i);
                    }
                }
            };
            return evtToDeliver;
        }

        EventToDeliver CreateBaseEventToDeliver(UIEDrag evt, UIContext context) {
            evt.StartPosition = _startPosition;
            evt.PreviousPosition = _previousPosition;
            evt.Position = _currentPosition;
            evt.Button = Button;
            evt.Dragged = Handler;
            evt.CurrentTarget = CurrentTarget;

            return new EventToDeliver {
                Event = evt,
                Context = context,
            };
        }
    }
}