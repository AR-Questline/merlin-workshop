using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [SpawnsView(typeof(VProficiencyEntryUI))]
    public partial class ProficiencyEntryUI : Element<ProficiencyCategoryUI> {
        public readonly ProfStatType proficiencyStat;
        public readonly ShareableSpriteReference proficiencyIcon;
        
        public ProficiencyEntryUI(ProfStatType proficiencyStat) {
            this.proficiencyStat = proficiencyStat;
            this.proficiencyIcon = proficiencyStat.GetIcon?.Invoke();
        }

        protected override void OnInitialize() {
            var description = proficiencyStat.Description;
            var entryInfo = new EntryInfoUI(description, typeof(VProficiencyEntryInfo));
            AddElement(entryInfo);
        }
    }
}