using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Stories.Quests.UI {
    public partial class QuestLogRootUI : CharacterSheetTab<VQuestLogRootUI>, QuestLogSubTabs.ITabParent<VQuestLogRootUI>, ICharacterSheetTabWithSubTabs {
        static SceneService SceneService => Services.Get<SceneService>();
        static CommonReferences CommonReferences => CommonReferences.Get;
        
        public QuestLogSubTabType CurrentType { get; set; } = QuestLogSubTabType.Default;
        public Tabs<QuestLogRootUI, VQuestLogTabs, QuestLogSubTabType, IQuestLogSubTab> TabsController { get; set; }
        public bool HasSarrasDlc { get; private set; }
        public bool ShowSarrasTab => HasSarrasDlc && HaveAnySarrasQuest();
        public bool HasMoreSubtabs => ShowSarrasTab;
        readonly QuestLogSubTabType _currentlyDefaultTabType = GetDefaultTab();

        protected override void OnInitialize() {
            base.OnInitialize();
            HasSarrasDlc = SocialService.Get.HasDlc(DlcCategory.Sarras);
        }

        protected override void AfterViewSpawned(VQuestLogRootUI view) {
            AddElement(new QuestLogSubTabs());
            TabsController.SelectTab(_currentlyDefaultTabType);
        }
        
        public bool TryToggleSubTab(CharacterSheetUI ui) {
            ui.Element<QuestLogRootUI>().TabsController.SelectTab(_currentlyDefaultTabType);
            return true;
        }

        static QuestLogSubTabType GetDefaultTab() {
            if (IsSarrasVisited() && HaveAnySarrasQuest()) {
                return QuestLogSubTabType.Sarras;
            }
            return QuestLogSubTabType.Default;
        }

        static bool IsSarrasVisited() {
            return LastOpenWorldUtils.WasLastOne(LastOpenWorldUtils.Worlds.Sarras) ||
                SceneService.ActiveSceneRef == CommonReferences.SarrasFirstSceneReference;
        }

        static bool HaveAnySarrasQuest() {
            return World.Any<Quest>(q => q.Category is QuestCategory.Sarras && q.State is not QuestState.NotTaken);
        }
    }
}
