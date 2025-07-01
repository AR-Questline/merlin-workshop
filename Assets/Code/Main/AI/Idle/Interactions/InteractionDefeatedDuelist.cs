using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class InteractionDefeatedDuelist : INpcInteraction {
        public event Action OnInternalEnd;

        readonly ShareableARAssetReference _animations;
        readonly bool _canBeTalkedTo;
        ARInteractionAnimations _arInteractionAnimations;
        CancellationTokenSource _arInteractionAnimationsToken;

        NpcElement _interactingNpc;
        bool _isStopping;
        
        public bool CanBeInterrupted => false;
        public bool AllowBarks => false;
        public bool AllowDialogueAction => _canBeTalkedTo;
        public bool AllowTalk => _canBeTalkedTo;
        public float? MinAngleToTalk => null;
        public int Priority => 99;
        public bool FullyEntered => true;

        public InteractionDefeatedDuelist(ARAssetReference animations, bool canBeTalkedTo) {
            _animations = animations.AsShareable();
            _canBeTalkedTo = canBeTalkedTo;
        }
        
        public Vector3? GetInteractionPosition(NpcElement npc) => npc.Coords;
        public Vector3 GetInteractionForward(NpcElement npc) => npc.Forward();

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) {
            if (_interactingNpc == null) {
                _interactingNpc = npc;
                return InteractionBookingResult.ProperlyBooked;
            } else if (_interactingNpc == npc) {
                return InteractionBookingResult.AlreadyBookedBySameNpc;
            } else {
                return InteractionBookingResult.AlreadyBookedByOtherNpc;
            }
        }

        public void Unbook(NpcElement npc) {
            _interactingNpc = null;
        }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            var duelistElement = npc.TryGetElement<NpcDuelistElement>();
            if (duelistElement == null) {
                StopInteraction(npc, InteractionStopReason.StoppedIdlingInstant);
                return;
            }
            duelistElement.Element<DefeatedNpcDuelistElement>().ListenToLimited(Model.Events.AfterDiscarded, OnElementDiscarded, npc);
            npc.Movement.InterruptState(new SnapToPositionAndRotate(GetInteractionPosition(npc)!.Value, GetInteractionForward(npc), null));
            
            if (!npc.HasElement<BlockEnterCombatMarker>()) {
                npc.AddElement<BlockEnterCombatMarker>();
            }

            if (_arInteractionAnimations != null) {
                Log.Critical?.Error($"NPC {npc} is starting interaction which is still waiting for animations to end for {_arInteractionAnimations.Npc}");
                _arInteractionAnimations.UnloadOverride();
            }
            _arInteractionAnimations = new ARInteractionAnimations(npc, _animations);
            _arInteractionAnimations.LoadOverride();
            Starting(npc).Forget();
        }

        async UniTaskVoid Starting(NpcElement npc) {
            _arInteractionAnimationsToken = new CancellationTokenSource();
            if (!await AsyncUtil.WaitWhile(npc, () => _arInteractionAnimations.IsLoadingOverrides, _arInteractionAnimationsToken)) {
                return;
            }
            npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomEnter);
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            if (reason is InteractionStopReason.MySceneUnloading or InteractionStopReason.NPCPresenceDisabled or InteractionStopReason.Death) {
                Cleanup();
                return;
            }
            if (npc?.HasBeenDiscarded ?? true) {
                Cleanup();
                return;
            }

            _isStopping = true;
            if (_arInteractionAnimations is { IsLoadingOverrides: false }) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomExit);
                npc.ListenToLimited(NpcCustomActionsFSM.Events.CustomStateExited, _ => OnCustomExitExited(npc), npc);
            } else {
                OnCustomExitExited(npc);
            }
        }

        void OnCustomExitExited(NpcElement npc) {
            npc.TryGetElement<BlockEnterCombatMarker>()?.Discard();
            npc.Movement.StopInterrupting();
            Cleanup();
        }

        void Cleanup() {
            _isStopping = false;
            _arInteractionAnimationsToken?.Cancel();
            _arInteractionAnimations?.UnloadOverride();
            _arInteractionAnimations = null;
        }

        void OnElementDiscarded(Model _) {
            TriggerOnEnd();
        }
        
        void TriggerOnEnd() {
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }
        
        public bool IsStopping(NpcElement npc) => _isStopping;
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => _canBeTalkedTo;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}