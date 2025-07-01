using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [SpawnsView(typeof(VProficienciesUI))]
    public partial class ProficienciesUI : Element<CharacterInfoUI> {
        protected override void OnFullyInitialized() {
            var proficienciesByCategory = ProfStatType.HeroProficiencies.GroupBy(profStatType => profStatType.Category);
            foreach (var group in proficienciesByCategory) {
                string categoryName = group.Key.DisplayName;
                ShareableSpriteReference categoryIcon = group.Key.Icon?.Invoke();
                List<ProfStatType> proficiencies = group.ToList();
                AddElement(new ProficiencyCategoryUI(categoryName, categoryIcon, proficiencies));
            }
        }
    }
}