using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class Observe : MovementState {
        readonly RotateTowards _rotate = new(0);

        bool _haveSetDestination;
        
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;

        bool CanEnterLookAround => Controller != null && Controller.Npc != null && Controller.Npc.NpcAI.InIdle &&
                                   (Controller.Npc.ParentModel?.TryGetElement<HumanoidCombat>()?.CanLookAround ?? false);
        NpcAnimatorState CurrentAnimatorState => Controller != null ? Controller.Npc?.Element<NpcGeneralFSM>().CurrentAnimatorState : null;
        float _delayNextLookAround;

        protected override void OnEnter() {
            _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            Controller.FinalizeMovement();
            Controller.SetRotationScheme(_rotate, VelocityScheme);
            if (CanEnterLookAround) {
                Controller.Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.LookAround);
            }

            UpdatePlace(true);
        }

        protected override void OnExit() {
            if (CanEnterLookAround && CurrentAnimatorState?.Type == NpcStateType.LookAround) {
                Controller.Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            }
            UpdatePlace(false);
            if (!CanEnterLookAround) {
                return;
            }
            
            if (StateAllowsLookAround()) {
                _delayNextLookAround -= deltaTime;
                if (_delayNextLookAround <= 0) {
                    Controller.Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.LookAround);
                }
            } else {
                _delayNextLookAround = 2.5f;
            }
        }

        bool StateAllowsLookAround() {
            switch (CurrentAnimatorState?.Type) {
                case NpcStateType.LookAround:
                case NpcStateType.AlertLookAround:
                case NpcStateType.AlertLookAt:
                case NpcStateType.AlertStart:
                case NpcStateType.AlertStartQuick:
                    return false;
                default:
                    return true;
            }
        }

        void UpdatePlace(bool lookForwardFallback) {
            Vector3? lookPos = Controller.Npc?.GetCurrentTarget()?.Coords ?? Controller.Npc?.NpcAI?.AlertTarget;
            if (lookPos.HasValue) {
                _rotate.LookAt(lookPos.Value);
            } else if (lookForwardFallback) {
                _rotate.LookAt(Vector3.zero);
            }
        }
    }
}