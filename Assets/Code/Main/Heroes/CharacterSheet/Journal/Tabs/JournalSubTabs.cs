using Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    public partial class JournalSubTabs : Tabs<JournalUI, VJournalTabs, JournalSubTabType, IJournalCategoryTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;

        protected override void ChangeTab(JournalSubTabType type) {
            base.ChangeTab(type);
            ParentModel.SetCountActive(type != JournalSubTabType.Recipes);
        }
    }
    
    public interface IJournalCategoryTab : JournalSubTabs.ITab { }
    public abstract partial class JournalCategoryTab<TTabView> : JournalSubTabs.Tab<TTabView>, IJournalCategoryTab where TTabView : View { }
    
    public class JournalSubTabType : JournalSubTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly JournalSubTabType
            Bestiary = new(nameof(Bestiary), LocTerms.JournalTabBestiary, _ => new JournalBestiary(), Always),
            Characters = new(nameof(Characters), LocTerms.JournalTabCharacters, _ => new JournalCharacters(), Always),
            Lore = new(nameof(Lore), LocTerms.JournalTabLore, _ => new JournalLore(), Always),
            Recipes = new(nameof(Recipes), LocTerms.Crafting, _ => new JournalRecipeUI(), Always),
            Tutorials = new(nameof(Tutorials), LocTerms.JournalTabTutorials, _ => new JournalTutorials(), Always),
            Fish = new(nameof(Fish), LocTerms.Fishing, _ => new JournalFish(), Always);

        protected JournalSubTabType(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible) : base(enumName, title, spawn, visible) { }
    }
}
