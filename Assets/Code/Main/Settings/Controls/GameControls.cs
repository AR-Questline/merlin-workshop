using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;
using Rewired;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class GameControls : Setting, IRewiredSetting {
        static Player Player => RewiredHelper.Player;
        
        public ControlScheme ControlScheme { get; }
        public Guid controllerIdentifier;
        List<KeyBindingOption> _options = new();
        ToggleOption _toggleSprintKeyboard = new("Option_ToggleSprint", "Option_ToggleSprint ", false, true);
        ToggleOption _toggleWalkKeyboard = new("Option_ToggleWalk", "Option_ToggleWalk ", false, true);
        ToggleOption _toggleCrouchKeyboard = new("Option_ToggleCrouch", "Option_ToggleCrouch ", true, true);
        
        public override string SettingName => "GameControls";
        public override IEnumerable<PrefOption> Options => _options;
        public IEnumerable<KeyBindingOption> KeyBindings => _options;
        public override bool IsVisible => BelongsToCurrentController;
        public bool IsSprintToggle => RewiredHelper.IsGamepad || _toggleSprintKeyboard.Enabled;
        public bool IsWalkToggle => RewiredHelper.IsGamepad || _toggleWalkKeyboard.Enabled;
        public bool IsCrouchToggle => RewiredHelper.IsGamepad || _toggleCrouchKeyboard.Enabled;
        bool BelongsToCurrentController => true; //controllerIdentifier == Player.controllers.GetLastActiveController().hardwareTypeGuid || ControlScheme == ControlScheme.KeyboardAndMouse;
        
        public GameControls(ControlScheme controlScheme, int controllerId = 0, Guid guid = default) {
            if (PlatformUtils.IsPS5) {
                //ReInput.touch.simulateMouseWithTouches = false;
                Input.simulateMouseWithTouches = false;
            }
            
            ControlScheme = controlScheme;
            controllerIdentifier = guid;

            if (ControlScheme == ControlScheme.KeyboardAndMouse) {
                AutoBindKeyboardLayout();
            }
            
            InitOptions(controllerId);
        }

        public override void RestoreDefault() {
            if (BelongsToCurrentController) {
                base.RestoreDefault();
            }
        }
        
        void AutoBindKeyboardLayout() {
            bool isAzerty = KeyboardLayoutDetector.IsAzertyKeyboard();

            Log.Marking?.Warning($"Set keyboard layout to: {(isAzerty ? "AZERTY" : "QWERTY")}");
            int currentLayoutId = isAzerty ? RewiredHelper.AzertyKeyboardLayoutId : RewiredHelper.DefaultKeyboardLayoutId;
            
            // foreach (var map in Player.controllers.maps.GetMaps(ControllerType.Keyboard, 0)) {
            //     map.enabled = map.layoutId == currentLayoutId;
            // }
            
            Services.Get<UIKeyMapping>().RefreshCache();
        }

        void InitOptions(int controllerId) {
            // foreach (var category in ReInput.mapping.UserAssignableMapCategories) {
            //     foreach (var controller in ControlScheme.Controllers()) {
            //         foreach (var controllerMap in Player.controllers.maps.GetMapsInCategory(controller, controllerId, category.id)) {
            //             if (controllerMap.enabled == false) continue;
            //             foreach (var actionMap in controllerMap.AllMaps) {
            //                 var action = ReInput.mapping.GetAction(actionMap.actionId);
            //                 if (action.userAssignable && !OverlappingControlsUtil.IsElementOfMergeGroup(action.name) && ActionHasAnyIcon(actionMap, action)) {
            //                     var option = new KeyBindingOption(controller, action, actionMap);
            //                     option.onChange += () => Services.Get<UIKeyMapping>().RefreshMapping();
            //                     _options.Add(option);
            //                 }
            //             }
            //         }
            //     }
            // }
            //
            // if (ControlScheme == ControlScheme.KeyboardAndMouse) {
            //     _options.Single(setting => setting.ID == "Sprint").toggleOptionIsToggle = _toggleSprintKeyboard;
            //     _options.Single(setting => setting.ID == "Walk").toggleOptionIsToggle = _toggleWalkKeyboard;
            //     _options.Single(setting => setting.ID == "Crouch").toggleOptionIsToggle = _toggleCrouchKeyboard;
            // }
            //
            // LoadAll();
        }

        // bool ActionHasAnyIcon(ActionElementMap element, InputAction action) {
        //     KeyIcon.Data data = new(UIKeyMapping.FindBindingFor(element, action), false);
        //     var icon = data.GetIcons()[ControlScheme];
        //     bool shouldAdd = false;
        //     if (icon is SpriteIcon spriteIcon) {
        //         shouldAdd = spriteIcon.Sprite.IsSet;
        //     } else if (icon is TextIcon textIcon) {
        //         shouldAdd = !string.IsNullOrEmpty(textIcon.Text);
        //     }
        //
        //     return shouldAdd;
        // }

        protected override void OnApply() {
            SaveAll();
        }

        void SaveAll() {
            SaveBehaviours();
            SaveOverrides();
        }
        
        void SaveBehaviours() {
            // foreach (InputBehavior behavior in Player.GetSaveData(true).inputBehaviors) {
            //     string key = InputBehaviourKey(Player, behavior);
            //     PrefMemory.Set(key, behavior.ToXmlString(), true);
            // }
        }
        
        void SaveOverrides() {
            var overrides = _options
                .Where(option => !option.IsOriginal)
                .Select(option => new BindingOverride(option))
                .ToList();
            var json = JsonConvert.SerializeObject(overrides, LoadSave.Settings);
            PrefMemory.Set(ControlSchemeKey(Player, ControlScheme), json, true);
        }

        void LoadAll() {
            LoadBehaviours();
            LoadBindingOverrides();
        }
        
        void LoadBehaviours() {
            // // all players have an instance of each input behavior so it can be modified
            // IList<InputBehavior> behaviors = ReInput.mapping.GetInputBehaviors(Player.id); // get all behaviors from player
            // for (int i = 0; i < behaviors.Count; i++) {
            //     string json = PrefMemory.GetString(InputBehaviourKey(Player, behaviors[i])); // try to the behavior for this id
            //     if (string.IsNullOrWhiteSpace(json)) continue; // no data found for this behavior
            //     behaviors[i].ImportXmlString(json); // import the data into the behavior
            // }
        }
        
        void LoadBindingOverrides() {
            string data = PrefMemory.GetString(ControlSchemeKey(Player, ControlScheme));
            if (data.IsNullOrWhitespace()) return;
            var overrides = JsonConvert.DeserializeObject<List<BindingOverride>>(data, LoadSave.Settings);
            if (overrides == null) return;
            foreach (var bindingOverride in overrides) {
                var option = _options.FirstOrDefault(bindingOverride.IsMine);
                if (option == null) continue;
                option.ChangeBinding(bindingOverride.Binding);
                option.Apply();
            }
        }

        // static string InputBehaviourKey(Player player, InputBehavior behaviour) => $"rewiredBeh:{player.id}:{behaviour.id}";
        static string ControlSchemeKey(Player player, ControlScheme scheme) => $"rewired:Player_{player.id}:{scheme}";
    }

    public partial struct BindingOverride {
        public ushort TypeForSerialization => SavedTypes.BindingOverride;

        [Saved] int _actionId;
        [Saved] BindingData _binding;

        public BindingData Binding => _binding;

        public BindingOverride(KeyBindingOption option) {
            _actionId = option.Action.id;
            _binding = option.CurrentBinding;
        }

        public bool IsMine(KeyBindingOption option) {
            return option.Action.id == _actionId && option.AxisContribution == _binding.axisContribution;
        }
    }
}