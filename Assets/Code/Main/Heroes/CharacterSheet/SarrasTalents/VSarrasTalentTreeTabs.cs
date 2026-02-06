using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    [UsesPrefab("CharacterSheet/SarrasTalents/" + nameof(VSarrasTalentTreeTabs))]
    public class VSarrasTalentTreeTabs : View<SarrasTalentTreeTabs> { }
}