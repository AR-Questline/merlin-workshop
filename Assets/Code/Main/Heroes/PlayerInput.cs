using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    /// <summary>
    /// Service for handling player input & shortcuts.
    /// </summary>
    public partial class PlayerInput : Element<GameUI>, ISmartHandler {
        public const int FramesProlongedInput = 0;
        const float ZoomSensitivity = 2.5f;

        // === Input Evaluated
        public Vector2 MoveInput { get; private set; } = Vector2.zero;
        public Vector2 MountMoveInput { get; private set; } = Vector2.zero;
        public Vector2 LookInput { get; private set; } = Vector2.zero;

        readonly MultiMap<string, IUIPlayerInput> _playerMapInputs = new();

        readonly Dictionary<string, KeyValueAutoRefresh> _heldActions = new();
        readonly Dictionary<string, KeyValueAutoRefresh> _longHeldActions = new();
        readonly Dictionary<string, KeyValueAutoRefresh> _downActions = new();
        readonly Dictionary<string, KeyValueAutoRefresh> _upActions = new();
        readonly Dictionary<string, KeyValueAutoRefresh> _longPressUpActions = new();
        
        readonly Dictionary<int, KeyValueAutoRefresh> _mouseDownActions = new();
        readonly Dictionary<int, KeyValueAutoRefresh> _mouseHeldActions = new();
        readonly Dictionary<int, KeyValueAutoRefresh> _mouseLongHeldActions = new();
        readonly Dictionary<int, KeyValueAutoRefresh> _mouseUpActions = new();
        readonly Dictionary<int, KeyValueAutoRefresh> _mouseLongPressUpActions = new();

        bool CanZoomTpp => Hero.TppActive && !World.HasAny<ContainerUI>();

        public Dictionary<int, KeyValueAutoRefresh> MouseDownActions => _mouseDownActions;
        public Dictionary<string, KeyValueAutoRefresh> LongHeldActions => _longHeldActions;

        public PlayerInput() {
            ModelElements.SetInitCapacity(70);
            ModelElements.SetInitCapacity(typeof(KeyValueAutoRefresh), 1, 70);
        }

        protected override void OnFullyInitialized() {
            this.GetOrCreateTimeDependent().WithLateUpdate(ProcessLateUpdate);
            InitKeyValueRefreshes();
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
        }

        void OnUIStateChanged(UIState state) {
            if (!state.IsMapInteractive) {
                foreach (var keyValueRefresh in Elements<KeyValueAutoRefresh>()) {
                    keyValueRefresh.Reset();
                }
            }
        }

        void InitKeyValueRefreshes() {
            _longPressUpActions[KeyBindings.Gameplay.Attack] = new KeyValueAutoRefresh();
            _longPressUpActions[KeyBindings.Gameplay.AttackHeavy] = new KeyValueAutoRefresh();
            
            _longHeldActions[KeyBindings.Gameplay.Attack] = new KeyValueAutoRefresh();
            _longHeldActions[KeyBindings.Gameplay.AttackHeavy] = new KeyValueAutoRefresh();
            _longHeldActions[KeyBindings.Gameplay.Block] = new KeyValueAutoRefresh();
            
            _upActions[KeyBindings.Gameplay.Attack] = new KeyValueAutoRefresh();
            _upActions[KeyBindings.Gameplay.AttackHeavy] = new KeyValueAutoRefresh();
            _upActions[KeyBindings.Gameplay.Crouch] = new KeyValueAutoRefresh();
            _upActions[KeyBindings.Gameplay.Block] = new KeyValueAutoRefresh();

            _heldActions[KeyBindings.Gameplay.Sprint] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Block] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Kick] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Attack] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.AttackHeavy] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Jump] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Crouch] = new KeyValueAutoRefresh();
            _heldActions[KeyBindings.Gameplay.Walk] = new KeyValueAutoRefresh();

            _downActions[KeyBindings.Gameplay.Crouch] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Dash] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Walk] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Jump] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Attack] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.AttackHeavy] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Sprint] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.Gameplay.Block] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.EquipFirstItem] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.EquipSecondItem] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.EquipThirdItem] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.EquipFourthItem] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.UseQuickSlot] = new KeyValueAutoRefresh();
            _downActions[KeyBindings.HeroItems.NextQuickSlot] = new KeyValueAutoRefresh();

            for (int i = 0; i < 7; i++) {
                _mouseDownActions[i] = new KeyValueAutoRefresh();
                _mouseHeldActions[i] = new KeyValueAutoRefresh();
                _mouseUpActions[i] = new KeyValueAutoRefresh();
                _mouseLongHeldActions[i] = new KeyValueAutoRefresh();
                _mouseLongPressUpActions[i] = new KeyValueAutoRefresh();
            }

            foreach (var heldAction in _heldActions.Values
                .Union(_downActions.Values)
                .Union(_upActions.Values)
                .Union(_longPressUpActions.Values)
                .Union(_longHeldActions.Values)
                .Union(_mouseDownActions.Values)
                .Union(_mouseHeldActions.Values)
                .Union(_mouseUpActions.Values)
                .Union(_mouseLongHeldActions.Values)
                .Union(_mouseLongPressUpActions.Values)) {
                AddElement(heldAction);
            }
        }

        public void RegisterPlayerInput(IUIPlayerInput playerInput, IModel owner = null) {
            foreach (KeyBindings keyBinding in playerInput.PlayerKeyBindings) {
                _playerMapInputs.Add(keyBinding, playerInput);
            }

            owner?.ListenTo(Events.BeforeDiscarded, () => UnregisterPlayerInput(playerInput), this);
        }
        
        public void UnregisterPlayerInput(IUIPlayerInput playerInput) {
            foreach (KeyBindings keyBinding in playerInput.PlayerKeyBindings) {
                _playerMapInputs.Remove(keyBinding, playerInput);
            }
        }

        // --- keyboard
        public bool GetButtonDown(KeyBindings keyBind) {
            return _downActions.TryGetValue(keyBind, out KeyValueAutoRefresh downAction) ? downAction : false;
        }

        public bool GetButtonUp(KeyBindings keyBind) {
            return _upActions.TryGetValue(keyBind, out KeyValueAutoRefresh upAction) ? upAction : false;
        }

        [UnityEngine.Scripting.Preserve]
        public bool GetButtonLongPressUp(KeyBindings keyBind) {
            return _longPressUpActions.TryGetValue(keyBind, out KeyValueAutoRefresh longPressUpAction) ? longPressUpAction : false;
        }
        
        public bool GetButtonHeld(KeyBindings keyBind) {
            return _heldActions.TryGetValue(keyBind, out KeyValueAutoRefresh heldAction) ? heldAction : false;
        }
        
        public bool GetButtonLongHeld(KeyBindings keyBind) {
            return _longHeldActions.TryGetValue(keyBind, out KeyValueAutoRefresh longHeldAction) ? longHeldAction : false;
        }
        
        // --- mouse
        public bool GetMouseDown(MouseButton mouseButton) => _mouseDownActions[(int)mouseButton];
        public bool GetMouseUp(MouseButton mouseButton) => _mouseUpActions[(int)mouseButton];
        [UnityEngine.Scripting.Preserve] public bool GetMouseLongPressUp(MouseButton mouseButton) => _mouseLongPressUpActions[(int)mouseButton];
        public bool GetMouseHeld(MouseButton mouseButton) => _mouseHeldActions[(int)mouseButton];
        public bool GetMouseLongHeld(MouseButton mouseButton) => _mouseLongHeldActions[(int)mouseButton];
        
        void ProcessLateUpdate(float deltaTime) {
            if (!UIStateStack.Instance.State.IsMapInteractive) {
                MoveInput = Vector2.zero;
                MountMoveInput = Vector2.zero;
                LookInput = Vector2.zero;
                return;
            }
            
            // MoveInput = new Vector2(
            //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal), 
            //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical)
            // );
            // MountMoveInput = new Vector2(
            //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.MountHorizontal), 
            //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.MountVertical)
            // );
            //
            // bool isChangingTppDistance = RewiredHelper.IsGamepad && Hero.TppActive && RewiredHelper.Player.GetButton(KeyBindings.Gameplay.ChangeHeroPerspective);
            //
            // LookInput = isChangingTppDistance ? Vector2.zero : new Vector2(
            //     RewiredHelper.Player.GetAxisRaw(KeyBindings.Gameplay.CameraHorizontal),
            //     RewiredHelper.Player.GetAxisRaw(KeyBindings.Gameplay.CameraVertical)
            // );
        }
        
        // === ISmartHandler
        public UIResult BeforeDelivery(UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            if (delivery.finalResult != UIResult.Ignore) {
                OnEventUsed(delivery);
                return delivery;
            }
            
            if (!UIStateStack.Instance.State.IsMapInteractive) {
                return delivery;
            }
            
            UIEvent evt = delivery.handledEvent;

            delivery = HandleRegisteredPlayerInputs(evt, delivery);
            if (delivery.finalResult != UIResult.Ignore) {
                return delivery;
            }
            
            HandleKeyboard(evt);
            HandleMouse(evt);
            HandleHeroInput();
            return delivery;
        }

        void OnEventUsed(UIEventDelivery delivery) {
            if (delivery.handledEvent is UIEMouseDown md) {
                // If anyone handled mouse down events, we should disallow further usage of that event and following mouse up event
                _mouseDownActions[md.Button].UpdateValue(false);
                SetMouseButtonActive(md.Button, false);
            }
        }

        void HandleKeyboard(UIEvent evt) {
            if (evt is UIKeyDownAction keyDownAction) {
                if (_downActions.TryGetValue(keyDownAction.Name, out KeyValueAutoRefresh action)) {
                    action.UpdateValue(true);
                }
            } else if (evt is UIKeyUpAction keyUpAction) {
                if (_upActions.TryGetValue(keyUpAction.Name, out KeyValueAutoRefresh action)) {
                    action.UpdateValue(true);
                }

                if (evt is UIKeyLongUpAction) {
                    if (_longPressUpActions.TryGetValue(keyUpAction.Name, out KeyValueAutoRefresh upAction)) {
                        upAction.UpdateValue(true);
                    }
                }

                if (_heldActions.TryGetValue(keyUpAction.Name, out var value)) {
                    value.UpdateValue(false);
                }
            } else if (evt is UIKeyHeldAction keyHeldAction) {
                if (_heldActions.TryGetValue(keyHeldAction.Name, out KeyValueAutoRefresh action)) {
                    action.UpdateValue(true);
                }

                if (evt is UIKeyLongHeldAction) {
                    if (_longHeldActions.TryGetValue(keyHeldAction.Name, out KeyValueAutoRefresh heldAction)) {
                        heldAction.UpdateValue(true);
                    }
                }
            }
        }

        void HandleMouse(UIEvent evt) {
            if (evt is UIEMouseDown mouseDown) {
                _mouseDownActions[mouseDown.Button].UpdateValue(true);
                SetMouseButtonActive(mouseDown.Button, true);
            } else if (evt is UIEMouseUp mouseUp) {
                _mouseUpActions[mouseUp.Button].UpdateValue(true);
                if (evt is UIEMouseUpLong) {
                    _mouseLongPressUpActions[mouseUp.Button].UpdateValue(true);
                }
            } else if (evt is UIEMouseHeld mouseHeld) {
                _mouseHeldActions[mouseHeld.Button].UpdateValue(true);
                if (evt is UIEMouseLongHeld) {
                    _mouseLongHeldActions[mouseHeld.Button].UpdateValue(true);
                }
            } else if (evt is UIEMouseScroll mouseScroll) {
                if (CanZoomTpp) {
                    World.Any<TppCameraDistanceSetting>()?.ChangeValue(mouseScroll.Value * ZoomSensitivity * Time.deltaTime);
                }
            }
        }

        void HandleHeroInput() {
            var hero = Hero.Current;
            if (hero == null) {
                return;
            }
            bool equipWeaponInputPressed = _mouseDownActions[0] || _mouseDownActions[1];
            equipWeaponInputPressed |= GetButtonUp(KeyBindings.Gameplay.Block);
            equipWeaponInputPressed |= GetButtonUp(KeyBindings.Gameplay.Attack);
            
            if (equipWeaponInputPressed && !hero.IsWeaponEquipped) {
                hero.Trigger(Hero.Events.ShowWeapons, false);
            }
        }

        void SetMouseButtonActive(int button, bool active) {
            _mouseUpActions[button].SetAllowed(active);
            _mouseLongPressUpActions[button].SetAllowed(active);
            _mouseHeldActions[button].SetAllowed(active);
            _mouseLongHeldActions[button].SetAllowed(active);
        }

        UIEventDelivery HandleRegisteredPlayerInputs(UIEvent evt, UIEventDelivery delivery) {
            if (evt is UIKeyAction keyAction) {
                var orderedInputs = _playerMapInputs.GetValues(keyAction.Name, true).OrderByDescending(input => input.InputPriority);
                foreach (IUIPlayerInput input in orderedInputs) {
                    var result = input.Handle(evt);
                    if (result != UIResult.Ignore) {
                        delivery.responsibleObject = input;
                        delivery.finalResult = result;
                        return delivery;
                    }
                }
            }
            return delivery;
        }
    }

    public enum MouseButton {
        [UnityEngine.Scripting.Preserve] LeftMouseButton = 0,
        [UnityEngine.Scripting.Preserve] RightMouseButton = 1,
        [UnityEngine.Scripting.Preserve] MiddleMouseButton = 2,
    }
}
