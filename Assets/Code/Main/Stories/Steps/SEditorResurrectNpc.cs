using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Execution;
using System;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Resurrect")]
    public class SEditorResurrectNpc : EditorStep {
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference[] npcs = Array.Empty<TemplateReference>();

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SResurrectNpc {
                npcs = npcs
            };
        }
    }

    public partial class SResurrectNpc : StoryStep {
        public TemplateReference[] npcs = Array.Empty<TemplateReference>();
        
        public override StepResult Execute(Story story) {
            foreach (var npc in npcs) {
                var template = npc.Get<LocationTemplate>();
                if (template != null) {
                    NpcRegistry.Resurrect(npc.Get<LocationTemplate>());
                }
            }
            return StepResult.Immediate;
        }
    }
}