using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Interactions: Refresh"), NodeSupportsOdin]
    public class SEditorNpcRefreshCurrentBehaviour : EditorStep {
        public LocationReference locationReference;
        public bool shouldWaitForStart;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcRefreshCurrentBehaviour {
                locationReference = locationReference,
                shouldWaitForStart = shouldWaitForStart
            };
        }
    }

    public partial class SNpcRefreshCurrentBehaviour : StoryStepWithLocationRequirementAllowingWait {
        public LocationReference locationReference;
        public bool shouldWaitForStart;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(shouldWaitForStart);
        }

        public partial class StepExecution : DeferredLocationExecutionAllowingWait {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcRefreshCurrentBehaviour;

            [Saved] bool _shouldPerformAndWait;
            
            public override bool ShouldPerformAndWait => _shouldPerformAndWait;

            [JsonConstructor, Preserve]
            StepExecution() { }
            
            internal StepExecution(bool waitForStart) {
                _shouldPerformAndWait = waitForStart;
            }
            
            public override void Execute(Location location) {
                IdleBehaviours behaviours = location.TryGetElement<NpcElement>()?.TryGetElement<IdleBehaviours>();
                behaviours?.RefreshCurrentBehaviour(true);
            }
            
            public override async UniTask ExecuteAndWait(Location location, Story api) {
                var npc = location.TryGetElement<NpcElement>();
                var behaviours = npc?.TryGetElement<IdleBehaviours>();
                if (behaviours == null) {
                    return;
                }
                bool refreshed = false;
                npc.ListenToLimited(NpcInteractor.Events.CurrentInteractionFullyEntered, () => refreshed = true, behaviours);
                behaviours.RefreshCurrentBehaviour(true);
                await AsyncUtil.WaitUntil(behaviours, () => refreshed);
            }
        }
    }
}