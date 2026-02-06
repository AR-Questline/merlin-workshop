using System;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    [Serializable]
    public class SarrasTalentTreeTabType : TreeTabTypeBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> {
        [SerializeField, TemplateType(typeof(TalentTreeTemplate))] TemplateReference talentTree;
        
        public override TalentTreeTemplate Tree => talentTree.Get<TalentTreeTemplate>();
        
        public override TalentTreeBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> Spawn(TalentOverviewBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> target) => new SarrasTalentTree(Tree);
        public override bool IsVisible(TalentOverviewBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> target) => false;
    }
}