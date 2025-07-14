using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
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
    [UsesPrefab("UI/SaveLoad/VSaveLoadSlotUI")]
    public class VSaveLoadSlotUI : RetargetableView<SaveLoadSlotUI>, ISemaphoreObserver, IVSaveLoadSlotUI {
        [SerializeField] ButtonConfig slotButton;
        [SerializeField] Image gameplayScreenshot;
        [SerializeField] TextMeshProUGUI questNameText;
        [SerializeField] TextMeshProUGUI saveNameText;
        [SerializeField] TextMeshProUGUI gameTimeText;
        [SerializeField] TextMeshProUGUI playerInfoText;
        [SerializeField] TextMeshProUGUI realDateTimeText;
        
        CoyoteSemaphore _isHovered;
        Texture2D _texture;
        Sprite _sprite;
        
        public ARButton SlotButton => slotButton.button;
        public override Transform DetermineHost() => Target.ParentModel.SlotsParent;
        
        SaveSlot SaveSlot => Target.saveSlot;

        protected override void OnInitialize() {
            _isHovered = new CoyoteSemaphore(this);
            slotButton.InitializeButton();
            slotButton.button.OnEvent += Handle;
            slotButton.button.OnPress += OnPressed;
            slotButton.button.disableAllSounds = SaveSlot.IsAutoSave || SaveSlot.IsQuickSave;
            RefreshSlotData();
            Target.ListenTo(Model.Events.AfterChanged, RefreshSlotData, this);
        }

        protected override IBackgroundTask OnDiscard() {
            ReleaseResources();
            return base.OnDiscard();
        }

        void Update() {
            _isHovered.Update();
        }

        void RefreshSlotData() {
            string heroName = SaveSlot.Hardcore ? "<sprite name=\"t\" color=#ff0000> " + SaveSlot.HeroName : SaveSlot.HeroName;

            ReleaseResources();
            _texture = Target.saveSlot.RecreateGameplayScreenshot();
            _sprite = _texture?.ToSprite();
            
            gameplayScreenshot.sprite = _sprite;
            
            questNameText.text = $"{SaveSlot.ActiveQuestName}";
            string displayName = SaveSlot.IsQuickSave || SaveSlot.IsAutoSave ? SaveSlot.DisplayName.ToString().Bold() : SaveSlot.DisplayName;
            saveNameText.text = $"{displayName}";
            playerInfoText.text = $"{LocTerms.LevelWithNumber.Translate(SaveSlot.HeroLevel)}     <b>{heroName}</b>     {SaveSlot.HeroLocation.ToString().Replace("_", " ")}";
            realDateTimeText.text = $"{SaveSlot.LastSavedTime:G}";

            var playRealTime = SaveSlot.PlayRealTime;
            gameTimeText.text = $"{playRealTime.Hours:00}:{playRealTime.Minutes:00}:{playRealTime.Seconds:00}";
        }

        void OnPressed() {
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
        
        void ReleaseResources() {
            if (_texture) {
                Destroy(_sprite);
                Destroy(_texture);
            }
        }
    }
}