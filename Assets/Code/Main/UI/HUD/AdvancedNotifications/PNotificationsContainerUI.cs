using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using EnhydraGames.BetterTextOutline;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public class PNotificationsContainerUI : QueryPresenter<HUD>, IPresenterWithAccessibilityBackground {
        VisualElement IPresenterWithAccessibilityBackground.Host => RecipeNotificationsParent;
        
        public VisualElement ExpNotificationsParent { get; private set; }
        public VisualElement ItemNotificationsParent { get; private set; }
        public VisualElement LocationNotificationsParent { get; private set; }
        public VisualElement SpecialItemNotificationsParent { get; private set; }
        public VisualElement ProficiencyNotificationsParent { get; private set; }
        public VisualElement LevelUpNotificationsParent { get; private set; }
        public VisualElement QuestNotificationsParent { get; private set; }
        public VisualElement ObjectiveNotificationsParent { get; private set; }
        public VisualElement RecipeNotificationsParent { get; private set; }
        public VisualElement JournalUnlockNotificationParent { get; private set; }
        public VisualElement WyrdInfoNotificationParent { get; private set; }
        
        public PNotificationsContainerUI(VisualElement parent) : base(parent) { }

        protected override void CacheVisualElements(VisualElement contentRoot) {
            ExpNotificationsParent = contentRoot.Q<VisualElement>("exp-notifications-parent");
            ItemNotificationsParent = contentRoot.Q<VisualElement>("item-notifications-parent");
            LocationNotificationsParent = contentRoot.Q<VisualElement>("location-notifications-parent");
            SpecialItemNotificationsParent = contentRoot.Q<VisualElement>("special-item-notifications-parent");
            ProficiencyNotificationsParent = contentRoot.Q<VisualElement>("proficiency-notifications-parent");
            LevelUpNotificationsParent = contentRoot.Q<VisualElement>("level-up-notifications-parent");
            QuestNotificationsParent = contentRoot.Q<VisualElement>("quest-notifications-parent");
            ObjectiveNotificationsParent = contentRoot.Q<VisualElement>("objective-notifications-parent");
            JournalUnlockNotificationParent = contentRoot.Q<VisualElement>("journal-unlock-notification-parent");
            WyrdInfoNotificationParent = contentRoot.Q<VisualElement>("wyrd-info-notification-parent");
            CacheRecipeNotificationsParent(contentRoot);
        }
        
        protected override void OnFullyInitialized() {
            Content.SetActiveOptimized(true);
            IPresenterWithAccessibilityBackground thisAsBackground = this;
            thisAsBackground.InitializeBackground(TargetModel);
        }

        protected override void ClearContent() {
            Content.SetActiveOptimized(false);
        }
        
        void CacheRecipeNotificationsParent(VisualElement contentRoot) {
            RecipeNotificationsParent = contentRoot.Q<VisualElement>("recipe-notifications-parent");
            RecipeNotificationsParent.Q<BetterOutlinedLabel>("recipe-buffer-title").text = LocTerms.NewRecipeUnlocked.Translate().ToUpper();
            RecipeNotificationsParent.Hide();
        }
    }
}