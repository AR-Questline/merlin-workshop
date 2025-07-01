using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Movement.States {
    public class RagdollMovement : MovementState {
        readonly Vector3 _forceDirection;
        readonly float _ragdollForce;
        float _restoreDelay;
        readonly bool _exitWhileIsSleeping;
        bool _exited;
        Rigidbody _rb;
        IEventListener _standUpListener;
        
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;

        public RagdollMovement(Vector3 forceDirection, float ragdollForce, float durationLeft, bool exitWhileIsSleeping = true) {
            _forceDirection = forceDirection;
            _ragdollForce = ragdollForce;
            _restoreDelay = 0.5f + durationLeft;
            _exitWhileIsSleeping = exitWhileIsSleeping;
        }

        protected override void OnEnter() {
            _exited = false;
            DeathElement deathElement = Controller.Npc?.TryGetElement<DeathElement>();
            deathElement?.GetBehaviour<DeathRagdollNpcBehaviour>()?.EnableRagdoll(_forceDirection * _ragdollForce, hitPosition: Npc.Hips.position);
            _rb = Controller.RootBone.GetComponent<Rigidbody>();
            Controller.SetRotationScheme(new NoRotationChange(), VelocityScheme);
            Controller.AlivePrefab.SetActive(false);
            Controller.ToggleGlobalRichAIActivity(false);
        }

        protected override void OnExit() {
            if (Npc is { HasBeenDiscarded: false } && !_exited) {
                Log.Important?.Error("Exited RagdollMovement from outside call! This is not valid! Please Fix!");
            }
        }

        public void ExitRagdoll(Vector3? newPosition = null, bool instant = false, bool disableFallDamage = true) {
            if (Npc == null || _exited) {
                return;
            }

            if (Npc.IsUnconscious) {
                return;
            }
            
            _exited = true;
            if (newPosition == null) {
                NNInfo nearest = AstarPath.active.GetNearest(Controller.RootBone.position, NNConstraint.Walkable);
                newPosition = nearest.node != null ? nearest.position : Controller.RootBone.position;
            }

            if (disableFallDamage) {
                Controller.DisableFallDamageForExitingRagdoll();
            }

            if (Npc.IsAlive) {
                StandUpFromRagdoll(newPosition.Value, instant).Forget();
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (_exited || deltaTime <= 0) {
                return;
            }
            
            _restoreDelay -= deltaTime;
            bool shouldExit = _restoreDelay <= 0 || (_exitWhileIsSleeping && _rb != null && _rb.IsSleeping());
            shouldExit = shouldExit && !Npc.IsStunned && !Npc.IsUnconscious;
            if (!shouldExit) {
                return;
            }
            // --- Increase search radius if some time passed and we still haven't found any point on navmesh near by
            float sampleRadius = _restoreDelay > -5f ? 1f : 3f;
            
            float originalMaxNearestNodeDistance = AstarPath.active.maxNearestNodeDistance;
            AstarPath.active.maxNearestNodeDistance = sampleRadius;
            NNInfo nearest = AstarPath.active.GetNearest(Controller.RootBone.position, NNConstraint.Walkable);
            AstarPath.active.maxNearestNodeDistance = originalMaxNearestNodeDistance;
            
            if (nearest.node != null) {
                ExitRagdoll(nearest.position, disableFallDamage: false);
                _exited = true;
                End();
            } else if (_restoreDelay < -15f) {
                // --- If we landed in place where there is no navmesh, or fell below map we should die
                Npc.ParentModel.Kill();
                _exited = true;
            }
        }
        
        async UniTaskVoid StandUpFromRagdoll(Vector3 newPosition, bool instant = false) {
            // if not grounded delay few frames cause AI can die from fall damage.
            if (!instant && !Npc.Movement.Controller.Grounded && !await AsyncUtil.DelayFrame(Npc, 10)) {
                return;
            }

            if (!Npc.IsAlive) {
                return;
            }
            
            ExitRagdollInternal(newPosition, instant);
        }

        void ExitRagdollInternal(Vector3 newPosition, bool instant = false) {
            Npc.SetAnimatorState(NpcFSMType.AdditiveFSM, NpcStateType.None, 0f);
            Npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, 0f);
            Npc.SetAnimatorState(NpcFSMType.TopBodyFSM, NpcStateType.None, 0f);

            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, instant ? NpcStateType.Idle : NpcStateType.StandUp, 0f, _ => {
                OnStandUpAnimationLoaded(newPosition);
            });
            
            if (instant) {
                OnStandUpFinished(true);
            } else {
                _standUpListener = Npc.ListenTo(EnemyBaseClass.Events.StandUpFinished, OnStandUpFinished, Npc);
            }
        }

        void OnStandUpAnimationLoaded(Vector3 newPosition) {
            NpcElement npc = Npc;
            npc.Element<DeathElement>().GetBehaviour<DeathRagdollNpcBehaviour>()?.DisableRagdoll();
            npc.ParentModel.SafelyMoveTo(newPosition);
            Controller.AlivePrefab.SetActive(true);
            ICharacter target = npc.GetCurrentTarget();
            Vector3 dir = target != null ? target.Coords - npc.Coords : Controller.RootBone.forward;
            Controller.SetRotationInstant(Quaternion.LookRotation(dir, Vector3.up));
            Controller.RichAI.ForceTeleport(npc.Coords);
            Controller.ToggleGlobalRichAIActivity(true);
        }

        void OnStandUpFinished(bool _) {
            if (_standUpListener != null) {
                World.EventSystem.RemoveListener(_standUpListener);
                _standUpListener = null;
            }
            
            Movement.StopInterrupting();
            Npc.ParentModel.TryGetElement<EnemyBaseClass>()?.StartWaitBehaviour();
        }
    }
}