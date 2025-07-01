using Awaken.TG.Assets;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    public class PKeyIcon : KeyIcon {
        [SerializeField] VGenericPresenterPrompt promptPresenter;

        VisualElement _keyIconRoot;
        VisualElement _keyIcon;
        VisualOutlineFillBar _holdFillBar;
        BetterOutlinedLabel _keyLabel;

        public override void Setup(in Data data, IListenerOwner view) {
            CacheVisualElements(promptPresenter.Prompt);
            base.Setup(data, view);
        }

        public void CacheVisualElements(VisualElement contentRoot) {
            _keyIconRoot = contentRoot.Q<VisualElement>("key-icon-root");
            _keyIcon = _keyIconRoot.Q<VisualElement>("key-icon");
            _keyLabel = _keyIcon.Q<BetterOutlinedLabel>("key-label");
            _holdFillBar = _keyIconRoot.Q<VisualOutlineFillBar>();
        }

        public override void SetHoldPercent(float value) {
            if (_data.IsHold) {
                _holdFillBar.Progress = value;
            } else {
                Log.Important?.Warning("Setting hold on key that is not a hold key or is missing the hold icon");
            }
        }

        protected override void SetupTextIcon(TextIcon textIcon) {
            ChangeIcon(_keyIcon, textIcon.Background, ref _loadedIconRef);
            _keyIcon.StretchToParentSize();
            _holdFillBar.StretchToParentSize();
            
            _keyIcon.SetActiveOptimized(true);
            _keyLabel.text = textIcon.Text;
            _keyLabel.SetActiveOptimized(true);
            
            _holdFillBar.ShapeType = textIcon.HoldAnimationUTKShape;
            _holdFillBar.SetActiveOptimized(_data.IsHold);
        }

        protected override void SetupSpriteIcon(SpriteIcon spriteIcon) {
            ChangeIcon(_keyIcon, spriteIcon.Sprite, ref _loadedIconRef);
            _keyIcon.StretchToParentSize();
            _holdFillBar.StretchToParentSize();
            
            _keyIcon.SetActiveOptimized(true);
            _keyLabel.SetActiveOptimized(false);
            
            _holdFillBar.ShapeType = spriteIcon.HoldAnimationUTKShape;
            _holdFillBar.SetActiveOptimized(_data.IsHold);
        }

        protected override void OnIconNull() {
            _keyIcon.SetActiveOptimized(false);
        }

        protected override void OnUnknownIconType() {
            OnIconNull();
        }

        void ChangeIcon(VisualElement target, SpriteReference sprite, ref SpriteReference loadedReference) {
            loadedReference?.Release();

            if (sprite != null) {
                loadedReference = sprite;
                loadedReference.SetSprite(target);
            } else {
                loadedReference = null;
                Log.Important?.Error($"No icon sprite reference for: {KeyBindingLog}", gameObject);
            }
        }
    }
}