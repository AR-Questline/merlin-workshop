using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Change Interactability"), NodeSupportsOdin]
    public class SEditorLocationChangeInteractability : EditorStep {
        [RichEnumExtends(typeof(LocationInteractability))]
        public RichEnumReference targetInteractability;
        public LocationReference locationReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationChangeInteractability {
                targetInteractability = targetInteractability,
                locationReference = locationReference
            };
        }
    }

    public partial class SLocationChangeInteractability : StoryStepWithLocationRequirement {
        public RichEnumReference targetInteractability;
        public LocationReference locationReference;

        protected override LocationReference RequiredLocations => locationReference;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(targetInteractability.EnumAs<LocationInteractability>());
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationChangeInteractability;

            [Saved] LocationInteractability _targetInteractability;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(LocationInteractability targetInteractability) {
                _targetInteractability = targetInteractability;
            }
            
            public override void Execute(Location location) {
                if (location.HasElement<NpcElement>()) {
                    Log.Important?.Error($"NPC {location.DisplayName} has changed interactability to {_targetInteractability.EnumName}, it shouldn't happen.");
                }
                location.SetInteractability(_targetInteractability);
            }
        }
    }
}