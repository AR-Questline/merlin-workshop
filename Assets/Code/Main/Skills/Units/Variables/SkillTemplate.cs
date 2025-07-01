using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillLocationTemplate : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("LocationTemplate", f => this.Skill(f).GetTemplate<LocationTemplate>(name));
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillItemTemplate : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("ItemTemplate", f => this.Skill(f).GetTemplate<ItemTemplate>(name));
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillStatusTemplate : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("StatusTemplate", f => this.Skill(f).GetTemplate<StatusTemplate>(name));
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillFactionTemplate : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("FactionTemplate", f => this.Skill(f).GetTemplate<FactionTemplate>(name));
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillStoryGraphTemplate : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        
        protected override void Definition() {
            ValueOutput("Bookmark", f => {
                var graphReference = this.Skill(f).GetTemplateReference(name);
                return StoryBookmark.ToInitialChapter(graphReference);
            });
        }
    }
}