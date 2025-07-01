using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Change Faction"), NodeSupportsOdin]
    public class SEditorChangeNpcFaction : EditorStep {
        public LocationReference locations;
        
        [Tooltip("If true, the NPC will reset to its default faction.")]
        public bool @default;
        
        [TemplateType(typeof(FactionTemplate)), HideIf(nameof(ShouldHideFaction))]
        public TemplateReference faction;
        
        // we cannot check @default directly as odin uses @ for a diffderent behaviour
        bool ShouldHideFaction => @default;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeNpcFaction {
                locations = locations,
                @default = @default,
                faction = faction
            };
        }
    }

    public partial class SChangeNpcFaction : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public bool @default;
        public TemplateReference faction;

        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(@default, faction);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_ChangeNpcFaction;

            [Saved] bool _default;
            [Saved] TemplateReference _faction;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool toDefault, TemplateReference faction) {
                _default = toDefault;
                _faction = faction;
            }
            
            public override void Execute(Location location) {
                if (_default) {
                    location.TryGetElement<NpcElement>()?.ResetFactionOverride();
                } else {
                    location.TryGetElement<NpcElement>()?.OverrideFaction(_faction.Get<FactionTemplate>());
                }
            }
        }
    }
}