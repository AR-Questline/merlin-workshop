using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;
using Awaken.TG.MVC.UI.Handlers.DoubleClicks;
using Awaken.TG.MVC.UI.Handlers.Drags;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility.Cameras;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using NUnit.Framework;
using Rewired;
using UnityEngine;

namespace Awaken.TG.MVC.UI {
    /// <summary>
    /// Main class for handling all UI-related concerns in Tainted Grail.
    /// </summary>
    public partial class GameUI : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        float _longPressTime;
        // === State

        public UIPosition MousePosition { get; private set; }
        ModelsSet<ISmartHandler>? _smartHandlers;
        ModelsSet<ISmartHandler> SmartHandlers => _smartHandlers ??= Elements<ISmartHandler>();

        public IEnumerable<IUIAware> MouseInteractionStack => _mouseInteractionStack;

        readonly List<InputAction> _inputActions = null;//ReInput.mapping.ActionCategories.SelectMany(actCat => ReInput.mapping.ActionsInCategory(actCat.id, true)).ToList();

        readonly List<IUIAware> _mouseInteractionStack = new List<IUIAware>();
        readonly List<IUIAware> _keyboardInteractionStack = new List<IUIAware>();
        readonly Dictionary<string, float> _mouseHeldTime = new();
        readonly List<IUIHandlerSource> _uiHandlerSourcesSorted = new(8);

        int _lastPositionUpdate = int.MinValue;
        CameraHandle _camera;
        public Camera MainCamera => _camera.Camera;

        Focus Focus => Element<Focus>();
        
        // === Profiling
        
        public GameUIProfiler Profiler { get; } = new GameUIProfiler();

        // === Constructors

        protected override void OnInitialize() {
            // create event sources
            AddElement(new RaycastHandlerSource());

            // create handlers for specific UI domains
            //AddElement(new Targeting());
            AddElement(new Selection());
            AddElement(new TooltipHandler());
            AddElement(new Hovering());
            AddElement(new Dragging());
            AddElement(new DoubleClick());
            AddElement(new CheatController());
            AddElement(new Focus());
            AddElement(new PlayerInput());

            Prepare();
        }

        void Prepare() {
            _longPressTime = 0;//RewiredHelper.Player.controllers.maps.GetInputBehavior(0).buttonLongPressTime;
            _camera = World.Only<CameraStateStack>().MainHandle;
            ModelUtils.ListenToFirstModelOfType<GameControls, Setting>(Setting.Events.SettingRefresh, RefreshMaps, this);
            RefreshMaps();
            this.GetOrCreateTimeDependent().WithUpdate(OnUpdate).ThatProcessWhenPause();
        }

        // === Handling interaction
        void OnUpdate(float deltaTime) {
            DetermineInteractionStack(MousePosition, UIContext.Keyboard, _keyboardInteractionStack);
            UpdateReInput();
            UpdateAdditionalMouseButtons();
        }
        
        public void PerformGUI() {
            // update once-per-frame state
            if (_lastPositionUpdate != Time.frameCount) {
                UpdateMousePosition();
                UpdateHover();
                UpdateMouseHeld();
                _lastPositionUpdate = Time.frameCount;
            }
            // handle mouse events
            Event e = Event.current;
            if (e.type == EventType.MouseDown) HandleMouseDown(e);
            if (e.type == EventType.MouseUp) HandleMouseUp(e);
            if (e.type == EventType.ScrollWheel) HandleScroll(e);
        }

        void UpdateMousePosition() {
            MousePosition = PositionFromScreen(RewiredHelper.IsGamepad ? Focus.FocusScreenPosition : Input.mousePosition);
            DetermineInteractionStack(MousePosition, UIContext.Mouse, _mouseInteractionStack);
        }

        void UpdateHover() {
            UIEPointTo ui = new UIEPointTo { GameUI = this, Position = MousePosition };
            DeliverEvent(ui, null, _mouseInteractionStack);
        }

