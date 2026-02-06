using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/" + nameof(VQuestLogRootUI))]
    public class VQuestLogRootUI : VTabParent<QuestLogRootUI>, IAutoFocusBase {
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            TabButtonsHost.TrySetActiveOptimized(Target.HasMoreSubtabs);
        }
    }
}