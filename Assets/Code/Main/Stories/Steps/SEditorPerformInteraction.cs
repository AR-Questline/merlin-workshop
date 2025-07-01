using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.Tags;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Interactions: Perform"), NodeSupportsOdin]
    public class SEditorPerformInteraction : EditorStep {
        [Tooltip("Soft:\n  - Involved: immediate start; end when uninvolved or ended by itself\n  - Uninvolved: immediate start; end when new idle interval or ended by itself or interrupted by hero/combat\n\n" +
                 "Hard:\n  - Involved: start after story; end when RemoveAllOverrides or ended by itself\n  - Uninvolved: immediate start; end when RemoveAllOverrides or ended by itself\n  - It's persistent between saves\n\n" +
                 "UntilStoryEnd:\n  - Involved: stop current involvement, start interaction and involve again. Ends when Story ends \n  - Uninvolved: Start interaction. Ends when Story ends\n\n" +
                 "UntilNpcInvolvementEnd:\n  - Involved: stop current involvement, start interaction and involve again. Ends when StopInvolve is triggered or Story ends \n  - Uninvolved: Start interaction and involve. Ends when StopInvolve is triggered or Story ends")]
        [NodeEnum] 
        [FormerlySerializedAs("forgetCurrentInteractions")]
        public SPerformInteraction.InteractionType type = SPerformInteraction.InteractionType.Hard;
        [ShowIf(nameof(CanAwaitForStart))]
        public bool awaitForPerformStart = true;
        public bool interactionShouldSkipStartAnimation;
        public LocationReference locations;
        
        [Tags(TagsCategory.InteractionID)] 
        public string uniqueID;
        
        [Tooltip("If interaction ends by itself, this bookmark is triggered\nIt is not triggered on RemoveAllOverrides")]
        [ShowIf(nameof(HasCallback))]
        public StoryBookmark callback;
        
        bool HasCallback => type == SPerformInteraction.InteractionType.Hard;
        bool CanAwaitForStart => type is SPerformInteraction.InteractionType.UntilStoryEnd or SPerformInteraction.InteractionType.UntilNpcInvolvementEnd;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPerformInteraction {
                type = type,
                awaitForPerformStart = awaitForPerformStart,
                interactionShouldSkipStartAnimation = interactionShouldSkipStartAnimation,
                locations = locations,
                uniqueID = uniqueID,
                callback = callback
            };
        }
    }

    public partial class SPerformInteraction : StoryStepWithLocationRequirementAllowingWait {
        public InteractionType type;
        public bool awaitForPerformStart;
        public bool interactionShouldSkipStartAnimation;
        public LocationReference locations;
        public string uniqueID;
        public StoryBookmark callback;
        
        protected override LocationReference RequiredLocations => locations;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(type, uniqueID, callback, interactionShouldSkipStartAnimation, interactionShouldSkipStartAnimation);
        }

        public partial class StepExecution : DeferredLocationExecutionAllowingWait {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_PerformInteraction;

            [Saved] InteractionType _type;
            [Saved] string _uniqueID;
            [Saved] StoryBookmark _callback;
            [Saved] bool _awaitForPerformStart;
            [Saved] bool _interactionShouldSkipStartAnimation;
            
            public override bool ShouldPerformAndWait => _type is InteractionType.UntilStoryEnd or InteractionType.UntilNpcInvolvementEnd;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(InteractionType type, string uniqueID, StoryBookmark callback, bool awaitForPerformStart, bool interactionShouldSkipStartAnimation) {
                _type = type;
                _uniqueID = uniqueID;
                _callback = callback;
                _awaitForPerformStart = awaitForPerformStart;
                _interactionShouldSkipStartAnimation = interactionShouldSkipStartAnimation;
            }
            
            public override void Execute(Location location) {
                var behaviours = location.TryGetElement<NpcElement>()?.Behaviours;
                if (behaviours == null) {
                    return;
                }
                
                switch (_type) {
                    case InteractionType.Hard:
                        InteractionStartReason? overridenStartReason = _interactionShouldSkipStartAnimation ? InteractionStartReason.InteractionFastSwap : null;
                        behaviours.AddOverride(new InteractionUniqueFinder(_uniqueID), _callback, overridenStartReason: overridenStartReason);
                        break;
                    case InteractionType.Soft:
                        behaviours.DropToAnchor().Forget();
                        behaviours.PushToStack(new InteractionUniqueFinder(_uniqueID).Interaction(behaviours.Npc));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            public override async UniTask ExecuteAndWait(Location location, Story api) {
                var behaviours = location.TryGetElement<NpcElement>()?.Behaviours;
                if (behaviours == null) {
                    return;
                }
                if (api == null || api.HasBeenDiscarded) {
                    return;
                }
                
                switch (_type) {
                    case InteractionType.UntilStoryEnd:
                        bool isInvolved = behaviours.HasAnchor;
                        if (_awaitForPerformStart) {
                            await ApplyStoryBasedOverride(api, behaviours, isInvolved, isInvolved, false);
                        } else {
                            ApplyStoryBasedOverride(api, behaviours, isInvolved, isInvolved, false).Forget();
                        }
                        break;
                    case InteractionType.UntilNpcInvolvementEnd:
                        if (_awaitForPerformStart) {
                            await ApplyStoryBasedOverride(api, behaviours, behaviours.HasAnchor, true, true);
                        } else {
                            ApplyStoryBasedOverride(api, behaviours, behaviours.HasAnchor, true, true).Forget();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            async UniTask ApplyStoryBasedOverride(Story api, IdleBehaviours behaviours, bool removeAnchor, bool applyAnchor, bool listenToStopInvolve) {
                var involvement = applyAnchor 
                    ? await NpcInvolvement.GetOrCreateFor(api, behaviours.Npc, true) 
                    : NpcInvolvement.GetFor(api, behaviours.Npc);
                if (removeAnchor) {
                    await involvement.EndTalk();
                }
                InteractionStartReason? overridenStartReason = _interactionShouldSkipStartAnimation ? InteractionStartReason.InteractionFastSwap : null;
                var interactionStoryBasedOverride = new InteractionStoryBasedOverride(api, listenToStopInvolve, new InteractionUniqueFinder(_uniqueID), 
                    overridenStartReason: overridenStartReason);
                behaviours.AddInteractionSpecificSource(interactionStoryBasedOverride);
                if (!await AsyncUtil.WaitUntil(api, () => interactionStoryBasedOverride.Started)) {
                    return;
                }
                if (applyAnchor) {
                    await involvement.StartTalk();
                }
            }
        }

        public enum InteractionType : byte {
            Soft,
            Hard,
            UntilStoryEnd,
            UntilNpcInvolvementEnd,
        }
    }
}