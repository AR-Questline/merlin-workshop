using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Attachment Groups"), NodeSupportsOdin]
    public class SEditorLocationChangeAttachments : EditorStep {
        public LocationReference locations;
        public SLocationChangeAttachments.ChangeType changeTo;
        public string groupName;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationChangeAttachments {
                locations = locations,
                changeTo = changeTo,
                groupName = groupName
            };
        }
    }

    public partial class SLocationChangeAttachments : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public ChangeType changeTo;
        public string groupName;

        protected override LocationReference RequiredLocations => locations;
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(changeTo, groupName);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationChangeAttachments;

            [Saved] ChangeType _changeTo;
            [Saved] string _groupName;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(ChangeType changeTo, string groupName) {
                _changeTo = changeTo;
                _groupName = groupName;
            }
            
            public override void Execute(Location location) {
                bool enable = _changeTo == ChangeType.Enable;
                if (enable) {
                    location.EnableGroup(_groupName);
                } else {
                    location.DisableGroup(_groupName);
                }
                
                if (location.TryGetElement<NpcPresence>() is { AliveNpc: not null } presence) {
                    World.Services.Get<PresenceTrackerService>().UpdatePresence(
                        new PresenceTrackerService.PresenceUpdate(presence.AliveNpc.Actor, _groupName, enable));
                }
            }
        }
        
        public enum ChangeType {
            Enable = 0,
            Disable = 1,
        }
    }
}