using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public partial class HeroTalents : Element<Hero>, ISkillOwner {
        public override ushort TypeForSerialization => SavedModels.HeroTalents;

        public Hero Hero => ParentModel;
        ICharacter ISkillOwner.Character => Hero;

        public IEnumerable<TalentTreeTemplate> AllTalentTree => Services.Get<TemplatesProvider>().GetAllOfType<TalentTreeTemplate>();

        protected override void OnInitialize() {
            foreach (TalentTreeTemplate treeTemplate in AllTalentTree) {
                if (TableOf(treeTemplate) == null) {
                    AddElement(new TalentTable(treeTemplate));
                }
            }
        }
        
        public TalentTable TableOf(TalentTreeTemplate treeTemplate) {
            return treeTemplate != null ? Elements<TalentTable>().FirstOrDefault(table => table.TreeTemplate == treeTemplate) : null;
        }

        public Talent TalentOf(TalentTemplate treeTemplate, StatType currency) {
            if (treeTemplate == null) {
                return null;
            }

            foreach (var table in Elements<TalentTable>()) {
                if (table.TreeTemplate.CurrencyStatType != currency) {
                    continue;
                }
                foreach (var talent in table.Elements<Talent>()) {
                    if (talent.Template == treeTemplate) {
                        return talent;
                    }
                }
            }
            return null;
        }

        public bool AnyUnappliedTalentPoints() {
            return Elements<TalentTable>().Any(table => table.talents.Any(t => t.WasChanged));
        }
        
        [UnityEngine.Scripting.Preserve]
        public void AcquireNextTemporaryLevel(TalentTemplate talentTemplate, StatType currency) {
            TalentOf(talentTemplate, currency).AcquireNextTemporaryLevel();
        }
        
        public void ApplyTemporaryLevels() {
            foreach (var table in Elements<TalentTable>()) {
                table.ApplyTemporaryLevels();
            }
        }

        public void ClearTemporaryPoints() {
            foreach (var table in Elements<TalentTable>()) {
                table.ClearTemporaryPoints();
            }
        }
        
        public void Reset() {
            foreach (var table in Elements<TalentTable>()) {
                table.Reset();
            }
        }
    }
}