        // === Clicks

        Dictionary<int, IUIAware> _clickAssignedTo = new Dictionary<int, IUIAware>();

        void UpdateAdditionalMouseButtons() {
            UpdateMouseButton(KeyCode.Mouse3);
            UpdateMouseButton(KeyCode.Mouse4);
            UpdateMouseButton(KeyCode.Mouse5);
            UpdateMouseButton(KeyCode.Mouse6);
        }

        void UpdateMouseButton(KeyCode keyCode) {
            int button = keyCode - KeyCode.Mouse0;
            if (Input.GetKeyDown(keyCode)) {
                HandleMouseDown(button);
            } else if (Input.GetKeyUp(keyCode)) {
                HandleMouseUp(button);
            }
        }
        
        void HandleMouseDown(int button, EventModifiers modifiers, Event unityEvent) {
            var mouseDown = new UIEMouseDown { GameUI = this, Position = MousePosition, Button = button, Modifiers = modifiers };
            UIEventDelivery delivery = DeliverEvent(mouseDown, unityEvent, _mouseInteractionStack);
            // remember who consumed the event (if anybody)
            bool consumed = delivery.finalResult == UIResult.Accept || delivery.finalResult == UIResult.Prevent;
            IUIAware consumer = consumed ? delivery.responsibleObject : null;
            _clickAssignedTo[button] = consumer;
            // make an interaction stack snapshot so future "mouse up" events can refer to it (to see what was hovered when the mouse was last pressed)
            MakeStackSnapshot(MouseDownSnapshotLabel(button));
            _mouseHeldTime[MouseDownSnapshotLabel(button)] = Time.timeSinceLevelLoad;
        }
        void HandleMouseDown(Event unityEvent) => HandleMouseDown(unityEvent.button, unityEvent.modifiers, unityEvent);
        void HandleMouseDown(int button) => HandleMouseDown(button, EventModifiers.None, null);
        
        void UpdateMouseHeld() {
            foreach (var kvp in _clickAssignedTo) {
                UIEMouseHeld mouseHeld;
                if (Time.timeSinceLevelLoad - _mouseHeldTime[MouseDownSnapshotLabel(kvp.Key)] > _longPressTime) {
                    mouseHeld = new UIEMouseLongHeld {GameUI = this, Position = MousePosition, Button = kvp.Key, Modifiers = Event.current?.modifiers ?? EventModifiers.None};
                } else {
                    mouseHeld = new UIEMouseHeld {GameUI = this, Position = MousePosition, Button = kvp.Key, Modifiers = Event.current?.modifiers ?? EventModifiers.None};
                }

                List<IUIAware> eligibleHandlers = GetEligibleHandlersFromMouseDown(kvp.Key);
                DeliverEvent(mouseHeld, null, eligibleHandlers);
            }
        }

        void HandleMouseUp(int button, EventModifiers modifiers, Event unityEvent = null) {
            bool mouseDownWasDetected = _mouseHeldTime.TryGetValue(MouseDownSnapshotLabel(button), out var holdTime);
            if (!mouseDownWasDetected) return;
            
            UIEMouseUp uiEvent;
            if (Time.timeSinceLevelLoad - holdTime > _longPressTime) {
                uiEvent = new UIEMouseUpLong {GameUI = this, Position = MousePosition, Button = button, Modifiers = modifiers};
            } else {
                uiEvent = new UIEMouseUp {GameUI = this, Position = MousePosition, Button = button, Modifiers = modifiers};
            }
            // to get a mouse up event, you:
            // - must have been under the mouse when button was pressed
            // - must still be under the mouse when button is released
            // there is one exception - if you handled the earlier mouse down event, you (and only you)
            // automatically get the corresponding mouse up deliver to the chosen ones
            List<IUIAware> eligibleHandlers = GetEligibleHandlersFromMouseDown(button);
            // deliver
            DeliverEvent(uiEvent, unityEvent, eligibleHandlers);
            // cleanup
            _clickAssignedTo.Remove(button);
        }
        void HandleMouseUp(Event unityEvent) => HandleMouseUp(unityEvent.button, unityEvent.modifiers, unityEvent);
        void HandleMouseUp(int button) => HandleMouseUp(button, EventModifiers.None, null);

