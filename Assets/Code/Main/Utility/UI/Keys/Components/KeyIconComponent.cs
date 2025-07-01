using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    public class KeyIconComponent : KeyIcon {
        [Title("Icons")]
        [SerializeField] Image icon;
        [SerializeField] int gamepadSizeAdd = 5;
        [SerializeField, CanBeNull] Image additionalImage;
        [Title("Hold")]
        [SerializeField, CanBeNull] GameObject holdHost;
        [SerializeField, CanBeNull] Image hold;
        [SerializeField, CanBeNull] Image holdPointer;
        [SerializeField] bool overrideDisableHold;
        [Title("Text")]
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Vector4 defaultTextMargin = new(3, 0, 3, 0);
        [Title("Host")]
        [SerializeField] LayoutElement layoutEle;
        
        float? _heightSize;
        float? _widthSize;
        bool IsHold => !overrideDisableHold && _data.IsHold && hold != null;
        
        public override void SetHoldPercent(float value) {
            if (IsHold) {
                hold!.fillAmount = value;
            } else {
                Log.Important?.Warning("Setting hold on key that is not a hold key or is missing the hold icon");
            }
        }

        protected override void SetupTextIcon(TextIcon textIcon) {
            gameObject.SetActive(true);
            ChangeIcon(icon, textIcon.Background, Image.Type.Sliced, false, ref _loadedIconRef);
            
            if (IsHold) {
                ChangeIcon(hold, textIcon.OverrideHoldAnimation, Image.Type.Filled, false, ref _loadedHoldRef);
            }
            
            bool hasAdditionalImage = textIcon.AdditionalImage is { IsSet: true };
            
            if (hasAdditionalImage) {
                ChangeIcon(additionalImage, textIcon.AdditionalImage, Image.Type.Simple, true, ref _loadedAdditionalImageRef);
            }
            
            additionalImage.TrySetActiveOptimized(hasAdditionalImage);
            
            holdHost.TrySetActiveOptimized(IsHold);
            holdPointer.TrySetActiveOptimized(!textIcon.DisableHoldPointer);
            text.text = textIcon.Text;
            text.margin = defaultTextMargin + textIcon.Margin;
            text.gameObject.SetActive(true);
        }

        protected override void SetupSpriteIcon(SpriteIcon spriteIcon) {
            float layoutHeightSize = spriteIcon.AddHeightSize;
            float layoutWidthSize = spriteIcon.AddWidthSize;
            
            _heightSize ??= layoutEle?.minHeight;
            _widthSize ??= layoutEle?.minWidth;
            gameObject.SetActive(true);
            ChangeIcon(icon, spriteIcon.Sprite, Image.Type.Simple, true, ref _loadedIconRef);
            
            if (IsHold) {
                ChangeIcon(hold, spriteIcon.OverrideHoldAnimation, Image.Type.Filled, false, ref _loadedHoldRef);
            }
            
            bool hasAdditionalImage = spriteIcon.AdditionalImage is { IsSet: true };
            
            if (hasAdditionalImage) {
                ChangeIcon(additionalImage, spriteIcon.AdditionalImage, Image.Type.Simple, true, ref _loadedAdditionalImageRef);
            }
            
            additionalImage.TrySetActiveOptimized(hasAdditionalImage);
            
            holdHost.TrySetActiveOptimized(IsHold);
            holdPointer.TrySetActiveOptimized(!spriteIcon.DisableHoldPointer);
            text.gameObject.SetActive(false);
            
            if (_heightSize != null && _widthSize != null && layoutEle != null) {
                layoutHeightSize += _heightSize.Value;
                layoutWidthSize += _widthSize.Value;
                
                layoutEle.minHeight = layoutHeightSize;
                layoutEle.minWidth = layoutWidthSize;
            }
        }
        
        protected override void OnIconNull() {
            gameObject.SetActive(false);
        }
        
        protected override void OnUnknownIconType() {
            OnIconNull();
        }
        
        protected override void OnPostIconSetup() {
            if (layoutEle != null && _currentScheme == ControlScheme.Gamepad) {
                layoutEle.minHeight += gamepadSizeAdd;
                layoutEle.minWidth += gamepadSizeAdd;
            }
        }

        void ChangeIcon(Image target, SpriteReference sprite, Image.Type type, bool preserveAspect, ref SpriteReference loadedReference) {
            target.type = type;
            target.preserveAspect = preserveAspect;
            target.fillAmount = 0;
            loadedReference?.Release();
            
            if (sprite != null) {
                loadedReference = sprite;
                loadedReference.SetSprite(target);
                target.gameObject.SetActive(true);
            } else {
                loadedReference = null;
                target.gameObject.SetActive(false);
                Log.Important?.Error($"No icon sprite reference for: {KeyBindingLog}", gameObject);
            }
        }
    }
}