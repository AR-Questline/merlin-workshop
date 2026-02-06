using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public partial class TalentTable : Element<HeroTalents> {
        public override ushort TypeForSerialization => SavedModels.TalentTable;

        [Saved] TalentTreeTemplate _template;
        public readonly List<Talent> talents = new();
        public int PointsSpent { get; set; }

        public TalentTreeTemplate TreeTemplate => _template;
        public int MaxTreeLevel => talents.Sum(talent => talent.MaxLevel);
        public int CurrentTreeLevel => talents.Sum(talent => talent.EstimatedLevel);
        public int MinTreeLevel => talents.Where(talent => talent.IsUpgraded).Sum(talent => talent.RequiredTreeLevelToUnlock);
        public Hero Hero => Talents.Hero;
        
        HeroTalents Talents => ParentModel;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] TalentTable() { }
        public TalentTable(TalentTreeTemplate template) {
            _template = template;
        }
        
        protected override void OnInitialize() {
            foreach (var subTree in TreeTemplate.TreeSubTrees) {
                foreach (var node in subTree.TreeNodes) {
                    talents.Add(AddElement(new Talent(node, subTree.SubtreeType, subTree.CurrencyStatType)));
                }
            }
        }
        
        protected override void OnRestore() {
            if (TreeTemplate == null) {
                Log.Important?.Error($"{LogUtils.GetDebugName(this)} has no talent tree template. Discarding TalentTable.");
                Discard();
                return;
            }
            
            var talentElements = Elements<Talent>().ToArraySlow();
            var existingTalentGuids = new HashSet<string>();
            
            for (int i = 0; i < talentElements.Length; i++) {
                if (talentElements[i].Template != null && talentElements[i].CheckTalentTree()) {
                    PointsSpent += talentElements[i].Level;
                    talents.Add(talentElements[i]);
                    existingTalentGuids.Add(talentElements[i].Template.GUID);
                } else {
                    if (talentElements[i].Template == null) {
                        Log.Important?.Error($"Talent at position ({i} - {talentElements[i].ID}) has no template assigned and is not present in its ParentTable ({TreeTemplate.GUID} - {TreeTemplate.Name}), marking it for a discard");
                    } else {
                        Log.Important?.Error($"Talent at position ({i} - {talentElements[i].ID}: {talentElements[i].Template.GUID} - {talentElements[i].Template.Name}) is not present in its ParentTable ({TreeTemplate.GUID} - {TreeTemplate.Name}), marking it for a discard");
                    }
                    talentElements[i].MarkForDiscard();
                }
            }
            
            foreach (var subTree in TreeTemplate.TreeSubTrees) {
                foreach (var node in subTree.TreeNodes) {
                    if (!existingTalentGuids.Contains(node.Talent.GUID)) {
                        talents.Add(AddElement(new Talent(node, subTree.SubtreeType, subTree.CurrencyStatType)));
                    } 
                }
            }
        }

        public void ApplyTemporaryLevels() {
            foreach (var talent in Elements<Talent>()) {
                talent.ApplyTemporaryLevels();
            }
            Hero.RestoreStats();
        }
        
        public void ClearTemporaryPoints() {
            foreach (var talent in Elements<Talent>()) {
                talent.ClearTemporaryPoints();
            }
        }
        
        public void Reset(bool withRefund = true) {
            foreach (var talent in Elements<Talent>()) {
                talent.Reset(withRefund);
            }
        }
    }
}