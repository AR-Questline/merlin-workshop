using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Skills {
    public sealed partial class CharacterSkills : Element<ICharacter>, ISkillOwner, ICharacterSkills {
        public override ushort TypeForSerialization => SavedModels.CharacterSkills;

        // === Properties
        public ICharacter Character => ParentModel;

        // === Queries
        public IEnumerable<Skill> AllSkills() {
            var skillElements = Elements<Skill>();
            foreach (Skill skill in skillElements) {
                yield return skill;
            }

            foreach (var item in ParentModel.Inventory.Items) {
                foreach (var activeSkill in item.ActiveSkills) {
                    yield return activeSkill;
                }
            }
        } 

        // === Initialization
        protected override void OnInitialize() {
            this.ListenTo(Events.AfterFullyInitialized, UpdateContext, this);
        }

        // === Changing skills
        public Skill LearnSkill(SkillGraph graph) {
            return LearnSkill(new Skill(graph));
        }

        public Skill LearnSkill(Skill skill) {
            AddElement(skill);
            skill.Learn();
            this.Trigger(Skill.Events.SkillLearned, skill);
            return skill;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void ForgetSkill(Skill skill) {
            this.Trigger(Skill.Events.SkillForgot, skill);
            skill.Forget();
            skill.Discard();
        }

        // === Refreshing the skill context
        public void UpdateContext() {
            TriggerChange();
            RefreshSkillState();
        }

        public void RefreshSkillState() {
            foreach (Skill s in AllSkills().ToList()) {
                if (s.Graph == null) {
                    continue;
                }
                s.TriggerChange();
                s.Trigger(Skill.Events.ContextChanged, s);
            }
            foreach (var status in ParentModel.Statuses.AllStatuses) {
                status.Skill?.TriggerChange();
            }
        }
    }
}
