using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI.Handlers.Drags {
    public partial class Dragging : Element<GameUI>, ISmartHandler {
        public sealed override bool IsNotSaved => true;

        // === Properties

        public IUIAware Dragged => _currentDrag?.Handler;
        public IUIAware CurrentTarget => _currentDrag?.CurrentTarget;

        [UnityEngine.Scripting.Preserve] public bool IsDragged(object v) => Dragged == v;

        IView DraggedView => Dragged as IView;
        GameUI GameUI => ParentModel;

        // === State

        DragInProgress _currentDrag;

        // === Events

        public new static class Events {
            public static readonly Event<IView, DragChange> DragChanged = new(nameof(DragChanged));
            public static readonly Event<IView, DragChange> TargetChanged = new(nameof(TargetChanged));
        }

        // === Assign Dragged/Target item

        public bool InitiateDrag(IUIAware handler, UIEvent sourceEvent, IEnumerable<IUIAware> potentialTargets) {
            UIMouseButtonEvent mouseEvent = (UIMouseButtonEvent) sourceEvent;
            if (_currentDrag != null) {
                return false;
            } else {
                _currentDrag = new DragInProgress(handler, mouseEvent.Button, mouseEvent.Position, potentialTargets);
                return true;
            }
        }

        public void AssignCurrentTarget(IUIAware target) {
            if (CurrentTarget != target) {
                View oldTarget = CurrentTarget as View;
                _currentDrag.CurrentTarget = target;
                oldTarget?.Trigger(Events.TargetChanged, new DragChange(Dragged as IView, false, oldTarget as IUIAware));
                View newTarget = CurrentTarget as View;
                newTarget?.Trigger(Events.TargetChanged, new DragChange(Dragged as IView, true, CurrentTarget));
            }
        }

        // === ISmartHandler

        public UIResult BeforeDelivery(UIEvent evt) {
            switch (evt) {
                case UIEPointTo point: {
                    // update the current drag with new position
                    // at least if there is one

                    if (_currentDrag != null) {
                        foreach (var evtToDeliver in _currentDrag.Update(point.Position)) {
                            if (IsCurrentDragStillValid()) {
                                evtToDeliver.Deliver(GameUI);
                            } else {
                                _currentDrag = null;
                                break;
                            }
                        }
                    }

                    // but let others do their stuff
                    return UIResult.Ignore;
                }
                case UIEMouseUp mup when _currentDrag != null && _currentDrag.Button == mup.Button:
                    return EndDragging(mup.Position, false);
                case UISubmitAction submit when _currentDrag != null && RewiredHelper.IsGamepad:
                    return EndDragging(ParentModel.MousePosition, false);
                case UICancelAction cancel when _currentDrag != null && RewiredHelper.IsGamepad:
                    return EndDragging(ParentModel.MousePosition, true);
                case UIEStartDrag _:
                    DraggedView?.Trigger(Events.DragChanged, new DragChange(DraggedView, true, null));
                    return UIResult.Ignore;
                default: {
                    return UIResult.Ignore;
                }
            }

            bool IsCurrentDragStillValid() {
                return _currentDrag is { Handler: not null } && _currentDrag.Handler switch {
                    IModel { HasBeenDiscarded: true } or IView { HasBeenDiscarded: true } => false,
                    _ => true
                };
            }
        }

        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            if (RewiredHelper.IsGamepad) {
                return TryStartGamepadDrag(handler, evt);
            } else {
                return TryStartMouseDrag(handler, evt);
            }
        }

        UIResult TryStartGamepadDrag(IUIAware handler, UIEvent evt) {
            if (_currentDrag == null && handler is IDraggableView && evt is UISubmitAction) {
                UIResult check = handler.Handle(new UIETryStartDrag {GameUI = ParentModel});
                if (check == UIResult.Prevent) {
                    return UIResult.Ignore;
                }
                var potentialTargets = handler is IDragNDropView dragNDrop ? dragNDrop.DragTargets() : Enumerable.Empty<IUIAware>();
                _currentDrag = new DragInProgress(handler, 0, ParentModel.MousePosition, potentialTargets);
            }
            return UIResult.Ignore;
        }

        UIResult TryStartMouseDrag(IUIAware handler, UIEvent evt) {
            if (evt is UIEMouseDown md && handler != null) {
                UIResult check = handler.Handle(new UIETryStartDrag {GameUI = ParentModel});
                if (check == UIResult.Prevent) {
                    return UIResult.Ignore;
                }
                if (handler is IDragNDropView dragNDrop) {
                    return md.TransformIntoDrag(handler, dragNDrop.DragTargets());
                }
                if (handler is IDraggableView) {
                    return md.TransformIntoDrag(handler);
                }
            }

            return UIResult.Ignore;
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            if (delivery.handledEvent is UIEHoveredByDrag) {
                if (delivery.finalResult == UIResult.Accept) {
                    AssignCurrentTarget(delivery.responsibleObject);
                } else {
                    AssignCurrentTarget(null);
                }
            }
            return delivery;
        }

        UIResult EndDragging(UIPosition position, bool cancelled) {
            // end the drag
            var endingDrag = _currentDrag;
            _currentDrag = null;

            // remove target if cancelled
            if (cancelled) {
                endingDrag.CurrentTarget = null;
            }

            // deliver end drag UI events
            bool consumeEvent = false;
            foreach (var evtToDeliver in endingDrag.End(position)) {
                UIEventDelivery delivery = evtToDeliver.Deliver(GameUI);
                if (delivery.handledEvent is UIEEndDrag {consumeMouseUpEvent : true}) {
                    // Event is consumed if Dragging was Triggered (mouse was moved far enough) and Handler didn't switch off 'consumeMouseUpEvent'
                    consumeEvent = true;
                }
            }

            // trigger event if possible
            (endingDrag.Handler as IView)?.Trigger(Events.DragChanged, new DragChange((IView) endingDrag.Handler, false, endingDrag.CurrentTarget));
            
            return consumeEvent ? UIResult.Prevent : UIResult.Ignore;
        }
    }
}