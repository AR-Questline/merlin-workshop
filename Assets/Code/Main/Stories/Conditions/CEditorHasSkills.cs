using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero has skills
    /// </summary>
    [Element("Hero: Has Skills")]
    public class CEditorHasSkills : EditorCondition {
        [TemplateType(typeof(SkillGraph))]
        public TemplateReference[] requiredSkills = Array.Empty<TemplateReference>();

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHasSkills {
                requiredSkills = requiredSkills
            };
        }
    }
    
    public partial class CHasSkills : StoryCondition {
        public TemplateReference[] requiredSkills = Array.Empty<TemplateReference>();
        
        public override bool Fulfilled(Story story, StoryStep step) {
            ICharacterSkills skills = Hero.Current?.Skills;
            if (skills == null) {
                return false;
            }

            return requiredSkills.All(rs => skills.AllSkills().Any(s => s.Graph?.GUID == rs.GUID));
        }
    }
}