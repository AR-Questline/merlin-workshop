using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.RootMotions {
    public class RootMotion : MonoBehaviour {
        float _walkSpeed = NpcController.DefaultWalkSpeed;
        float _trotSpeed = NpcController.DefaultTrotSpeed;
        float _runSpeed = NpcController.DefaultRunSpeed;
        float _backwardsWalkSpeed = NpcController.DefaultWalkBackwardsSpeed;
        float _backwardsTrotSpeed = NpcController.DefaultTrotBackwardsSpeed;
        float _backwardsRunSpeed = NpcController.DefaultRunBackwardsSpeed;
        
        public float WalkSpeed => IsMovingForward ? _walkSpeed : _backwardsWalkSpeed;
        public float TrotSpeed => IsMovingForward ? _trotSpeed : _backwardsTrotSpeed;
        public float RunSpeed => IsMovingForward ? _runSpeed : _backwardsRunSpeed;

        public float MaxSpeed => RunSpeed;
        
        bool IsMovingForward {
            get {
                if (Controller == null) {
                    return true;
                }
                return Controller.CurrentVelocity == Vector2.zero || Vector2.Dot(Controller.CurrentVelocity, Controller.LogicalForward) >= 0;
            }
        }
        
        Animator Animator { get; set; }
        ARNpcAnimancer NpcAnimancer { get; set; }
        NpcController Controller { get; set; }

        void Awake() {
            Animator = GetComponent<Animator>();
            NpcAnimancer = GetComponent<ARNpcAnimancer>();
            Controller = GetComponentInParent<NpcController>();

            if (Controller == null) {
                return;
            }

            _walkSpeed = Controller.WalkSpeed;
            _trotSpeed = Controller.TrotSpeed;
            _runSpeed = Controller.RunSpeed;
            _backwardsWalkSpeed = Controller.BackwardsWalkSpeed;
            _backwardsTrotSpeed = Controller.BackwardsTrotSpeed;
            _backwardsRunSpeed = Controller.BackwardsRunSpeed;
        }
        
        public void OnUpdate(float deltaTime) {
            if (Controller.Npc.HasBeenDiscarded) {
                return;
            }
            UpdateAnimator(Controller.CurrentVelocity, deltaTime);
        }

        void OnAnimatorMove() {
            OnAnimatorMoved?.Invoke(Animator);
        }

        public event AnimatorMoved OnAnimatorMoved;
        public delegate void AnimatorMoved(Animator animator);

        /// <summary>
        /// Animator parameters works in 0-1 range, that's why velocity passed to this method is divided by max forward speed (RunSpeed).
        /// </summary>
        public void UpdateAnimator(Vector2 velocity, float deltaTime) {
            velocity /= Controller.Npc.CharacterStats.MovementSpeedMultiplier;
            
            float desiredMovementSpeed = velocity.magnitude;
            float desiredVelocityZ = Vector2.Dot(velocity, Controller.LogicalForward);
            float desiredVelocityX = Vector2.Dot(velocity, Controller.LogicalRight);

            bool updateOnlyVertical = Controller.UpdateAnimatorOnlyVertical || Controller.ForwardMovementOnly;
            NpcAnimancer.UpdateVelocity(desiredMovementSpeed, desiredVelocityZ, desiredVelocityX, deltaTime, updateOnlyVertical);

            float angularAngle = Controller.EstimatedAngularVelocity;
            NpcAnimancer.UpdateAngularVelocity(angularAngle, deltaTime);
        }
    }
}