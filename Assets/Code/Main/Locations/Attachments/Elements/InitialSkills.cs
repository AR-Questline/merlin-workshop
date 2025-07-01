using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class InitialSkills : Element<Location>, IRefreshedByAttachment<InitialSkillsAttachment> {
        public override ushort TypeForSerialization => SavedModels.InitialSkills;

        // === Fields & Properties
        InitialSkillsAttachment _spec;

        IEnumerable<Skill> Skills => _spec.Skills.Select(s => s.CreateSkill());

        public void InitFromAttachment(InitialSkillsAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(InitSkills);
        }

        void InitSkills() {
            var characterSkills = GetCharacterSkills();
            if (characterSkills == null) {
                return;
            }
            foreach (var skill in Skills) {
                characterSkills.LearnSkill(skill).MarkedNotSaved = true;
            }
        }

        ICharacterSkills GetCharacterSkills() {
            if (ParentModel.TryGetElement<NpcElement>(out var npc)) {
                return npc.Skills;
            } else if (ParentModel.TryGetElement<Hero>(out var hero)) {
                return hero.Skills;
            } else {
                return null;
            }
        }
    }
}
