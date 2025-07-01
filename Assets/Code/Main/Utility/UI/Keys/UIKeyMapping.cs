using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Rewired;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys {
    public class UIKeyMapping : ScriptableObject, IService {

        static readonly ByControlScheme<KeyIcons> EmptyKeyIconsByController = new();
        //static readonly List<ControllerTemplateElementTarget> Targets = new();
        
        readonly Dictionary<KeyBindings, ByControlScheme<KeyIcons>> _cache = new();
        
        [SerializeField, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, CellPadding = 10)] GamepadTemplateIcons[] gamepadTemplate = Array.Empty<GamepadTemplateIcons>();
        [SerializeField, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, CellPadding = 10)] XboxIcons[] xbox = Array.Empty<XboxIcons>();
        [SerializeField, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, CellPadding = 10)] DualSense[] dualSense = Array.Empty<DualSense>();
        [SerializeField, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, CellPadding = 10)] KeyboardIcons[] keyboard = Array.Empty<KeyboardIcons>();
        [SerializeField, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, CellPadding = 10)] MouseIcons[] mouse = Array.Empty<MouseIcons>();
        
        // === Cache
        
        public static void EDITOR_RuntimeReset() {
            //Targets.Clear();
        }
        
        public void Init() {
            // ReInput.ControllerConnectedEvent -= OnConnectedControllersChanged;
            // ReInput.ControllerConnectedEvent += OnConnectedControllersChanged;
            // ReInput.ControllerDisconnectedEvent -= OnConnectedControllersChanged;
            // ReInput.ControllerDisconnectedEvent += OnConnectedControllersChanged;
            RefreshMapping();
        }
        
        // void OnConnectedControllersChanged(ControllerStatusChangedEventArgs args) {
        //     RefreshMapping();
        // }

        public void RefreshMapping() {
            RefreshCache();
            World.Any<Focus>()?.Trigger(Focus.Events.KeyMappingRefreshed, this);
        }
        
        public void RefreshCache() {
            _cache.Clear();

            // var maps = RewiredHelper.Player.controllers.maps.GetAllMaps().Where(map => map.enabled).SelectMany(map => map.AllMaps);
            // var lastActiveController = ReInput.players.GetPlayer(0).controllers.GetLastActiveController();
            // if (lastActiveController?.type != ControllerType.Joystick) {
            //     lastActiveController = null;
            // }
            // foreach (var element in maps) {
            //     var controllerType = element.controllerMap.controllerType;
            //
            //     KeyBindings binding = FindBindingFor(element);
            //     if (binding == null) {
            //         continue;
            //     }
            //     
            //     if (!_cache.TryGetValue(binding, out var mappingByController)) {
            //         _cache[binding] = mappingByController = new ByControlScheme<KeyIcons>();
            //     }
            //
            //     mappingByController[ControlSchemes.Get(controllerType)] = controllerType switch {
            //         ControllerType.Keyboard => GetIconOf(ControllerKey.GetKeyboard(element)),
            //         ControllerType.Mouse => GetIconOf(ControllerKey.GetMouse(element)),
            //         ControllerType.Joystick => GetIconOfJoystick(element, lastActiveController),
            //         _ => throw new ArgumentOutOfRangeException(),
            //     };
            // }

            TryAddManuallyMouseIcon(KeyBindings.Gameplay.Attack, ControllerKey.Mouse.LeftMouseButton);
            TryAddManuallyMouseIcon(KeyBindings.Gameplay.AttackHeavy, ControllerKey.Mouse.LeftMouseButton);
            TryAddManuallyMouseIcon(KeyBindings.Gameplay.Block, ControllerKey.Mouse.RightMouseButton);
        }

        void TryAddManuallyMouseIcon(KeyBindings binding, ControllerKey.Mouse mouseButton) {
            if (!_cache.TryGetValue(binding, out _)) {
                _cache[binding] = new ByControlScheme<KeyIcons>();
            }
            
            _cache[binding][ControlScheme.KeyboardAndMouse] = GetIconOf(mouseButton);
        }

        public static KeyBindings FindBindingFor(ActionElementMap element, InputAction action = null) {
            // we use actionDescriptiveName to handle axis defined as multiple keys on keyboard (eg. WASD)
            string name = "";//element.actionDescriptiveName;

            // HACK for hard to track error causing actionDescriptiveName to be Action0 for InputActions with correct names
            // if (name is null or "" or "Action0") {
            //     // action ??= ReInput.mapping.GetAction(element.actionId);
            //     // name = (element.axisRange, element.axisContribution) switch {
            //     //     (AxisRange.Full, _) => action.descriptiveName,
            //     //     (_, Pole.Positive) => action.positiveDescriptiveName,
            //     //     (_, Pole.Negative) => action.negativeDescriptiveName,
            //     //     _ => action.name,
            //     // };
            //     // if (name is null or "" or "Action0") {
            //     //     name = action.name;
            //     // }
            // }
            //
            // if (name.StartsWith("KeyBind/")) {
            //     name = name[8..];
            // }

            return RichEnumCache.GetDerived<KeyBindings>().FirstOrDefault(b => b == name);
        }

        KeyIcons GetIconOfJoystick(ActionElementMap element, Controller controller) {
            // Guid guid = controller?.hardwareTypeGuid ?? element.controllerMap.controller.hardwareTypeGuid;
            //
            // if (guid == ControllerKey.Xbox360Guid || guid == ControllerKey.XboxOneGuid) {
            //     return GetIconOf(Mapped(ControllerKey.GetXbox(element)));
            // }
            //
            // if (guid == ControllerKey.DualShock2Guid) {
            //     return GetIconOf(Mapped(ControllerKey.GetDualShock2(element)));
            // }
            //
            // if (guid == ControllerKey.DualShock3Guid) {
            //     return GetIconOf(Mapped(ControllerKey.GetDualShock3(element)));
            // }
            //
            // if (guid == ControllerKey.DualShock4Guid) {
            //     return GetIconOf(Mapped(ControllerKey.GetDualShock4(element)));
            // }
            //
            // if (guid == ControllerKey.DualSenseGuid) {
            //     return GetIconOf(Mapped(ControllerKey.GetDualSense(element)));
            // }
            //
            // if (controller == null) {
            //     Log.Important?.Warning("Unsupported controller, using xbox mapping");
            //     return GetIconOf(Mapped(ControllerKey.GetXbox(element)));
            // }
            
            // if (controller.templateCount > 0) {
            //     var template = controller.Templates[0];
            //     if (template.GetElementTargets(element, Targets) > 0) {
            //         if (Targets[0].template.typeGuid == ControllerKey.GamepadTemplateGuid) {
            //             return GetIconOf(Mapped(ControllerKey.GetGamepadTemplate(Targets[0].element)));
            //         }
            //     }
            // }
            
            // Log.Important?.Error($"Unsupported controller type {controller.hardwareName} {controller.hardwareTypeGuid}");
            return null;
        }

        static ControllerKey.DualSense Mapped(ControllerKey.DualShock2 dualShock) => dualShock.ToDualSense();
        static ControllerKey.DualSense Mapped(ControllerKey.DualShock3 dualShock) => dualShock.ToDualSense();
        static ControllerKey.DualSense Mapped(ControllerKey.DualShock4 dualShock) => dualShock.ToDualSense();
        static ControllerKey.DualSense Mapped(ControllerKey.DualSense dualSense) => dualSense;
        static ControllerKey.Xbox Mapped(ControllerKey.Xbox xbox) => xbox;
        static ControllerKey.Xbox Mapped(ControllerKey.GamepadTemplate template) => template.ToXbox();

        // === Retrieving
        
        // -- By KeyBindings
        
        public ByControlScheme<IIconSearchResult> GetIconsOf(KeyBindings key, bool hold) {
            return GetIconsByControlSchemaOf(key).Transformed(mapping => mapping?.GetIcon(hold));
        }
        
        public IIconSearchResult GetIconOf(KeyBindings key, bool hold, ControlScheme scheme) {
            return GetIconsByControlSchemaOf(key)[scheme]?.GetIcon(hold);
        }
        
        public string GetDisplayNameOf(KeyBindings key, bool hold, ControlScheme scheme) {
            var controllerSchema = GetIconsByControlSchemaOf(key)[scheme];
            return controllerSchema?.GetDisplayName(hold);
        }
        
        ByControlScheme<KeyIcons> GetIconsByControlSchemaOf(KeyBindings key) {
            return key != null && _cache.TryGetValue(key, out var mapping)
                ? mapping 
                : EmptyKeyIconsByController;
        }

        // -- By Key
        
        [UnityEngine.Scripting.Preserve] 
        public IIconSearchResult GetIconOf(ControllerKey.Keyboard key, bool hold) => GetIconOf(key).GetIcon(hold);
        [UnityEngine.Scripting.Preserve] 
        public IIconSearchResult GetIconOf(ControllerKey.Mouse key, bool hold) => GetIconOf(key).GetIcon(hold);
        [UnityEngine.Scripting.Preserve] 
        public IIconSearchResult GetIconOf(ControllerKey.GamepadTemplate key, bool hold) => GetIconOf(key).GetIcon(hold);
        [UnityEngine.Scripting.Preserve] 
        public IIconSearchResult GetIconOf(ControllerKey.Xbox key, bool hold) => GetIconOf(key).GetIcon(hold);
        [UnityEngine.Scripting.Preserve] 
        public IIconSearchResult GetIconOf(ControllerKey.DualSense key, bool hold) => GetIconOf(key).GetIcon(hold);

        public IIconSearchResult GetCustomIconOf(ControllerKey.CustomVisualOnlyKey key, bool hold, ControlScheme scheme) {
            return GetIconOf(key, scheme)?.GetIcon(hold);
        }

        KeyIcons GetIconOf(ControllerKey.Keyboard key) => keyboard[(int) key];
        KeyIcons GetIconOf(ControllerKey.Mouse key) => mouse[(int) key];
        KeyIcons GetIconOf(ControllerKey.GamepadTemplate key) => gamepadTemplate[(int) key];
        KeyIcons GetIconOf(ControllerKey.Xbox key) => xbox[(int) key];
        KeyIcons GetIconOf(ControllerKey.DualSense key) => dualSense[(int) key];
        KeyIcons GetIconOf(ControllerKey.CustomVisualOnlyKey key, ControlScheme scheme) {
            bool isGamepad = scheme == ControlScheme.Gamepad;
            if (isGamepad) {
                return RewiredHelper.IsSony ? GetIconOf(key.ToDualSense()) : GetIconOf(key.ToXbox());
            }

            return mouse[(int) key.ToMouse()];
        }

        // === Icons

        // -- Icon Types
        
        enum IconType : byte {
            Sprite,
            Text,
        }
        
        [Serializable]
        struct SpriteIcon {
            [FoldoutGroup("Base Setup", true), UIAssetReference(AddressableLabels.UI.Input)]
            public ShareableSpriteReference icon;
            [FoldoutGroup("Base Setup")] public string displayName;
            [FoldoutGroup("Base Setup")] public int addWidthSize;
            [FoldoutGroup("Base Setup")] public int addHeightSize;
            [FoldoutGroup("Base Setup"), UIAssetReference(AddressableLabels.UI.Input)] 
            public ShareableSpriteReference additionalImage;
            
            [FoldoutGroup("Hold Specific", true)]
            [SerializeField] bool showHoldSection;
            [FoldoutGroup("Hold Specific")]
            [UIAssetReference(AddressableLabels.UI.Input), ShowIf(nameof(showHoldSection))]
            public ShareableSpriteReference overrideHoldAnimation;
            [FoldoutGroup("Hold Specific"), ShowIf(nameof(showHoldSection))] public bool disableHoldPointer;
            [FoldoutGroup("Hold Specific"), ShowIf(nameof(showHoldSection))] public VisualOutlineFillBar.Shape holdAnimationShapeForUTK;
        }

        [Serializable]
        struct TextIcon {
            [FoldoutGroup("Base Setup", true), UIAssetReference(AddressableLabels.UI.Input)]
            public ShareableSpriteReference background;
            [FoldoutGroup("Base Setup")] public string text;
            [FoldoutGroup("Base Setup")] public string displayName;
            [FoldoutGroup("Base Setup")] public Vector4 margin;
            [FoldoutGroup("Base Setup"), UIAssetReference(AddressableLabels.UI.Input)] 
            public ShareableSpriteReference additionalImage;
            
            [FoldoutGroup("Hold Specific", true)]
            [SerializeField] bool showHoldSection;
            [FoldoutGroup("Hold Specific")]
            [UIAssetReference(AddressableLabels.UI.Input), ShowIf(nameof(showHoldSection))]
            public ShareableSpriteReference overrideHoldAnimation;
            [FoldoutGroup("Hold Specific"), ShowIf(nameof(showHoldSection))] public bool disableHoldPointer;
            [FoldoutGroup("Hold Specific"), ShowIf(nameof(showHoldSection))] public VisualOutlineFillBar.Shape holdAnimationShapeForUTK;
        }

        // -- Key Data
        
        [Serializable]
        abstract class KeyIcons {
            [SerializeField, InlineProperty, HideLabel]
            [VerticalGroup("Icon Tap", 1), ShowIf(nameof(IsSprite))] 
            public SpriteIcon spriteTap;
            [SerializeField, InlineProperty, HideLabel]
            [VerticalGroup("Icon Tap", 1), ShowIf(nameof(IsText))] 
            public TextIcon textTap;
            
            [SerializeField, InlineProperty, HideLabel]
            [VerticalGroup("Icon Hold", 1), ShowIf(nameof(IsSprite))] 
            public SpriteIcon spriteHold;
            [SerializeField, InlineProperty, HideLabel]
            [VerticalGroup("Icon Hold", 1), ShowIf(nameof(IsText))] 
            public TextIcon textHold;

            public IIconSearchResult GetIcon(bool hold) {
                return IconType switch {
                    IconType.Sprite => IconSearchResult(hold ? spriteHold : spriteTap),
                    IconType.Text => IconSearchResult(hold ? textHold : textTap),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            public string GetDisplayName(bool hold) {
                return IconType switch {
                    IconType.Sprite => DisplayName(hold ? spriteHold : spriteTap),
                    IconType.Text => DisplayName(hold ? textHold : textTap),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            
            public abstract IconType IconType { get; }
            protected bool IsSprite() => IconType == IconType.Sprite;
            protected bool IsText() => IconType == IconType.Text;

            static IIconSearchResult IconSearchResult(in SpriteIcon icon) => new Keys.SpriteIcon {
                Sprite = icon.icon.IsSet? icon.icon.Get() : null,
                AddWidthSize = icon.addWidthSize,
                AddHeightSize = icon.addHeightSize,
                OverrideHoldAnimation = icon.overrideHoldAnimation.IsSet ? icon.overrideHoldAnimation.Get() : null,
                AdditionalImage = icon.additionalImage.IsSet ? icon.additionalImage.Get() : null,
                DisableHoldPointer = icon.disableHoldPointer,
                HoldAnimationUTKShape = icon.holdAnimationShapeForUTK
            };

            static IIconSearchResult IconSearchResult(in TextIcon icon) => new Keys.TextIcon {
                Background = icon.background.IsSet ? icon.background.Get() : null,
                Text = icon.text,
                Margin = icon.margin,
                OverrideHoldAnimation = icon.overrideHoldAnimation.IsSet ? icon.overrideHoldAnimation.Get() : null,
                AdditionalImage = icon.additionalImage.IsSet ? icon.additionalImage.Get() : null,
                DisableHoldPointer = icon.disableHoldPointer,
                HoldAnimationUTKShape = icon.holdAnimationShapeForUTK
            };
            
            static string DisplayName(in SpriteIcon icon) {
                string iconSpriteName = icon.icon.arSpriteReference.SubObjectName;
                return string.IsNullOrEmpty(iconSpriteName) ? icon.displayName : iconSpriteName;
            }
            static string DisplayName(in TextIcon icon) => icon.displayName;
        }
        
        [Serializable]
        abstract class KeyIcons<TKey> : KeyIcons {
            [SerializeField, ReadOnly, HideLabel]
            [VerticalGroup("Key"), TableColumnWidth(180, false)]
            TKey key;
            
            [SerializeField, HideLabel]
            [VerticalGroup("Key"), TableColumnWidth(180, false)]
            public IconType type;

            protected KeyIcons(TKey key) {
                this.key = key;
            }

            [UnityEngine.Scripting.Preserve]
            public TKey Key => key;
            public override IconType IconType => type;
        }

        [Serializable] 
        class GamepadTemplateIcons : KeyIcons<ControllerKey.GamepadTemplate> {
            public GamepadTemplateIcons(ControllerKey.GamepadTemplate key) : base(key) { }
        }
        
        [Serializable] 
        class XboxIcons : KeyIcons<ControllerKey.Xbox> {
            public XboxIcons(ControllerKey.Xbox key) : base(key) { }
        }
        
        [Serializable] 
        class DualShock2 : KeyIcons<ControllerKey.DualShock2> {
            public DualShock2(ControllerKey.DualShock2 key) : base(key) { }
        }
        
        [Serializable] 
        class DualShock3 : KeyIcons<ControllerKey.DualShock3> {
            public DualShock3(ControllerKey.DualShock3 key) : base(key) { }
        }
        
        [Serializable] 
        class DualShock4 : KeyIcons<ControllerKey.DualShock4> {
            public DualShock4(ControllerKey.DualShock4 key) : base(key) { }
        }
        
        [Serializable] 
        class DualSense : KeyIcons<ControllerKey.DualSense> {
            public DualSense(ControllerKey.DualSense key) : base(key) { }
        }
        
        [Serializable] 
        class KeyboardIcons : KeyIcons<ControllerKey.Keyboard> {
            public KeyboardIcons(ControllerKey.Keyboard key) : base(key) { }
        }
        
        [Serializable] 
        class MouseIcons : KeyIcons<ControllerKey.Mouse> {
            public MouseIcons(ControllerKey.Mouse key) : base(key) { }
        }
        
#if UNITY_EDITOR
        [FoldoutGroup("Debug & Dev")]
        [SerializeField, FoldoutGroup("Debug & Dev/Clear"), UIAssetReference(AddressableLabels.UI.Input)] 
        ShareableSpriteReference keyboardBackground;
        
        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearAll() {
            ClearGamepadTemplate();
            ClearXbox();
            ClearDualSense();
            ClearKeyboard();
            ClearMouse();
            _cache.Clear();
        }
        
        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearGamepadTemplate() {
            gamepadTemplate = new GamepadTemplateIcons[ControllerKey.GamepadCount];
            for (int i = 0; i < ControllerKey.GamepadCount; i++) {
                gamepadTemplate[i] = new GamepadTemplateIcons((ControllerKey.GamepadTemplate)i) {
                    type = IconType.Sprite
                };
            }
        }

        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearXbox() {
            xbox = new XboxIcons[ControllerKey.XboxCount];
            for (int i = 0; i < ControllerKey.XboxCount; i++) {
                xbox[i] = new XboxIcons((ControllerKey.Xbox)i) {
                    type = IconType.Sprite
                };
            }
        }

        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearDualSense() {
            dualSense = new DualSense[ControllerKey.DualSenseCount];
            for (int i = 0; i < ControllerKey.DualSenseCount; i++) {
                dualSense[i] = new DualSense((ControllerKey.DualSense)i) {
                    type = IconType.Sprite
                };
            }
        }

        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearKeyboard() {
            keyboard = new KeyboardIcons[ControllerKey.KeyboardCount];
            for (int i = 0; i < ControllerKey.KeyboardCount; i++) {
                keyboard[i] = new KeyboardIcons((ControllerKey.Keyboard)i) {
                    type = IconType.Sprite
                };
            }
        }
        
        [Button, FoldoutGroup("Debug & Dev/Clear")]
        void ClearMouse() {
            mouse = new MouseIcons[ControllerKey.MouseCount];
            for (int i = 0; i < ControllerKey.MouseCount; i++) {
                mouse[i] = new MouseIcons((ControllerKey.Mouse) i) {
                    type = IconType.Sprite
                };
            }
        }

        [FoldoutGroup("Debug & Dev/Fill"), ShowInInspector, UIAssetReference(AddressableLabels.UI.Input)]
        ARAssetReference _spriteForFill = new();
        
        [Button, FoldoutGroup("Debug & Dev/Fill")]
        void FillKeyboardHoldWith(bool textIconBackground) {
            if (_spriteForFill is not { IsSet: true }) {
                return;
            }
            
            foreach (KeyboardIcons keyboardIcons in keyboard) {
                if (textIconBackground) {
                    if (keyboardIcons.IconType == IconType.Text) {
                        keyboardIcons.textHold.background = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    }
                } else {
                    if (keyboardIcons.IconType == IconType.Text) {
                        keyboardIcons.textHold.overrideHoldAnimation = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    } else {
                        keyboardIcons.spriteHold.overrideHoldAnimation = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [Button, FoldoutGroup("Debug & Dev/Fill")]
        void FillMouseHoldWith(bool textIconBackground) {
            if (_spriteForFill is not { IsSet: true }) {
                return;
            }
            
            foreach (MouseIcons keyboardIcons in mouse) {
                if (textIconBackground) {
                    if (keyboardIcons.IconType == IconType.Text) {
                        keyboardIcons.textHold.background = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    }
                } else {
                    if (keyboardIcons.IconType == IconType.Text) {
                        keyboardIcons.textHold.overrideHoldAnimation = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    } else {
                        keyboardIcons.spriteHold.overrideHoldAnimation = new ShareableSpriteReference(new SpriteReference() { arSpriteReference = _spriteForFill });
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}