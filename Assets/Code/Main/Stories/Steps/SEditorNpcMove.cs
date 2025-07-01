using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Move To"), NodeSupportsOdin]
    public class SEditorNpcMove : SEditorCharacterMoveBase {
        [Header("Locations that should move"), PropertyOrder(-9)]
        public LocationReference targets;
        [PropertyOrder(-9)]
        public bool shouldUninvolve;
        [PropertyOrder(-9)]
        public bool shouldInvolveAfterMoved;
        [PropertyOrder(-9), RichEnumExtends(typeof(VelocityScheme))]
        public RichEnumReference movementType = VelocityScheme.Walk;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcMove {
                targets = targets,
                shouldUninvolve = shouldUninvolve,
                shouldInvolveAfterMoved = shouldInvolveAfterMoved,
                movementType = movementType,
                
                stoppingDistance = stoppingDistance,
                waitForEnd = waitForEnd,
                moveToType = moveToType,
                target = target,
                targetPos = targetPos,
                moveToOffsetType = moveToOffsetType,
                offset = offset,
            };
        }
    }

    public partial class SNpcMove : SCharacterMoveBase {
        public LocationReference targets;
        public bool shouldUninvolve;
        public bool shouldInvolveAfterMoved;
        public RichEnumReference movementType = VelocityScheme.Walk;

        protected override IEnumerable<ICharacter> CharactersToMove(Story api) {
            return targets.MatchingLocations(api).Select(loc => loc.TryGetElement<NpcElement>());
        }

        protected override void TryMoveCharacter(ICharacter character, Story api, StepResult result) {
            if (character is NpcElement npc) {
                MoveNpc(npc, api, result).Forget();
            }
        }

        async UniTaskVoid MoveNpc(NpcElement npc, Story api, StepResult result) {
            HashSet<NpcElement> npcs = new();
            HashSet<NpcElement> arrivedNpcs = new();
            npcs.Add(npc);

            if (shouldUninvolve) {
                await api.SetupNpc(npc, false, false, false, true);
            }

            var commuteTo = new StoryCommuteToPosition(movementType.EnumAs<VelocityScheme>() ?? VelocityScheme.Walk, waitForEnd, (shouldInvolveAfterMoved ? api : null));
            commuteTo.Setup(ExtractTargetPos(api, npc));
            commuteTo.OnInternalEnd += () => {
                arrivedNpcs.Add(npc);
                if (npc?.HasElement<NpcMovement>() ?? false) {
                    npc.Movement.Controller.SteeringDirection = api.Hero.Coords.ToHorizontal2() - npc.Coords.ToHorizontal2();
                }
                if (arrivedNpcs.IsSupersetOf(npcs)) {
                    result.Complete();
                    npcs.Clear();
                    arrivedNpcs.Clear();
                }
            };
            
            npc.Behaviours?.PushToStack(commuteTo);
        }
    }
}