        List<IUIAware> GetEligibleHandlersFromMouseDown(int button) {
            List<IUIAware> eligibleHandlers;
            _clickAssignedTo.TryGetValue(button, out IUIAware consumer);
            bool consumerIsNull = consumer == null || (consumer is Component comp && comp == null);
            if (!consumerIsNull) {
                eligibleHandlers = new List<IUIAware>(1){consumer};
            } else {
                eligibleHandlers = _mouseInteractionStack.Intersect(Snapshot(MouseDownSnapshotLabel(button))).ToList();
            }
            return eligibleHandlers;
        }
        
        void HandleScroll(Event unityEvent) {
            var uiEvent = new UIEMouseScroll { GameUI = this, Position = MousePosition, Button = unityEvent.button, Modifiers = unityEvent.modifiers, Value = unityEvent.delta.y * -1f};
            DeliverEvent(uiEvent, unityEvent, _mouseInteractionStack);
        }

        public UIEventDelivery HandleMouseDoubleClick(UIEMouseDown mouseDownEvent) {
            // Create double-click event based on mouseDownEvent
            var doubleClick = new UIEMouseDoubleClick() { Button = mouseDownEvent.Button, GameUI = this, Modifiers = mouseDownEvent.Modifiers, Position = mouseDownEvent.Position };
            List<IUIAware> eligibleHandlers;
            // Check targets like in HandleMouseUp
            _clickAssignedTo.TryGetValue(mouseDownEvent.Button, out IUIAware consumer);
            if (consumer != null) {
                eligibleHandlers = new List<IUIAware>(1) { consumer };
            } else {
                eligibleHandlers =
                    _mouseInteractionStack.Intersect(Snapshot(MouseDownSnapshotLabel(mouseDownEvent.Button))).ToList();
            }
            UIEventDelivery delivery = DeliverEvent(doubleClick, null, eligibleHandlers);

            return delivery;
        }

        // === Keyboard & Gamepad

        HashSet<KeyCode> _heldKeys = new();
        List<ActionElementMap> _allControllersMaps = new();
        HashSet<int> _usedButtons = new();
        HashSet<int> _invokedByButtons = new();

        public void RefreshMaps() => _allControllersMaps ??= new List<ActionElementMap>();

        void UpdateReInput() {
            var player = RewiredHelper.Player;
            _usedButtons.Clear();
            
            NaviDirection.Update(player);
            UpdateRewiredActions(player);
            UpdateKeyboardKeys(player);
        }

        void UpdateRewiredActions(Player player) {
            foreach (InputAction action in _inputActions) {
                // if (action.type == InputActionType.Axis) {
                //     var uiActionData = UIAction.CreateData(player, action);
                //     var naviAction = UIAction.CreateNaviAction(this, MousePosition, player, uiActionData);
                //     if (naviAction != null) {
                //         DeliverReInputEvent(action, naviAction);
                //     }
                //     if (Mathf.Abs(player.GetAxisRaw(uiActionData.actionId)) > RewiredHelper.DeadZone) {
                //         var axisAction = new UIAxisAction { GameUI = this, Position = MousePosition, Data = uiActionData };
                //         DeliverReInputEvent(action, axisAction);
                //     }
                // } else {
                //     UIAction eventToDeliver = UIAction.CreateButton(this, MousePosition, player, action);
                //     if (eventToDeliver != null) {
                //         DeliverReInputEvent(action, eventToDeliver);
                //     }
                // }
            }
        }

