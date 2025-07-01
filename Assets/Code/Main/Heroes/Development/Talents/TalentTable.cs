using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
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
            if (TreeTemplate.Pattern == null) {
                Log.Important?.Error($"{TreeTemplate.name} has no talent tree pattern. Please check the template.");
                return;
            }
            
            foreach (TalentTreeNode node in TreeTemplate.Pattern.TalentNodes) {
                talents.Add(AddElement(new Talent (node.Talent, node.Parent)));
            }
        }
        protected override void OnRestore() {
            foreach (var talent in Elements<Talent>()) {
                PointsSpent += talent.Level;
                talent.CheckTalentTree();
                talents.Add(talent);
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
        
        public void Reset() {
            foreach (var talent in Elements<Talent>()) {
                talent.Reset();
            }
        }
    }
}