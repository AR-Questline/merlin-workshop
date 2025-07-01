using System;
using System.Linq;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    [Serializable]
    public class TalentTreeTabType : TalentTreeTabs.ITabType {
        [SerializeField, TemplateType(typeof(TalentTreeTemplate))] TemplateReference talentTree;
        
        public TalentTreeTemplate Tree => talentTree.Get<TalentTreeTemplate>();
        
        public TalentTree Spawn(TalentOverviewUI target) => new(Tree);
        public bool IsVisible(TalentOverviewUI target) => Tree.CurrencyStatType != HeroStatType.WyrdMemoryShards || (Tree.CurrencyStatType == HeroStatType.WyrdMemoryShards && WyrdTalentsUnlocked());
        static bool WyrdTalentsUnlocked() => Hero.Current.Development.WyrdSoulFragments.UnlockedFragments.Contains(WyrdSoulFragmentType.Excalibur);
    }
}