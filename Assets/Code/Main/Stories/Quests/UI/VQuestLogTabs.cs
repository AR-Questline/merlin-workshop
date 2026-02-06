using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/" + nameof(VQuestLogTabs))]
    public class VQuestLogTabs : View<QuestLogSubTabs> { }
}
