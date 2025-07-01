using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Templates;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public class VWyrdTalentTreePattern : VTalentTreePatternBase {
        [SerializeField] List<WyrdTalentSubTree> subTrees;
        
        public List<WyrdTalentSubTree> WyrdTalentTree => subTrees;
        protected override List<TalentSubTreeBase> GetSubTrees() => subTrees.Cast<TalentSubTreeBase>().ToList();
    }
    
    [Serializable]
    public class WyrdTalentSubTree : TalentSubTreeBase {
        [Title("Soul Fragment")]
        [SerializeField] VCWyrdTalentType wyrdTalentType;
        [Title("Main Skill")]
        [SerializeField, TemplateType(typeof(TalentTemplate))] TemplateReference wyrdTalentTemplate;
        [SerializeField] Transform wyrdMainTalentSlot;
        [SerializeField] GameObject enableSection;
        [SerializeField] GameObject disableSection;
        [Title("Tooltip")]
        [SerializeField] TooltipPosition tooltipPositionLeft;
        [SerializeField] TooltipPosition tooltipPositionRight;
        
        public Transform WyrdMainTalentSlot => wyrdMainTalentSlot;
        public VCWyrdTalentType WyrdTalentType => wyrdTalentType;
        public TalentTemplate WyrdMainTalent => wyrdTalentTemplate.Get<TalentTemplate>();
        public TooltipPosition TooltipPositionLeft => tooltipPositionLeft;
        public TooltipPosition TooltipPositionRight => tooltipPositionRight;
        
        public override void SetSectionState(bool enabled) {
            enableSection.SetActiveOptimized(enabled);
            disableSection.SetActiveOptimized(!enabled);
            base.SetSectionState(enabled);
        }
    }
}
