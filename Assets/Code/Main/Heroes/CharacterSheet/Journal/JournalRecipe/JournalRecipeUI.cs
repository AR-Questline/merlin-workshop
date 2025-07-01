using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe {
    public partial class JournalRecipeUI : JournalCategoryUI<JournalEntryData, BaseRecipe, VJournalRecipeUI>, JournalRecipeSubTabs.ISubTabParent<VJournalRecipeUI> {
        public JournalRecipeSubTabType CurrentType { get; set; } = JournalRecipeSubTabType.None;
        public JournalRecipeSubTabs.ISubTabParent<VJournalRecipeUI> SubTabParent => this;
        public bool ForceInvisibleTab => true;
        public Tabs<JournalRecipeUI, VJournalRecipeTabs, JournalRecipeSubTabType, IJournalRecipeSubTab> TabsController { get; set; }

        readonly StringBuilder _stringBuilder = new();
        JournalRecipeSubTabType _recipeType;
        VCJournalRecipeCategoryButton[] _subTabButtons;
        VJournalRecipeUI VJournalRecipeUI => View<VJournalRecipeUI>();
        
        public new static class Events {
            public static readonly Event<IJournalCategoryUI, IJournalCategoryUI> CategoryChanged = new(nameof(CategoryChanged));
        }

        public async UniTaskVoid Focus() {
            SubTabParent.TabsController.BlockNavigation = true;
            
            if (await AsyncUtil.DelayFrame(this)) {
                _subTabButtons.FirstOrAny(b => b.gameObject.activeSelf).Focus();
            }
        }
        
        public void HideTabs() {
            SubTabParent.TabsController.BlockNavigation = true;
            VJournalRecipeUI.HideTabs();
            Focus().Forget();
        }
        
        public void ShowTabs() {
            SubTabParent.TabsController.BlockNavigation = false;
            VJournalRecipeUI.ShowTabs();
        }
        
        protected override void AfterViewSpawned(VJournalRecipeUI view) {
            _subTabButtons ??= View.RecipesSubTabParent.GetComponentsInChildren<VCJournalRecipeCategoryButton>();
            
            foreach (var button in _subTabButtons) {
                button.Attach(World.Services, this, View);
            }
            
            AddElement(new JournalRecipeSubTabs());
        }
        
        public void SetRecipeType(JournalRecipeSubTabType recipeType) {
            _recipeType = recipeType;
            SubTabParent.TabsController.SelectTab(recipeType);
        }

        public void ShowEntries(JournalRecipeSubTabType recipeType) {
            _recipeType = recipeType;
            ClearEntries();
            PopulateEntries();
        }

        protected override IEnumerable<BaseRecipe> GatherAllEntries() {
            return _recipeType.GatherRecipes();
        }
        
        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            var categories = allEntries
                .GroupBy(x => ItemUtils.ItemTypeTranslation(null, x.Outcome));

            foreach (var category in categories) {
                yield return (category.Key, GatherKnownEntries(category));
            }
        }

        IEnumerable<JournalEntryData> GatherKnownEntries(IEnumerable<BaseRecipe> knownEntries) {
            return knownEntries.Select(data => new JournalEntryData(data.Outcome.itemName.Translate(), CreateDescription(data), Array.Empty<string>(), data.Outcome.iconReference));
        }
        
        string CreateDescription(BaseRecipe recipe) {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine(LocTerms.Ingredients.Translate());
            recipe.Ingredients.ForEach(x => _stringBuilder.AppendLine(x.Template.itemName.Translate()));
            return _stringBuilder.ToString();
        }
    }
}
