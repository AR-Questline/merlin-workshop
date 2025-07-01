using System;
using System.Collections.Generic;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;

namespace Awaken.TG.Main.UI.ButtonSystem {
    public partial class Prompt : Element<Prompts>, IButton, IUniqueKeyProvider {
        readonly KeyBindings _key;
        readonly Action _action;
        readonly IButton.PressType _pressType;
        readonly bool _visualOnly;
        readonly Position _position;
        readonly ControlSchemeFlag _controllers;
        readonly KeyIcon[] _registerKeyIcons = new KeyIcon[4];

        string _name;
        
        bool _active;
        bool _visible;
        float _holdPercent;

        List<IPromptListener> _listeners = new();

        public float HoldTime { get; set; }
        public string ActionName => _name;
        [UnityEngine.Scripting.Preserve] public IButton.PressType PressType => _pressType;
        IButton.PressType IButton.ButtonPressType => _pressType;
        KeyIcon.Data IUniqueKeyProvider.UniqueKey => new(_key, _pressType == IButton.PressType.Hold);
        
        public bool IsActive => IsVisibleForController && _active;
        public bool IsVisibleForController => _visible && _controllers.HasFlagFast(ControlSchemes.CurrentAsFlag());
        
        public float HoldPercent {
            [UnityEngine.Scripting.Preserve] get => _holdPercent;
            private set {
                _holdPercent = value;
                foreach (KeyIcon icon in _registerKeyIcons) {
                    if (icon != null) {
                        icon.SetHoldPercent(value);
                    }
                }
            }
        }
        
        public new static class Events {
            public static readonly Event<Prompt, Prompt> OnHoldPromptInterrupted = new(nameof(OnHoldPromptInterrupted));
        }

        Prompt(KeyBindings key, string name, IButton.PressType pressType, Action action, Position position, ControlSchemeFlag controllers = ControlSchemeFlag.All, float holdTime = ButtonsHandler.HoldTime) {
            _key = key;
            _name = name;
            _pressType = pressType;
            _action = action;
            _visualOnly = action == null;
            _position = position;
            _controllers = controllers;
            HoldTime = holdTime;
        }
        
        protected override void OnFullyInitialized() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, RefreshState);
            RefreshState();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_holdPercent > 0) {
                OnHoldInterrupted();
            }
        }

        /// <summary>
        /// if not visible active setting is ignored
        /// </summary>
        public Prompt SetupState(bool visible, bool active) {
            SetActive(active);
            SetVisible(visible);
            return this;
        }

        /// <summary>
        /// if not visible active setting is ignored
        /// </summary>
        public void SetActive(bool active) {
            if (active == _active) return;
            _active = active;
            RefreshState();
        }

        /// <summary>
        /// Will not be interactable if not active
        /// </summary>
        public void SetVisible(bool visible) {
            if (visible == _visible) return;
            _visible = visible;
            RefreshState();
        }

        public void ChangeName(string name) {
            if (name == _name) return;
            _name = name;
            RefreshState();
        }

        public void AddListener(IPromptListener listener) {
            _listeners.Add(listener);
        }
        
        public void RemoveListener(IPromptListener listener) {
            _listeners.Remove(listener);
        }

        public Prompt AddAudio(PromptAudio audio = null) {
            audio ??= CommonReferences.Get.AudioConfig.DefaultPromptAudio;
            AddListener(audio);

            return this;
        }
        
        public void RefreshState() {
            foreach (var listener in _listeners) {
                listener.SetName(ActionName);
                listener.SetVisible(IsVisibleForController);
                listener.SetActive(IsActive);
            }
        }

        public void RefreshPosition(ref int firstsCount) {
            if (MainView == null) {
                return;
            }
            
            if (_position == Position.First) {
                MainView.transform.SetSiblingIndex(firstsCount);
                firstsCount++;
            } else if (_position == Position.Last) {
                MainView.transform.SetAsLastSibling();
            }
        }
        
        public bool Accept(UIKeyAction action) => !_visualOnly && IsActive && ActionMatches(action);
        public bool ActionMatches(UIKeyAction action) => action.Name == _key || RewiredHelper.IsEqualElementMapKey(action.Id, _key);
        
        public void OnHoldInterrupted() {
            HoldPercent = 0;
            foreach (IPromptListener listener in _listeners) {
                listener.OnHoldPromptInterrupted(this);
            }
            this.Trigger(Events.OnHoldPromptInterrupted, this);
        }
        
        public void InvokeCallback() {
            if (IsActive) {
                _action?.Invoke();
            }
        }

        void IButton.Invoke() => _action?.Invoke();
        
        void IButtonTap.OnTap() {
            foreach (var listener in _listeners) {
                listener.OnTap(this);
            }
        }
        
        void IButtonHold.OnKeyDown() {
            foreach (var listener in _listeners) {
                listener.OnHoldKeyDown(this);
            }
            HoldPercent = 0;
        }
        
        void IButtonHold.OnKeyHeld(float percent) {
            foreach (var listener in _listeners) {
                listener.OnHoldKeyHeld(this, percent);
            }
            HoldPercent = percent;
        }
        
        void IButtonHold.OnKeyUp(bool completed) {
            foreach (var listener in _listeners) {
                listener.OnHoldKeyUp(this, completed);
            }
            HoldPercent = 0;
        }
        
        void IUniqueKeyProvider.RegisterForHold(KeyIcon keyIcon) {
            for (int i = 0; i < _registerKeyIcons.Length; i++) {
                if (_registerKeyIcons[i] == null) {
                    _registerKeyIcons[i] = keyIcon;
                    return;
                }
            }
            Log.Important?.Error("Not supported extreme case, increase " + nameof(_registerKeyIcons) + " arraySize to accomodate");
        }
        
        public static Prompt Tap(KeyBindings key, string name, [NotNull] Action action, Position position = Position.Middle, ControlSchemeFlag controllers = ControlSchemeFlag.All) {
            return new(key, name, IButton.PressType.Tap, action, position, controllers);
        }   
        public static Prompt Hold(KeyBindings key, string name, [NotNull] Action action, Position position = Position.Middle, ControlSchemeFlag controllers = ControlSchemeFlag.All, float holdTime = ButtonsHandler.HoldTime) {
            return new(key, name, IButton.PressType.Hold, action, position, controllers, holdTime);
        }
        public static Prompt VisualOnlyTap(KeyBindings key, string name, Position position = Position.Middle, ControlSchemeFlag controllers = ControlSchemeFlag.All) {
            return new(key, name, IButton.PressType.Tap, null, position, controllers);
        }
        public static Prompt VisualOnlyHold(KeyBindings key, string name, Position position = Position.Middle, ControlSchemeFlag controllers = ControlSchemeFlag.All) {
            return new(key, name, IButton.PressType.Hold, null, position, controllers);
        }

        public enum Position : byte {
            Middle,
            First,
            Last,
        }
    }
}