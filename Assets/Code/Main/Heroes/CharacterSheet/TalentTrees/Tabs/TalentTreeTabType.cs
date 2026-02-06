using System;
using System.Linq;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    [Serializable]
    public class TalentTreeTabType : TreeTabTypeBase<VTalentOverviewUI, VTalentTreeTabs> {
        [SerializeField, TemplateType(typeof(TalentTreeTemplate))] TemplateReference talentTree;
        
        public override TalentTreeTemplate Tree => talentTree.Get<TalentTreeTemplate>();
        
        static bool WyrdTalentsUnlocked() => Hero.Current.Development.WyrdSoulFragments.UnlockedFragments.Contains(WyrdSoulFragmentType.Excalibur);
        public override TalentTreeBase<VTalentOverviewUI, VTalentTreeTabs> Spawn(TalentOverviewBase<VTalentOverviewUI, VTalentTreeTabs> target) => new TalentTree(Tree);
        public override bool IsVisible(TalentOverviewBase<VTalentOverviewUI, VTalentTreeTabs> target) => Tree.CurrencyStatType != HeroStatType.WyrdMemoryShards || (Tree.CurrencyStatType == HeroStatType.WyrdMemoryShards && WyrdTalentsUnlocked());
    }
}