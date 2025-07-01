using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalTabs))]
    public class VJournalTabs : View<JournalSubTabs> { }
}
