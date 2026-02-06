using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class SyncGroupInteractionPart : GroupInteractionPart, IAnimatorBridgeStateProvider {
        IEventListener _mainInteractionListener;
        bool _animatorBridgeStateProviderRegistered;
        
        public bool AlwaysAnimate => true;
        
        public SyncGroupInteractionPart(GroupInteraction parentInteraction, INpcInteraction interaction) : base(parentInteraction, interaction) { }
        
        public override void Unbook(NpcElement npc) {
            ToggleSync(false, npc);
            base.Unbook(npc);
        }
        
        public override void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            ToggleSync(true, npc);
            base.StartInteraction(npc, reason);
        }

        public override void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            ToggleSync(false, npc);
            base.StopInteraction(npc, reason);
        }

        public override void ResumeInteraction(NpcElement npc, InteractionStartReason reason) {
            ToggleSync(true, npc);
            base.ResumeInteraction(npc, reason);
        }

        public override void PauseInteraction(NpcElement npc, InteractionStopReason reason) {
            ToggleSync(false, npc);
            base.PauseInteraction(npc, reason);
        }

        void ToggleSync(bool enable, NpcElement npc) {
            if (enable) {
                if (ParentInteraction.FirstNpc is { } firstNpc && firstNpc != npc) {
                    _mainInteractionListener ??= firstNpc.ListenTo(CustomLoop.Events.CustomLoopEnded, _ => OnCustomLoopEnded(firstNpc, npc), npc);
                }
                ToggleAnimationBridge(true, npc);
            } else {
                World.EventSystem.TryDisposeListener(ref _mainInteractionListener);
                ToggleAnimationBridge(false, npc);
            }
        }
        
        void ToggleAnimationBridge(bool toSync, NpcElement npc) {
            if (toSync == _animatorBridgeStateProviderRegistered) {
                return;
            }
            if (npc is not { Controller: { Animator: { } animator}}) {
                return;
            }
            _animatorBridgeStateProviderRegistered = toSync;
            var bridge = AnimatorBridge.GetOrAddDefault(animator);
            if (toSync) {
                bridge.RegisterStateProvider(this);
            } else {
                bridge.UnregisterStateProvider(this);
            }
        }

        void OnCustomLoopEnded(NpcElement firstNpc, NpcElement npc) {
            if (firstNpc is not { HasBeenDiscarded: false, IsAlive: true, NpcAI: { Working: true, InIdle: true } }) {
                return;
            }
            if (npc is not { HasBeenDiscarded: false, IsAlive: true, NpcAI: { Working: true, InIdle: true } }) {
                return;
            }
            var firstNpcState = firstNpc.GetAnimatorSubstateMachine(NpcFSMType.CustomActionsFSM)?.CurrentAnimatorState;
            if (firstNpcState?.Type is not NpcStateType.CustomLoop) {
                return;
            }
            var state = npc.GetAnimatorSubstateMachine(NpcFSMType.CustomActionsFSM)?.CurrentAnimatorState;
            if (state?.Type is not NpcStateType.CustomLoop) {
                return;
            }
            
            state.CurrentState.SetNormalizedTimeWithEventsInvoke(AnimancerUtils.SynchronizeNormalizedTime(firstNpcState.CurrentState, firstNpc.GetDeltaTime()));
        }
    }
}