        void UpdateKeyboardKeys(Player player) {
            // Keyboard keyboard = player.controllers.Keyboard;
            // if (keyboard == null) return;
            //
            // foreach (var keyDown in keyboard.PollForAllKeysDown()) {
            //     HandleKeyDown(keyDown.keyboardKey);
            // }
            //
            // if (_heldKeys.AnyNonAlloc()) {
            //     foreach (var heldKey in _heldKeys.Where(k => keyboard.GetKeyUp(k)).ToList()) {
            //         HandleKeyUp(heldKey);
            //     }
            // }
        }

        void DeliverReInputEvent(InputAction action, UIAction eventToDeliver) {
            _invokedByButtons.Clear();
            // foreach (var controllerMap in _allControllersMaps) {
            //     if (controllerMap.actionId == action.id) {
            //         _invokedByButtons.Add(controllerMap.elementIdentifierId);
            //     }
            // }

            bool buttonsUsable = _invokedByButtons.HasAnyCommonValue(_usedButtons) == false;

            if (buttonsUsable) {
                var delivery = DeliverEvent(eventToDeliver, null, _keyboardInteractionStack);
                var result = delivery.finalResult;
                if (result != UIResult.Ignore) {
                    _usedButtons.AddRange(_invokedByButtons);
                }
            }
        }

        void HandleKeyDown(KeyCode keyCode) {
            // don't report spurious 'None' key events
            if (keyCode == KeyCode.None) return;
            // don't report "auto-repeat" key down events
            if (!_heldKeys.Add(keyCode)) return;
            // deliver event
            UIEKeyDown uiEvent = new() {
                GameUI = this, Key = keyCode, Position = MousePosition
            };
            DeliverEvent(uiEvent, null, _keyboardInteractionStack);
        }

        void HandleKeyUp(KeyCode keyCode) {
            // don't report spurious 'None' key events
            if (keyCode == KeyCode.None) return;
            _heldKeys.Remove(keyCode);
        }

        // === Stack snapshots
        
        // snapshots are used when handling for future events has to look what was interactable in the past

        Dictionary<string, List<IUIAware>> _stackSnapshots = new Dictionary<string, List<IUIAware>>();

        List<IUIAware> Snapshot(string label) {
            _stackSnapshots.TryGetValue(label, out List<IUIAware> snapshot);
            return snapshot ?? new List<IUIAware>();
        }

        void MakeStackSnapshot(string label) {
            _stackSnapshots[label] = new List<IUIAware>(_mouseInteractionStack);
        }

        static string MouseDownSnapshotLabel(int button) => $"atMouseDown:{button}";

        // === Working with UI events and interaction stacks

        public UIEventDelivery DeliverEvent(UIEvent uiEvent, Event unityEvent, IReadOnlyList<IUIAware> stack) {
            Profiler.OnNewEvent(uiEvent, SmartHandlers.GetManagedEnumerator(), stack);
            // Finding first uiAware or smart Handler that doesn't ignore event
            var deliveryData = GetFirstNotIgnoredEventDeliveryDataOrDefault(uiEvent, stack);
            deliveryData.uiEvent = uiEvent;
            deliveryData.unityEvent = unityEvent;

            return PerformDelivery(deliveryData);
        }
        
        EventDeliveryData GetFirstNotIgnoredEventDeliveryDataOrDefault(UIEvent uiEvent, IReadOnlyList<IUIAware> stack) {
            // smart handlers before delivery
            foreach (var smartHandler in SmartHandlers) {
                var result = smartHandler.BeforeDelivery(uiEvent);
                Profiler.OnBeforeDelivery(uiEvent, smartHandler, result);
                if (result != UIResult.Ignore) {
                    return new EventDeliveryData(result, null);
                }
            }

            // iterate stack
            TryGetFirstNotIgnoredEventDeliveryDataFromHandlingBy(stack, uiEvent, out var data);
            return data;
        }
        
