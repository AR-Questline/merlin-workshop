using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.Rotation;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class RotateToInteraction : ITempInteraction {
        NpcElement _npc;
        Vector3 _targetForward;
        NpcOverridesFSM _overrideFSM;
        SnapToPositionAndRotate _snapToPositionAndRotate;
        
        public bool FastStart { get; private set; }
        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => true;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public event Action OnInternalEnd;

        public RotateToInteraction() {
            FastStart = false;
        }

        public void Setup(Vector3 targetForward) {
            _targetForward = targetForward;
        }

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => _targetForward;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);
        public void Unbook(NpcElement npc) => _npc = null;

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (reason is InteractionStartReason.NPCActivated or InteractionStartReason.NPCReactivatedFromGameLoad) {
                FastStart = true;
                End();
                return;
            }
            FastStart = false;
            
            _overrideFSM ??= npc.Element<NpcOverridesFSM>();
            NpcRotate.TryEnterRotationState(npc, _targetForward);
            
            World.Services.Get<UnityUpdateProvider>().RegisterRotateToInteraction(this);
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            World.Services.Get<UnityUpdateProvider>().UnregisterRotateToInteraction(this);
            if (npc is { HasBeenDiscarded: false }) {
                npc.Movement.ResetMainState(_snapToPositionAndRotate);
            }
            _snapToPositionAndRotate = null;
        }
        
        public bool IsStopping(NpcElement npc) => false;

        public static bool InCorrectRotation(NpcElement npc, INpcInteraction targetInteraction) => Vector3.Angle(npc.Forward(), targetInteraction.GetInteractionForward(npc)) <= NpcRotate.MinimumAngleToRotate;
        public bool InCorrectRotation(NpcElement npc) => Vector3.Angle(npc.Forward(), _targetForward) <= NpcRotate.MinimumAngleToRotate; // npc.Hips.parent.forward?

        public void UnityUpdate() {
            if (_npc == null || _npc.HasBeenDiscarded) {
                World.Services.Get<UnityUpdateProvider>().UnregisterRotateToInteraction(this);
                return;
            }
            
            if (_overrideFSM.CurrentAnimatorState is not NpcRotate) {
                End();
            }
        }
        
        void End() {
            if (_npc == null) {
                return;
            }
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }

        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}