using Awaken.TG.Graphics.FloatingTexts;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Tutorials.Views;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe;
using Awaken.TG.Main.UI.HUD.Notifications;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD {
    [SpawnsView(typeof(VFloatingTextHUD), false, order = 1)]
    [SpawnsView(typeof(VTutorialHUD), false, order = 2)]
    public partial class HUD : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public PNotificationsContainerUI NotificationsContainerUI { get; private set; }
        static UIDocumentProvider UIDocumentProvider => Services.Get<UIDocumentProvider>();

        VisualElement _utkHUDRoot;
        Transform _uguiHUDRoot;
        ShowUIHUD _showUIHUDSetting;

        // === Initialization
        protected override void OnFullyInitialized() {
            _utkHUDRoot = UIDocumentProvider.TryGetDocument(UIDocumentType.HUD).rootVisualElement.Q<VisualElement>("HUD");
            _uguiHUDRoot = Services.Get<ViewHosting>().OnHUD();

            NotificationsContainerUI = new PNotificationsContainerUI(_utkHUDRoot);
            World.BindPresenter(this, NotificationsContainerUI);

            _showUIHUDSetting = World.Only<ShowUIHUD>();
            ShowHUD(_showUIHUDSetting.HUDEnabled);

            _showUIHUDSetting.ListenTo(Setting.Events.SettingChanged, OnHudVisibilityChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<DecorMode>(), this, OnHudVisibilityChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<DecorMode>(), this, OnHudVisibilityChanged);
            
            InitializeNotificationBuffers();
        }
        
        void OnHudVisibilityChanged() {
            ShowHUD(_showUIHUDSetting.HUDEnabled && !World.HasAny<DecorMode>());
        }

        void InitializeNotificationBuffers() {
            AddElement(new NotificationBuffer());
            AddElement(new ProficiencyNotificationBuffer());
            AddElement(new SpecialItemNotificationBuffer());
            AddElement(new ItemNotificationBuffer());
            AddElement(new HeroLevelUpNotificationBuffer());
            AddElement(new MiddleScreenNotificationBuffer());
            AddElement(new LowerMiddleScreenNotificationBuffer());
            AddElement(new ExpNotificationBuffer());
            AddElement(new DialogueNotificationBuffer());
            AddElement(new RecipeNotificationBuffer());
            AddElement(new LocationDiscoveryBuffer());
            AddElement(new QuestNotificationBuffer());
            AddElement(new ObjectiveNotificationBuffer());
            AddElement(new JournalUnlockNotificationBuffer());
            AddElement(new WyrdInfoNotificationBuffer());
        }

        void ShowHUD(bool show) {
            _utkHUDRoot.SetActiveOptimized(show);
            _uguiHUDRoot.gameObject.SetActive(show);
        }
        
        #if UNITY_EDITOR
            public void EDITOR_DEBUG_ShowHUD(bool show) {
                _utkHUDRoot.SetActiveOptimized(show);
                _uguiHUDRoot.gameObject.SetActive(show);
            }
        #endif
    }
}