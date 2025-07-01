using Awaken.TG.Assets;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Debugging;
using EnhydraGames.BetterTextOutline;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.CustomControls {
    /// <summary>
    /// For now it's a modified copy of PKeyIcon for notifications purposes so we don't have to spawn all redundant MVC Views.
    /// </summary>
    public class VisualPresenterKeyIcon : Presenter<IModel> {
        VisualElement _keyIconRoot;
        VisualElement _keyIcon;
        VisualOutlineFillBar _holdFillBar;
        BetterOutlinedLabel _keyLabel;
        BetterOutlinedLabel _nameLabel;

        KeyIcon.Data _data;
        ControlScheme _currentScheme;
        SpriteReference _loadedIconRef;
        ByControlScheme<IIconSearchResult> _icons;

        public VisualPresenterKeyIcon(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _keyIconRoot = Content.Q<VisualElement>("key-icon-root");
            _nameLabel = Content.Q<BetterOutlinedLabel>("prompt-label");
            _keyIcon = _keyIconRoot.Q<VisualElement>("key-icon");
            _keyLabel = _keyIcon.Q<BetterOutlinedLabel>("key-label");
            _holdFillBar = _keyIconRoot.Q<VisualOutlineFillBar>();
        }

        protected override void OnAfterDiscard() {
            _loadedIconRef?.Release();
        }

        public void Setup(Prompt prompt) {
            IUniqueKeyProvider uniqueKeyProvider = prompt;
            _data = uniqueKeyProvider.UniqueKey;

            var focus = World.Only<Focus>();
            focus.ListenTo(Focus.Events.KeyMappingRefreshed, RefreshIcons, prompt);
            focus.ListenTo(Focus.Events.ControllerChanged, RefreshIcon, prompt);

            RefreshIcons();
            _nameLabel.text = prompt.ActionName;
        }

        [UnityEngine.Scripting.Preserve]
        public void SetHoldPercent(float value) {
            if (_data.IsHold) {
                _holdFillBar.Progress = value;
            } else {
                Log.Important?.Warning("Setting hold on key that is not a hold key or is missing the hold icon");
            }
        }

        protected void SetupTextIcon(TextIcon textIcon) {
            ChangeIcon(_keyIcon, textIcon.Background, ref _loadedIconRef);
            _keyIcon.StretchToParentSize();
            _holdFillBar.StretchToParentSize();
            
            _keyIcon.SetActiveOptimized(true);
            _keyLabel.text = textIcon.Text;
            _keyLabel.SetActiveOptimized(true);
            
            _holdFillBar.ShapeType = RewiredHelper.IsGamepad ? VisualOutlineFillBar.Shape.Circle : VisualOutlineFillBar.Shape.Square;
            _holdFillBar.SetActiveOptimized(_data.IsHold);
        }

        protected void SetupSpriteIcon(SpriteIcon spriteIcon) {
            ChangeIcon(_keyIcon, spriteIcon.Sprite, ref _loadedIconRef);
            _keyIcon.StretchToParentSize();
            _holdFillBar.StretchToParentSize();
            
            _keyIcon.SetActiveOptimized(true);
            _keyLabel.SetActiveOptimized(false);
            
            _holdFillBar.ShapeType = RewiredHelper.IsGamepad ? VisualOutlineFillBar.Shape.Circle : VisualOutlineFillBar.Shape.Square;
            _holdFillBar.SetActiveOptimized(_data.IsHold);
        }

        protected void OnIconNull() {
            _keyIcon.SetActiveOptimized(false);
        }

        protected void OnUnknownIconType() {
            OnIconNull();
            Log.Important?.Error("Unknown KeyIcon");
        }
        protected virtual void OnPostIconSetup() { }

        void RefreshIcon() {
            _currentScheme = ControlSchemes.Current();
            var currentIcon = _icons[_currentScheme];

            if (currentIcon is SpriteIcon spriteIcon) {
                SetupSpriteIcon(spriteIcon);
            } else if (currentIcon is TextIcon textIcon) {
                SetupTextIcon(textIcon);
            } else if (currentIcon == null) {
                OnIconNull();
            } else {
                OnUnknownIconType();
            }

            OnPostIconSetup();
        }
        
        void ChangeIcon(VisualElement target, SpriteReference sprite, ref SpriteReference loadedReference) {
            loadedReference?.Release();

            if (sprite != null) {
                loadedReference = sprite;
                loadedReference.SetSprite(target);
            } else {
                loadedReference = null;
                Log.Important?.Error("No icon set");
            }
        }

        void RefreshIcons() {
            _icons = _data.GetIcons();
            RefreshIcon();
        }
    }
}