        bool TryGetFirstNotIgnoredEventDeliveryDataFromHandlingBy(IReadOnlyList<IUIAware> uiAwares, UIEvent uiEvent, out EventDeliveryData data) {
            for (int index = 0; index < uiAwares.Count; index++) {
                IUIAware uiAware = uiAwares[index];
                // smart handlers before handling
                foreach (var smartHandler in SmartHandlers) {
                    var resultOfBeforeHandling = smartHandler.BeforeHandlingBy(uiAware, uiEvent);
                    Profiler.OnBeforeHandling(uiEvent, uiAware, smartHandler, resultOfBeforeHandling);
                    if (resultOfBeforeHandling != UIResult.Ignore) {
                        data = new EventDeliveryData(resultOfBeforeHandling, uiAware);
                        return true;
                    }
                }

                // handling
                var resultOfHandling = uiAware.Handle(uiEvent);
                Profiler.OnHandling(uiEvent, uiAware, resultOfHandling);
                if (resultOfHandling != UIResult.Ignore) {
                    data = new EventDeliveryData(resultOfHandling, uiAware);
                    return true;
                }

                if (uiAware is IUIAwareContainer awareContainer) {
                    if (TryGetFirstNotIgnoredEventDeliveryDataFromHandlingBy(awareContainer.UIAwares, uiEvent, out data)) {
                        return true;
                    }
                }

                // smart handlers after handling
                foreach (var smartHandler in SmartHandlers) {
                    var resultOfAfterHandling = smartHandler.AfterHandlingBy(uiAware, uiEvent);
                    Profiler.OnAfterHandling(uiEvent, uiAware, smartHandler, resultOfAfterHandling);
                    if (resultOfAfterHandling != UIResult.Ignore) {
                        data = new EventDeliveryData(resultOfAfterHandling, uiAware);
                        return true;
                    }
                }
            }

            data = default;
            return false;
        }

        UIEventDelivery PerformDelivery(UIResult result, UIEvent evt, Event unityEvent, IUIAware responsible = null) {
            // use up Unity's event if we should
            if (result != UIResult.Ignore) {
                unityEvent?.Use();
            }
            // prepare object
            UIEventDelivery delivery = new() {
                finalResult = result,
                handledEvent = evt,
                responsibleObject = responsible,
            };
            // smart handlers after delivery
            var aggregateResult = delivery;
            foreach (var ext in SmartHandlers) {
                aggregateResult = ext.AfterDelivery(aggregateResult);
            }
            return aggregateResult;
        }

        UIEventDelivery PerformDelivery(EventDeliveryData data) {
            return PerformDelivery(data.result, data.uiEvent, data.unityEvent, data.responsible);
        }

        public void DetermineInteractionStack(UIPosition position, UIContext context, List<IUIAware> handlers) {
            handlers.Clear();
            _uiHandlerSourcesSorted.Clear();
            foreach (var source in Elements<IUIHandlerSource>()) {
                _uiHandlerSourcesSorted.Add(source);
            }
            _uiHandlerSourcesSorted.Sort(static (a, b) => b.Priority.CompareTo(a.Priority));
            int count = _uiHandlerSourcesSorted.Count;
            for (int i = 0; i < count; i++) {
                var source = _uiHandlerSourcesSorted[i];
                if ((source.Context & context) == context) {
                    source.ProvideHandlers(position, handlers);
                }
            }
        }

        public UIPosition PositionFromScreen(Vector2 screenPos) {
            // world position at y=0 plane
            Vector3 worldPos = CameraUtils.ScreenPositionToWorldAtY(screenPos, 0, _camera.Camera);
            return new UIPosition { world = worldPos, screen = screenPos };
        }

        struct EventDeliveryData {
            public UIResult result;
            public IUIAware responsible;
            public UIEvent uiEvent;
            public Event unityEvent;

            public EventDeliveryData(UIResult result, IUIAware responsible) {
                this.result = result;
                this.responsible = responsible;
                uiEvent = null;
                unityEvent = null;
            }
        }
    }
}