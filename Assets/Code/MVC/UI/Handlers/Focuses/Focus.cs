using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using EventSystem = UnityEngine.EventSystems.EventSystem;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    /// <summary>
    /// Handles gamepad focus
    /// </summary>
    public partial class Focus : Element<GameUI>, ISmartHandler, IUIHandlerSource {
        public sealed override bool IsNotSaved => true;

        // == IUIHandlerSource Config
        public UIContext Context => UIContext.All;
        public int Priority => 1;
        
        // === State
        Component _focused;
        // this is to return null if this Component was destroyed by unity
        public Component Focused => _focused != null ? _focused : null;
        public Transform FocusBase => _focusBases.LastOrDefault();
        public bool FocusBaseSuppressed { get; private set; }

        public Vector3 FocusScreenPosition {
            get {
                Vector3 screenPos = GetScreenPosition();
                screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
                screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);
                return screenPos;
            }
        }

        public static bool DebugMode => SafeEditorPrefs.GetBool("debug.game.pad");

        FocusHistorian Historian => Element<FocusHistorian>();
        private int CurrentNumberOfControllers => 1;//ReInput.controllers.Controllers.Count;
        
        List<Transform> _focusBases = new List<Transform>();
        bool _isGamepad;
        bool _isConnected;
        int _numberOfControllers;

        public bool IsGamepad {
            [UnityEngine.Scripting.Preserve] get => _isGamepad;
            set {
                if (_isGamepad != value) {
                    _isGamepad = value;
                    this.Trigger(Events.ControllerChanged, value ? ControllerType.Joystick : ControllerType.Keyboard);
                    ParentModel.RefreshMaps();
                }
            }
        }

        bool IsGamepadConnected {
            set {
                bool wasDisconnected = _isConnected && !value;
                bool canShowMenuUI = !World.HasAny<MenuUI>() && !World.HasAny<Cutscene>() && UIStateStack.Instance.State.IsMapInteractive;
                bool numberOfControllersReduced = _numberOfControllers > CurrentNumberOfControllers;
                if (wasDisconnected && canShowMenuUI && numberOfControllersReduced) { 
                    World.Add(new MenuUI());
                }

                _isConnected = value;
            }
        }
        
        // === Events
        public new static class Events {
            public static readonly Event<Focus, FocusChange> FocusChanged = new(nameof(FocusChanged));
            public static readonly Event<Focus, FocusChange> AfterFocusChanged = new(nameof(AfterFocusChanged));
            public static readonly Event<Focus, Focus> FocusBaseChanged = new(nameof(FocusBaseChanged));
            public static readonly Event<Focus, ControllerType> ControllerChanged = new(nameof(ControllerChanged));
            public static readonly Event<Focus, UIKeyMapping> KeyMappingRefreshed = new(nameof(KeyMappingRefreshed));
        }

        // === Initialization
        protected override void OnInitialize() {
            AddElement(new FocusHistorian());

            this.GetOrCreateTimeDependent().WithLateUpdate(ProcessLateUpdate).ThatProcessWhenPause();
        }

        // === Focus / Unfocus

        /// <summary>
        /// Changes focus to a new one, triggering events.
        /// </summary>
        public void Select(Component selectable, bool isFromInit = false) {
            if (selectable == _focused) return;
            if (!BelongsToFocusBase(selectable) || !RewiredHelper.IsGamepad) {
                // Register this try, so that historian can select something, when user decides to pickup the gamepad.
                Historian.RegisterFocusChange(selectable, isFromInit);
                return;
            }
            
            Component previous = _focused;
            // change selection
            _focused = selectable;
            EventSystem.current.SetSelectedGameObject(null);
            if (selectable != null) {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
            ParentModel.Element<Hovering>().ChangeHoverTo(selectable);
            this.Trigger(Events.FocusChanged, new FocusChange {previous = previous, current = _focused});
            Historian.RegisterFocusChange(_focused, isFromInit);
            if (DebugMode) {
                Log.Important?.Error($"Selection - changed to {selectable?.gameObject.name}", selectable?.gameObject);
            }
            this.Trigger(Events.AfterFocusChanged, new FocusChange {previous = previous, current = _focused});
        }

        /// <summary>
        /// Deselects a specified object. If this object is not currently selected, nothing happens.
        /// </summary>
        public void Deselect(Component selectable) {
            if (_focused == selectable) Select(null);
            Historian.Erase(selectable);
        }

        /// <summary>
        /// Removes any currently active selection.
        /// </summary>
        public void DeselectAll() {
            if (!RewiredHelper.IsGamepad) {
                _focused = null;
            } else {
                Select(null);
            }
        }
        
        /// <summary>
        /// Register new view that wants to change focus
        /// </summary>
        public void RegisterView(View view) {
            if (view is IAutoFocusBase focusBase) {
                SwitchToFocusBase(focusBase.transform);
            }

            if (view is IFocusSource focusSource) {
                if (focusSource.ForceFocus) {
                    Select(focusSource.DefaultFocus, true);
                } else {
                    TryToFocusGently(focusSource.DefaultFocus, true);
                }
            }
        }

        public void TryToFocusGently(Component component, bool isFromInit = false) {
            if (component == null) {
                return;
            }
            if (_focused == null || !BelongsToFocusBase(_focused)) {
                Select(component, isFromInit);
            } else {
                Historian.RegisterFocusChange(component, isFromInit);
            }
        }

        // === Focus Base logic
        public void SwitchToFocusBase(Transform focusBase) {
            _focusBases.Remove(focusBase);
            _focusBases.Add(focusBase);
            this.Trigger(Events.FocusBaseChanged, this);
            LogFocusBaseChanged();
        }

        public void RemoveFocusBase(Transform focusBase) {
            _focusBases.Remove(focusBase);
            this.Trigger(Events.FocusBaseChanged, this);
            LogFocusBaseChanged();
        }

        [UnityEngine.Scripting.Preserve]
        public void SuppressFocusBase() {
            FocusBaseSuppressed = true;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void RestoreFocusBase() {
            FocusBaseSuppressed = false;
        }

        public bool BelongsToFocusBase(Component component) {
            if (component == null) {
                return true;
            }

            if (!component.gameObject.activeInHierarchy) {
                return false;
            }
            CleanupFocusBases();
            if (!_focusBases.Any()) {
                return true;
            }
            return FocusBaseSuppressed || component.GetComponentsInParent<Transform>().Any(t => t == FocusBase);
        }

        void CleanupFocusBases() {
            Transform current = FocusBase;
            _focusBases.RemoveAll(b => b == null);
            if (FocusBase != current) {
                this.Trigger(Events.FocusBaseChanged, this);
                LogFocusBaseChanged();
            }
        }

        [Conditional("DEBUG")]
        void LogFocusBaseChanged() {
            if (DebugMode) {
                Log.Important?.Error($"Focus Base - changed to {FocusBase?.name}", FocusBase?.gameObject);
            }
        }

        // === UI event handling
        public UIResult BeforeDelivery(UIEvent evt) {
            if (_focused == null) {
                return UIResult.Ignore;
            }

            if (evt is UIAction) {
                // check if focused component is valid
                if (!BelongsToFocusBase(_focused)) {
                    // allow focus historian to select something more appropriate
                    DeselectAll();
                    return UIResult.Accept;
                }
            }
            
            if (evt is UINaviAction naviAction) {
                return HandleNaviAction(naviAction);
            } else if (evt is UISubmitAction) {
                return HandleSubmitButton();
            } else if (evt is UIKeyAction) {
                return DeliverEventToFocused(evt, false, false);
            }
            
            return UIResult.Ignore;
        }
        
        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;
        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            if (delivery.handledEvent is UIKeyDownAction action && delivery.finalResult == UIResult.Ignore) {
                NaviDirection direction = null;
                
                if (action.Name == KeyBindings.Gamepad.DPad_Down) {
                    direction = NaviDirection.Down;
                } else if (action.Name == KeyBindings.Gamepad.DPad_Up) {
                    direction = NaviDirection.Up;
                } else if (action.Name == KeyBindings.Gamepad.DPad_Left) {
                    direction = NaviDirection.Left;
                } else if (action.Name == KeyBindings.Gamepad.DPad_Right) {
                    direction = NaviDirection.Right;
                }

                if (direction != null) {
                    delivery.finalResult = HandleNaviAction(new UINaviAction() {
                        GameUI = action.GameUI,
                        Position = action.Position,
                        Data = action.Data,
                        direction = direction,
                    });
                }
            }

            return delivery;
        }

        // === Providing Handler Source
        public void ProvideHandlers(UIPosition position, List<IUIAware> handlers) {
            if (RewiredHelper.IsGamepad && Focused is IUIAware uiAware) {
                handlers.Add(uiAware);
            }
        }
        
        // === Helpers
        UIResult HandleNaviAction(UINaviAction naviAction) {
            //Ignore immediately if Focused is null
            if (_focused == null) {
                return UIResult.Ignore;
            }
            
            // Check if Focused wants to manage navigation
            IUIAware focusedAware = _focused?.GetComponentInParent<IUIAware>();
            if (focusedAware != null) {
                UIResult result = HandleNaviActionByUIAware(focusedAware, naviAction);
                if (result != UIResult.Ignore) {
                    return result;
                }
            }
            
            // Check if Focused override navigation somehow
            INaviOverride naviOverride = _focused?.GetComponentInParent<INaviOverride>();
            if (naviOverride != null) {
                UIResult result = naviOverride.Navigate(naviAction);
                if (result != UIResult.Ignore) {
                    return result;
                }
            }

            // Find the object we should navigate to  
            Component target = FindClosestTarget(naviAction);
            if (target != null) {
                Select(target);
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        UIResult HandleNaviActionByUIAware(IUIAware aware, UINaviAction navi) {
            UIResult result = aware.Handle(navi);
            if (result != UIResult.Ignore) return result;

            if (aware is IUIAwareContainer container) {
                foreach (var a in container.UIAwares) {
                    result = HandleNaviActionByUIAware(a, navi);
                    if (result != UIResult.Ignore) return result;
                }
            }

            return UIResult.Ignore;
        }

        UIResult HandleSubmitButton() {
            // Different methods of submitting
            switch (_focused) {
                case Button button:
                    button.onClick.Invoke();
                    return UIResult.Accept;
                case Toggle toggle:
                    toggle.isOn = !toggle.isOn;
                    return UIResult.Accept;
                default:
                     return UIResult.Ignore;
            }
        }

        Component FindClosestTarget(UINaviAction naviAction) {
            Selectable target = null;
            Selectable previousTarget = null;
            Selectable source = _focused?.GetComponent<Selectable>();
            bool endOfLoop = source == null;
            while (!endOfLoop) {
                target = naviAction.direction.GetFrom(source);
                endOfLoop = IsNavigable(target);
                if (!endOfLoop) {
                    if (target == previousTarget) {
                        target = null;
                        endOfLoop = true;
                    } else {
                        previousTarget = source;
                        source = target;
                    }
                }
            }

            return target;
        }

        public static bool IsNavigable(Selectable selectable) {
            if (selectable == null) return true;
            if (selectable.GetComponent<Scrollbar>() != null) return false;
            return selectable.GetComponent<INaviBlocker>()?.AllowNavigation ?? true;
        }
        
        UIResult DeliverEventToFocused(UIEvent evt, bool goUpHierarchy, bool allowSmartHandlers) {
            if (_focused == null || !RewiredHelper.IsGamepad) {
                return UIResult.Ignore;
            }
            
            IUIAware[] awares = GetUIAwares(goUpHierarchy);
            if (!awares.Any()) {
                return UIResult.Ignore;
            }
            
            if (allowSmartHandlers) {
                return ParentModel.DeliverEvent(evt, null, awares).finalResult;
            }
            return awares.Select(a => a.Handle(evt)).FirstOrDefault(r => r != UIResult.Ignore);

        }

        IUIAware[] GetUIAwares(bool goUpHierarchy) {
            IUIAware[] awares;
            if (goUpHierarchy) {
                awares = _focused.GetComponentsInParent<IUIAware>();
            } else {
                IUIAware aware = _focused.GetComponentInParent<IUIAware>();
                awares = aware != null ? new[] {aware} : new IUIAware[0];
            }
            return awares;
        }
        
        Vector3 GetScreenPosition() {
            if (_focused == null) {
                return Input.mousePosition;
            }

            if (_focused.transform is RectTransform rect) {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector3 position = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
                
                Canvas canvas = _focused.transform.GetComponentInParent<Canvas>(); 
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                    return position;
                }
                return ParentModel.MainCamera.WorldToScreenPoint(position);
            }
            return ParentModel.MainCamera.WorldToScreenPoint(_focused.transform.position);
        }

        // === TimeDependent
        
        void ProcessLateUpdate(float deltaTime) {
            IsGamepad = RewiredHelper.IsGamepad;
            IsGamepadConnected = RewiredHelper.IsCurrentControllerConnected;
            _numberOfControllers = CurrentNumberOfControllers;
        }
    }
}