using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe {
    public class VCJournalRecipeTabButton : JournalRecipeSubTabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(JournalRecipeSubTabType))] 
        [SerializeField] RichEnumReference tabType;
        public override JournalRecipeSubTabType Type => tabType.EnumAs<JournalRecipeSubTabType>();
        public override string ButtonName => Type.Title;
    }
}