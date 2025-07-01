using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    [UsesPrefab("CharacterSheet/TalentTree/" + nameof(VTalentTreeTabs))]
    public class VTalentTreeTabs : View<TalentTreeTabs> { }
}