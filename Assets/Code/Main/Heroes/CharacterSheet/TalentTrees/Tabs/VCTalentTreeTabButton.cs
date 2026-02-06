using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public class VCTalentTreeTabButton : VCTabButtonBase<VTalentOverviewUI, VTalentTreeTabs> {
        [Space(10f)] [SerializeField, InlineProperty, HideLabel]
        TalentTreeTabType type;
        public override TreeTabTypeBase<VTalentOverviewUI, VTalentTreeTabs> Type => type;
    }
}