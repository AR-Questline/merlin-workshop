using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Rotation {
    public abstract partial class NpcRotate : NpcAnimatorState<NpcOverridesFSM> {
        public const float MinimumAngleToRotate = 20f;
        
        bool _canExit;
        
        public override bool ResetMovementSpeed => true;
        public override bool CanBeExited => _canExit;
        [UnityEngine.Scripting.Preserve] Vector3 RootForward => ParentModel.RootForward;
        
        public new static class Events {
            public static readonly Event<NpcElement, bool> RotationStopped = new(nameof(RotationStopped));
        }
        
        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            Entered = false;
            NpcAnimancer.GetAnimancerNode(StateToEnter,
                n => {
                    OnNodeLoaded(n, overrideCrossFadeTime);
                    onNodeLoaded?.Invoke(n);
                }, OnFailedFindNode, false);
        }
        
        void OnFailedFindNode() {
            StopRotation(true);
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canExit = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                StopRotation();
            }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
            
            if (ParentModel.ParentModel.NpcAI is { IsOrWillBeInNonPacifistState: true }) {
                StopRotation(true, true);
            }
        }
        
        public void StopRotation(bool instant = false, bool aborted = false) {
            _canExit = true;
            ParentModel.SetCurrentState(NpcStateType.None, instant ? 0f : null);
            Npc.Trigger(Events.RotationStopped, aborted);
        }
        
        public static bool TryEnterRotationState(NpcElement npcElement, Vector3 desiredForward, bool force = false) {
            var npcOverridesFSM = npcElement.Element<NpcOverridesFSM>();
            
            if (npcOverridesFSM.CurrentAnimatorState is NpcRotate currentRotateState) {
                currentRotateState.StopRotation();
                npcElement.Controller.ResetTargetRootRotation();
            }
            
            var initialForward = npcOverridesFSM.RootForward;
            float angle = Vector3.SignedAngle(initialForward, desiredForward, Vector3.up);
            
            if (!force && math.abs(angle) < MinimumAngleToRotate) {
                return false;
            }
            
            NpcRotate desiredState = angle switch {
                >= 120 => npcOverridesFSM.Element<NpcRotateRight180>(),
                >= 60 => npcOverridesFSM.Element<NpcRotateRight90>(),
                >= 0 => npcOverridesFSM.Element<NpcRotateRight45>(),
                <= -120 => npcOverridesFSM.Element<NpcRotateLeft180>(),
                <= -60 => npcOverridesFSM.Element<NpcRotateLeft90>(),
                _ => npcOverridesFSM.Element<NpcRotateLeft45>(),
            };

            npcOverridesFSM.SetCurrentState(desiredState.Type);
            return true;
        }
        
        public static void AbortRotationState(NpcElement npcElement) {
            var npcOverridesFSM = npcElement.Element<NpcOverridesFSM>();
            if (npcOverridesFSM.CurrentAnimatorState is NpcRotate currentRotateState) {
                currentRotateState.StopRotation(true, true);
            }
        }

        public static bool IsInRotation(NpcElement npcElement) {
            var npcOverridesFSM = npcElement.Element<NpcOverridesFSM>();
            return npcOverridesFSM.CurrentAnimatorState is NpcRotate;
        }
    }
}