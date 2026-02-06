using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    public class VCSarrasTalentTreeTabButton : VCTabButtonBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> {
        [Space(10f)] [SerializeField, InlineProperty, HideLabel]
        SarrasTalentTreeTabType type;
        public override TreeTabTypeBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> Type => type;
    }
}