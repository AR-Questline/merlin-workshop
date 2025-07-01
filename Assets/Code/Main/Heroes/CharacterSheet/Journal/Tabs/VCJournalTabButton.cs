using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    public class VCJournalTabButton : JournalSubTabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(JournalSubTabType))] 
        [SerializeField] RichEnumReference tabType;
        public override JournalSubTabType Type => tabType.EnumAs<JournalSubTabType>();
        public override string ButtonName => Type.Title;
    }
}