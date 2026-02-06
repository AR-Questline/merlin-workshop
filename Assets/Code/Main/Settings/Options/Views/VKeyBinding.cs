using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Rewired;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VKeyBinding")]
    public class VKeyBinding : VFocusableSetting<KeyBindingOption> {
        
        const int NoBindingId = -1;
        
        [SerializeField] TMP_Text displayName;
        [SerializeField] TMP_Text infoText;
        [SerializeField] KeyIcon keyIcon;
        [SerializeField] ARButton button; 
        [SerializeField] GameObject actionChangeGameObject;
        [SerializeField] TextMeshProUGUI optionNameText;
        [SerializeField] ARButton leftButton;
        [SerializeField] ARButton rightButton;
        [SerializeField] GameObject icon;
        
        PopupUI _popup;
        ToggleOption _toggleOption;
        View _newKeyBindingView;
        PrefOption _option;
        bool _canActionBeChanged;
        bool _isBeingChanged;
        BindingData _newKeyBinding;
        Prompt _change;
        Prompt _unbind;
        
        public string ActionName => Option.Action.name;
        public Pole Pole => Option.AxisContribution;
        bool IsToggle => _toggleOption?.Enabled ?? false;
        string OptionText => IsToggle ? LocTerms.SettingsBindingToggle.Translate() : LocTerms.SettingsBindingHold.Translate();
        bool HasBinding => RewiredHelper.IsGamepad ? Option.CurrentBinding.elementIdentifierId != NoBindingId : Option.CurrentBinding.keyCode != KeyCode.None;
        
        public override void Setup(PrefOption option) {
            base.Setup(option);
            _option = option;
            UpdateIcon();
            Refresh();
            displayName.text = Option.DisplayName;
            infoText.text = "";
            KeyBindingOption keyBindingOption = (KeyBindingOption)option;
            button.OnClick += StartNewBinding;
            keyBindingOption.onChange += UpdateIcon;
            
            if (keyBindingOption.toggleOptionIsToggle != null) {
                _toggleOption = keyBindingOption.toggleOptionIsToggle;
                optionNameText.text = OptionText;
                actionChangeGameObject.SetActive(true);
                leftButton.OnClick += ChangeBindAction;
                rightButton.OnClick += ChangeBindAction;
            } else {
                actionChangeGameObject.SetActive(false);
            }
        }
        
        protected override void OnInitialize() {
            Target.ListenTo(VNewKeyBinding.Events.KeyPressed, OnNewKeyBindingSet, this);
            Target.ListenTo(VNewKeyBinding.Events.NewBindingCanceled, OnNewKeyBindingCanceled, this);
        }
        
        protected override IBackgroundTask OnDiscard() {
            KeyBindingOption keyBindingOption = (KeyBindingOption)_option;
            keyBindingOption.onChange -= UpdateIcon;
            return base.OnDiscard();
        }
        
        protected override void SpawnPrompts() {
            _change = Target.Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Accept.Translate()), Target);
            _unbind = Target.Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Unbind, LocTerms.Remove.Translate(), Unbind), Target);
        }
        
        protected override void RemovePrompts() {
            Target.Prompts.RemovePrompt(ref _change);
            Target.Prompts.RemovePrompt(ref _unbind);
        }

        protected override void Cleanup() {
            _popup?.Discard();
            _newKeyBindingView?.Discard();
        }

        protected override void Refresh() {
            optionNameText.text = OptionText;
        }
        
        void ChangeBindAction() {
            _toggleOption.Enabled = !_toggleOption.Enabled;
            Refresh();
        }
        
        void StartNewBinding() {
            _isBeingChanged = true;
            _newKeyBindingView = World.SpawnView(Target, typeof(VNewKeyBinding));
            infoText.text = LocTerms.SettingsPressNewKey.Translate();
            keyIcon.gameObject.SetActive(false);
        }

        void OnNewKeyBindingSet(KeyPressedData data) {
            _newKeyBinding = Option.CurrentBinding;
            
            if (data.controllerType == ControllerType.Keyboard) {
                _newKeyBinding.controller = ControllerType.Keyboard; 
                _newKeyBinding.keyCode = (KeyCode)data.id;
            } else if (data.controllerType == ControllerType.Mouse) {
                _newKeyBinding.controller = ControllerType.Mouse; 
                _newKeyBinding.elementIdentifierId = (int)ControllerKey.Mouse.LeftMouseButton + data.id;
            } else if (data.controllerType == ControllerType.Joystick) {
                // _newKeyBinding.elementIdentifierId = RewiredHelper.IsSony ? (int)ControllerKey.GetDualSense(data.id) : (int)ControllerKey.GetXbox(data.id);
                // _newKeyBinding.elementType = RewiredHelper.IsSony ? ElementTypeForPS(data.id) : ElementTypeForXbox(data.id);
            }

            if (_isBeingChanged) {
                if (IsAllowedForBinding(_newKeyBinding) && !ConflictsWithOtherBinding(_newKeyBinding)) {
                    Option.ChangeBinding(_newKeyBinding);
                    _isBeingChanged = false;
                    infoText.text = "";
                    Target.Trigger(ISettingHolder.Events.KeyProcessed, Target);
                } else if (IsAllowedForBinding(_newKeyBinding) && ConflictsWithOtherBinding(_newKeyBinding)) {
                    _isBeingChanged = false;
                    Target.Trigger(ISettingHolder.Events.KeyProcessed, Target);
                    _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                                        LocTerms.PopupOverrideBindingdSettings.Translate(),
                                        PopupUI.AcceptTapPrompt(OverrideBinding),
                                        PopupUI.CancelTapPrompt(DiscardPopup),
                                        LocTerms.PopupOverrideBindingdSettingsTitle.Translate()
                                    );
                }
            }
        }
        
        bool ConflictsWithOtherBinding(BindingData binding) {
            return World.ViewsFor(Target).AnyNonAlloc(view =>
                view is VKeyBinding kb &&
                kb != this &&
                kb.Option.CurrentBinding == binding &&
                !OverlappingControlsUtil.AllowedTogether(kb, this)
            );
        }

        void OnNewKeyBindingCanceled() {
            if (_isBeingChanged) {
                infoText.text = "";
                _isBeingChanged = false;
                keyIcon.gameObject.SetActive(true);
                Target.Trigger(ISettingHolder.Events.KeyProcessed, Target);
            }
        }

        void OverrideBinding() {
            Option.ChangeBinding(_newKeyBinding);
            DiscardPopup();
        }
        
        void DiscardPopup() {
            _popup?.Discard();
            _popup = null;
            infoText.text = "";
            _isBeingChanged = false;
            keyIcon.gameObject.SetActive(true);
        }
        
        void Unbind() {
            Option.ChangeBinding(new BindingData(Option.CurrentBinding.controller, NoBindingId, KeyCode.None));
        }
        
        void UpdateIcon() {
            if (HasBinding) {
                KeyIcon.Data data = new(UIKeyMapping.FindBindingFor(Option.CachedActionElementMap), false);
                keyIcon.Setup(data, this);
            }
            
            icon.SetActiveOptimized(HasBinding);
        }
        
        // static ControllerElementType ElementTypeForPS(int id) {
        //     return id switch {
        //         (int)ControllerKey.DualSense.L2 or (int)ControllerKey.DualSense.R2 or
        //             (int)ControllerKey.DualShock4.L2 or (int)ControllerKey.DualShock4.R2 => ControllerElementType.Axis,
        //         _ => ControllerElementType.Button
        //     };
        // }
        
        // static ControllerElementType ElementTypeForXbox(int id) {
        //     return id switch {
        //         (int)ControllerKey.Xbox.LeftTrigger or (int)ControllerKey.Xbox.RightTrigger => ControllerElementType.Axis,
        //         _ => ControllerElementType.Button
        //     };
        // }

        static bool IsAllowedForBinding(in BindingData binding) {
            if (binding.TryGetForMouse(out ControllerKey.Mouse button)) {
                return button is not (
                    ControllerKey.Mouse.LeftMouseButton or
                    ControllerKey.Mouse.RightMouseButton or
                    ControllerKey.Mouse.MouseHorizontal or
                    ControllerKey.Mouse.MouseVertical or
                    ControllerKey.Mouse.MouseWheel or
                    ControllerKey.Mouse.MouseWheelHorizontal
                    );
            }

            if (binding.TryGetForKeyboard(out ControllerKey.Keyboard key)) {
                return key is not (
                    ControllerKey.Keyboard.None or
                    ControllerKey.Keyboard.ESC
                    );
            }

            if (binding.TryGetForJoystick(out int id)) {
                // var lastController = ReInput.players.GetPlayer(0).controllers.GetLastActiveController();
                //
                // if (lastController.hardwareTypeGuid == ControllerKey.XboxOneGuid || lastController.hardwareTypeGuid == ControllerKey.Xbox360Guid) {
                //     return id is not (int)ControllerKey.Xbox.Guide;
                // }
                //
                // if (lastController.hardwareTypeGuid == ControllerKey.DualSenseGuid) {
                //     return id is not (
                //         (int)ControllerKey.DualSense.Mute or
                //         (int)ControllerKey.DualSense.PSButton or
                //         (int)ControllerKey.DualSense.Create
                //         );
                // }
                //
                // if (lastController.hardwareTypeGuid == ControllerKey.DualShock4Guid) {
                //     return id is not (
                //         (int)ControllerKey.DualShock4.Share or
                //         (int)ControllerKey.DualShock4.PSButton
                //         );
                // }
                //
                // return true;
            } 
            
            return false;
        }
    }
}