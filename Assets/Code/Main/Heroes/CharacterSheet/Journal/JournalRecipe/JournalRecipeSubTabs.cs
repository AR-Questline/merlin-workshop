using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.AlchemyCrafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe {
    public partial class JournalRecipeSubTabs : Tabs<JournalRecipeUI, VJournalRecipeTabs, JournalRecipeSubTabType, IJournalRecipeSubTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next =>  KeyBindings.UI.Generic.NextAlt;
        
        public void SetNone() {
            ParentModel.ParentModel.SetCountActive(false);
            ParentModel.HideTabs();
            base.ChangeTab(JournalRecipeSubTabType.None);
            ParentModel.Focus().Forget();
        }
        
        protected override void ChangeTab(JournalRecipeSubTabType type) {
            base.ChangeTab(type);
            ParentModel.Trigger(JournalRecipeUI.Events.CategoryChanged, ParentModel);
            ParentModel.ParentModel.SetCountActive(type != JournalRecipeSubTabType.None);
            ParentModel.ShowTabs();
            ParentModel.ShowEntries(type);
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.Focus().Forget();
        }
    }
    
    public interface IJournalRecipeSubTab : JournalRecipeSubTabs.ITab { }
    public partial class NoneJournalRecipeSubTab : JournalRecipeSubTabs.TabWithoutView, IJournalRecipeSubTab { }
    public partial class EmptyJournalRecipeSubTab : JournalRecipeSubTabs.EmptyTabWithBackBehaviour, IJournalRecipeSubTab {
        public override void Back() {
            ParentModel.Element<JournalRecipeSubTabs>().SetNone();
        }
    }
    
    public class JournalRecipeSubTabType : JournalRecipeSubTabs.DelegatedTabTypeEnum {
        public Func<IEnumerable<BaseRecipe>> GatherRecipes { get; }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly JournalRecipeSubTabType
            None = new(nameof(None), string.Empty, _ => new NoneJournalRecipeSubTab(), Always, Enumerable.Empty<BaseRecipe>),
            JournalAlchemy = new(nameof(JournalAlchemy), LocTerms.Alchemy, _ => new EmptyJournalRecipeSubTab(), Always, () => Hero.Current.Element<HeroRecipes>().knownRecipes.OfType<AlchemyRecipe>()),
            JournalCooking = new(nameof(JournalCooking), LocTerms.Cooking, _ => new EmptyJournalRecipeSubTab(), Always, () => Hero.Current.Element<HeroRecipes>().knownRecipes.OfType<CookingRecipe>()),
            JournalHandcrafting = new(nameof(JournalHandcrafting), LocTerms.Handcrafting, _ => new EmptyJournalRecipeSubTab(), Always, () => Hero.Current.Element<HeroRecipes>().knownRecipes.OfType<HandcraftingRecipe>());

        protected JournalRecipeSubTabType(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible, Func<IEnumerable<BaseRecipe>> gatherRecipes) : base(enumName, title, spawn, visible) {
            GatherRecipes = gatherRecipes;
        }
    }
}
