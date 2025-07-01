using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Menu.Artbook {
    [UsesPrefab("TitleScreen/" + nameof(VArtbookUI))]
    public class VArtbookUI : View<ArtbookUI>, IAutoFocusBase, IPromptHost, IUIAware {
        [SerializeField] Image mainImage;
        [SerializeField, UIAssetReference(AddressableGroup.UniqueTexturesArtbook)]
        SpriteReference[] artSpriteReferences = Array.Empty<SpriteReference>();
        [SerializeField] MenuUIButton previousButton;
        [SerializeField] MenuUIButton nextButton;
        [SerializeField] TextMeshProUGUI artOfText;
        [SerializeField] Image logoImage;
        [SerializeField] Transform promptHost;
        
        int _currentIndex;
        SpriteReference _currentSpriteReference;
        
        public Transform PromptsHost => promptHost;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        bool IsFirstArt => _currentIndex == 0;
        bool IsLastArt => _currentIndex == artSpriteReferences.Length - 1;

        protected override void OnFullyInitialized() {
            InitButtons();
            InitPrompts();
            LoadImage();
            artOfText.SetText(LocTerms.ArtOf.Translate());
        }
        
        protected override void OnMount() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
        }
        
        void InitButtons() {
            previousButton.button.OnClick += PreviousImage;
            nextButton.button.OnClick += NextImage;
        }

        void InitPrompts() {
            var prompts = Target.AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Generic.IncreaseValue, LocTerms.PreviousNextTrack.Translate(), controllers: ControlSchemeFlag.Gamepad), Target);
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Close), Target);
        }

        [Button]
        void NextImage() {
            _currentIndex = (_currentIndex + 1) % artSpriteReferences.Length;
            LoadImage();
        }
        
        [Button]
        void PreviousImage() {
            _currentIndex = (_currentIndex - 1 + artSpriteReferences.Length) % artSpriteReferences.Length;
            LoadImage();
        }

        void LoadImage() {
            _currentSpriteReference?.Release();
            _currentSpriteReference = artSpriteReferences[_currentIndex];
            _currentSpriteReference.SetSprite(mainImage);
            
            RefreshComponents();
        }
        
        void RefreshComponents() {
            logoImage.TrySetActiveOptimized(!IsFirstArt);
            artOfText.TrySetActiveOptimized(!IsFirstArt);
            previousButton.TrySetActiveOptimized(!IsFirstArt);
            nextButton.TrySetActiveOptimized(!IsLastArt);
        }
        
        public UIResult Handle(UIEvent evt) {
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValue && !IsLastArt:
                    NextImage();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValue && !IsFirstArt:
                    PreviousImage();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
        
        void Close() {
            Target.Discard();
        }

        protected override IBackgroundTask OnDiscard() {
            _currentSpriteReference?.Release();
            _currentSpriteReference = null;
            return base.OnDiscard();
        }
    }
}