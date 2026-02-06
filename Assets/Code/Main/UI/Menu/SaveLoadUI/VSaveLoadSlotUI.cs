using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Graphics;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [UsesPrefab("UI/SaveLoad/" + nameof(VSaveLoadSlotUI))]
    public class VSaveLoadSlotUI : RetargetableView<SaveLoadSlotUI>, ISemaphoreObserver, IVSaveLoadSlotUI {
        const float DisabledAllAlpha = 0.15f;
        const float DisabledBackgroundAlpha = 0.6f;
        const float DisabledIconAlpha = 0.3f;
        const float DisabledLineAlpha = 0.05f;
        static readonly int Grayscale = Shader.PropertyToID("_Grayscale");

        [SerializeField] ButtonConfig slotButton;
        [SerializeField] Image gameplayScreenshot;
        [SerializeField] TextMeshProUGUI questNameText;
        [SerializeField] TextMeshProUGUI saveNameText;
        [SerializeField] TextMeshProUGUI gameTimeText;
        [SerializeField] TextMeshProUGUI playerInfoText;
        [SerializeField] TextMeshProUGUI realDateTimeText;
        [SerializeField] CanvasGroup allGroup;
        [SerializeField] CanvasGroup backgroundGroup;
        [SerializeField] CanvasGroup iconGroup;
        [SerializeField] CanvasGroup lineGroup;
        [SerializeField] TextMeshProUGUI invalidDlcText;

        CoyoteSemaphore _isHovered;
        Texture2D _texture;
        Sprite _sprite;
        Material _iconMaterial;

        public ARButton SlotButton => slotButton.button;
        public override Transform DetermineHost() => Target.ParentModel.SlotsParent;
        
        SaveSlot SaveSlot => Target.saveSlot;
        bool _hasValidDLCs;

        protected override void OnFirstInit() {
            _iconMaterial = new Material(gameplayScreenshot.material);
            gameplayScreenshot.material = _iconMaterial;
            slotButton.InitializeButton();
        }
        
        protected override void OnOldTargetRemove() {
            SlotButton.OnEvent -= Handle;
            SlotButton.OnPress -= OnPressed;
        }

        protected override void OnNewTarget() {
            _hasValidDLCs = Target.saveSlot.HasValidDLCs();
            _isHovered = new CoyoteSemaphore(this);
            SlotButton.OnEvent += Handle;
            SlotButton.OnPress += OnPressed; 
            SlotButton.disableAllSounds = SaveSlot.IsAutoSave || SaveSlot.IsQuickSave;

            RefreshSlotData();
            Target.ListenTo(Model.Events.AfterChanged, RefreshSlotData, this);
        } 
        
        void Update() {
            _isHovered.Update();
        }

        void RefreshSlotData() {
            invalidDlcText.SetActiveAndText(!_hasValidDLCs, LocTerms.SlotNameDlcInvalid.Translate());
            allGroup.alpha = _hasValidDLCs ? 1f : DisabledAllAlpha;
            iconGroup.alpha = _hasValidDLCs ? 1f : DisabledIconAlpha;
            backgroundGroup.alpha = _hasValidDLCs ? 1f : DisabledBackgroundAlpha;
            lineGroup.alpha = _hasValidDLCs ? 1f : DisabledLineAlpha;
            
            string heroName = SaveSlot.Hardcore ? "<sprite name=\"t\" color=#ff0000> " + SaveSlot.HeroName : SaveSlot.HeroName;

            gameplayScreenshot.enabled = false;
            ReleaseResources();
            _texture = Target.saveSlot.RecreateGameplayScreenshot();
            _sprite = _texture?.ToSprite();
            
            var grayscale = _hasValidDLCs ? 0 : 1;
            _iconMaterial.SetFloat(Grayscale, grayscale);
            gameplayScreenshot.sprite = _sprite;
            gameplayScreenshot.enabled = true;

            questNameText.text = $"{SaveSlot.ActiveQuestName}";
            string displayName = SaveSlot.IsQuickSave || SaveSlot.IsAutoSave ? SaveSlot.DisplayName.ToString().Bold() : SaveSlot.DisplayName;
            saveNameText.text = $"{displayName}";
            playerInfoText.text = $"{LocTerms.LevelWithNumber.Translate(SaveSlot.HeroLevel)}     <b>{heroName} {NewGamePlusLevel()}</b>     {SaveSlot.HeroLocation.ToString().Replace("_", " ")}";
            realDateTimeText.text = $"{SaveSlot.LastSavedTime:G}";

            var playRealTime = SaveSlot.PlayRealTime;
            gameTimeText.text = $"{playRealTime.Hours:00}:{playRealTime.Minutes:00}:{playRealTime.Seconds:00}";
        }

        string NewGamePlusLevel() {
            var newGamePlusLevel = SaveSlot.NewGamePlusLevel;
            if (newGamePlusLevel <= 0) {
                return SaveSlot.AllowNewGamePlus ? LocTerms.NewGamePlusAvailable.Translate() : string.Empty;
            }
            
            string info = NewGamePlusUtils.NewGamePlusLevel(newGamePlusLevel);
            return SaveSlot.AllowNewGamePlus ? $"{info} {LocTerms.NewGamePlusAvailable.Translate()}" : info;
        }

        void OnPressed() {
            if (!_hasValidDLCs) {
                return;
            }
            
            TitleScreenUI titleScreenUI = World.Any<TitleScreenUI>();
            if (titleScreenUI != null) {
                titleScreenUI.PauseMusic();
                FMODManager.PlayOneShotAfter(CommonReferences.Get.AudioConfig.StartGameSound, slotButton.button.clickSound, this).Forget();
            }

            Target?.ParentModel.SaveLoadAction(Target);
        }

        UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _isHovered.Notify();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        void Hover() => World.Only<Selection>().Select(Target);
        void Unhover() => World.Only<Selection>().Deselect(Target);
        
        void ISemaphoreObserver.OnUp() => Hover();
        void ISemaphoreObserver.OnDown() => Unhover();
        
        protected override IBackgroundTask OnDiscard() {
            gameplayScreenshot.sprite = null;

            if (_iconMaterial) {
                Destroy(_iconMaterial);
                _iconMaterial = null;
            }
            
            ReleaseResources();
            return base.OnDiscard();
        }
        
        void ReleaseResources() {
            if (_texture) {
                Destroy(_sprite);
                Destroy(_texture);
            }
        }
    }
}