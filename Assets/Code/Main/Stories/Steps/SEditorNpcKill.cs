using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Kill"), NodeSupportsOdin]
    public class SEditorNpcKill : EditorStep {
        public LocationReference locations;
        [LabelWidth(150)] public bool markDeathAsNonCriminal = true;
        [LabelWidth(150)] public bool allowPrevention = false;
        [FoldoutGroup("Advanced"), LabelWidth(150)] public bool keepBodyForever = false;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcKill {
                locations = locations,
                markDeathAsNonCriminal = markDeathAsNonCriminal,
                allowPrevention = allowPrevention,
                keepBodyForever = keepBodyForever
            };
        }
    }

    public partial class SNpcKill : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public bool markDeathAsNonCriminal;
        public bool allowPrevention;
        public bool keepBodyForever;
        
        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(markDeathAsNonCriminal, allowPrevention, keepBodyForever);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcKill;

            [Saved] bool _markDeathAsNonCriminal;
            [Saved] bool _allowPrevention;
            [Saved] bool _keepBodyForever;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool markDeathAsNonCriminal, bool allowPrevention, bool keepBodyForever) {
                _markDeathAsNonCriminal = markDeathAsNonCriminal;
                _allowPrevention = allowPrevention;
                _keepBodyForever = keepBodyForever;
            }
            
            public override void Execute(Location location) {
                if (_keepBodyForever && location.TryGetElement<NpcElement>(out var npc)) {
                    npc.KeepCorpseAfterDeath = true;
                }
                location.Kill(markDeathAsNonCriminal: _markDeathAsNonCriminal, allowPrevention: _allowPrevention);
            }
        }
    }
}