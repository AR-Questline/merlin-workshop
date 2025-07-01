using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalRecipeTabs))]
    public class VJournalRecipeTabs : View<JournalRecipeSubTabs> { }
}
