using System;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Look At"), NodeSupportsOdin]
    public class SEditorNpcLookAt : EditorStep {
        public bool waitForEnd;
        [Header("Source"), Tooltip("Who should look? Doesn't require adding to story beforehand.")]
        public LocationReference source = new() {targetTypes = TargetType.Actor};
        
        [Header("Target"), Tooltip("What should be looked at?")]
        public SNpcLookAt.TargetType lookAtType = SNpcLookAt.TargetType.Location;
        
        [ShowIf(nameof(ShowLocationRef)), Tooltip("Location to look at.")]
        public LocationReference target = new() {targetTypes = TargetType.Actor};

        [ShowIf(nameof(ShowVector3))]
        public Vector3 targetPos;

        bool ShowLocationRef => lookAtType == SNpcLookAt.TargetType.Location;
        bool ShowVector3 => lookAtType is SNpcLookAt.TargetType.WorldPosition or SNpcLookAt.TargetType.LocalPosition;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcLookAt {
                waitForEnd = waitForEnd,
                source = source,
                lookAtType = lookAtType,
                target = target,
                targetPos = targetPos
            };
        }
    }
    
    public partial class SNpcLookAt : StoryStep {
        const float MaxWaitTime = 1f;
        
        public bool waitForEnd;
        public LocationReference source;
        public TargetType lookAtType;
        public LocationReference target;
        public Vector3 targetPos;
        
        public override StepResult Execute(Story story) {
            NpcElement sourceNpc = source.FirstOrDefault(story)?.TryGetElement<NpcElement>();
            if (sourceNpc == null) {
                Log.Important?.Error("Couldn't find source Npc for LookAt step");
                return StepResult.Immediate;
            }

            GroundedPosition lookAtPos = ExtractTargetPos(story, sourceNpc);
            if (lookAtPos == null) {
                Log.Important?.Error("Couldn't find Target Position for LookAt step");
            } else {
                UniTask task = NpcLookAt(sourceNpc, lookAtPos);
                if (waitForEnd && !DebugReferences.FastStory && !DebugReferences.ImmediateStory) {
                    StepResult result = new();
                    WaitForRotation(task, result).Forget();
                    return result;
                }
            }

            return StepResult.Immediate;
        }

        async UniTaskVoid WaitForRotation(UniTask task, StepResult result) {
            await task;
            result.Complete();
        }

        GroundedPosition ExtractTargetPos(Story api, NpcElement sourceNpc) {
            return lookAtType switch {
                TargetType.Hero => GroundedPosition.ByGrounded(api.Hero),
                TargetType.Location => GroundedPosition.ByGrounded(target.FirstOrDefault(api)),
                TargetType.WorldPosition => GroundedPosition.ByPosition(targetPos),
                TargetType.LocalPosition => GroundedPosition.ByPosition(sourceNpc.Coords + targetPos),
                _ => throw new NotImplementedException()
            };
        }
        
        public static async UniTask NpcLookAt(NpcElement source, GroundedPosition target, bool canCancel = false, bool lookAtOnlyWithHead = false) {
            if (source.HasElement<SimpleInteractionExitMarker>()) {
                if (!await AsyncUtil.WaitWhile(source, source.HasElement<SimpleInteractionExitMarker>)) {
                    return;
                }
            }

            var interaction = source.Behaviours.CurrentInteraction;
            if (interaction == null || !interaction.LookAt(source, target, lookAtOnlyWithHead)) {
                return;
            }

            Vector3 previousDir = source.CharacterView?.transform.forward ?? Vector3.forward;
            Vector3 newDir = target != null ? (target.Coords - source.Coords).normalized : Vector3.forward;

            float dot = Vector3.Dot(previousDir, newDir);
            dot = -0.5f * dot + 0.5f; // bring it to 0-1 space, where 0 equals no change and 1 equals 180 degrees change
            float waitTime = Easing.Quadratic.Out(dot) * MaxWaitTime;
            if (canCancel) {
                await AsyncUtil.BlockInputUntilDelay(source, waitTime, cancelable: true);
            } else {
                await UniTask.Delay((int) (waitTime * 1000f));
            }
        }

        [Serializable]
        public enum TargetType {
            Location = 0,
            Hero = 1,
            WorldPosition = 2,
            LocalPosition = 3,
        }
    }
}