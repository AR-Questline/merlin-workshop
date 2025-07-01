using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class VMountAnimator {
        public const float ForwardMovementAnimatorScalar = 0.5f;
        public const float TurningMovementAnimatorScalar = 1.0f;
        
        const float DefaultIdleAnimationInterval = 6.3f;
        const float CustomIdleAnimationInterval = 3.150f;
        const float RearingIdleAnimationInterval = 2.070f;
        const int DefaultIdleAnimationIndex = 0;
        const int MinCustomIdleAnimationIndex = 1;
        const int MaxCustomIdleAnimationIndex = 4;
        const int RearingIdleAnimationIndex = 4;
        const string RandomIdleParameterName = "RandomIdle";
        const float GroundedAnimationBufferTime = 0.2f;
        
        static readonly int Horizontal = Animator.StringToHash("Horizontal");
        static readonly int Forward = Animator.StringToHash("Vertical");
        static readonly int AnimState = Animator.StringToHash("State");
        static readonly int Grounded = Animator.StringToHash("Grounded");
        static readonly int LastAnimState = Animator.StringToHash("LastState");

        public enum State : byte {
            [UnityEngine.Scripting.Preserve] Idle = 0,
            [UnityEngine.Scripting.Preserve] Locomotion = 1,
            [UnityEngine.Scripting.Preserve] Jump = 2,
            [UnityEngine.Scripting.Preserve] Fall = 3,
            [UnityEngine.Scripting.Preserve] Swim = 4,
            [UnityEngine.Scripting.Preserve] Fly = 6,
            [UnityEngine.Scripting.Preserve] Neigh = 7,
            [UnityEngine.Scripting.Preserve] Death = 10,
        }

        float _midairTime;
        
        VMount _mount;
        Animator _animator;
        
        public VMountAnimator(VMount mount) {
            _mount = mount;
            _animator = _mount.GetComponentInChildren<Animator>();
            HandleIdleAnimation().Forget();
        }
        
        async UniTaskVoid HandleIdleAnimation() {

            if (!await PlayDefaultIdleAnimation()) {
                return;
            }
            
            if (!await PlayRandomIdleAnimation()) {
                return;
            }
            
            HandleIdleAnimation().Forget();
        }

        async UniTask<bool> PlayDefaultIdleAnimation() {
            return await PlayIdleAnimation(DefaultIdleAnimationIndex, DefaultIdleAnimationInterval);
        }

        async UniTask<bool> PlayRandomIdleAnimation() {
            int idleAnimationIndex = Random.Range(MinCustomIdleAnimationIndex, MaxCustomIdleAnimationIndex + 1);

            float idleAnimationInterval = idleAnimationIndex == RearingIdleAnimationIndex
                ? RearingIdleAnimationInterval
                : CustomIdleAnimationInterval;
            
            return await PlayIdleAnimation(idleAnimationIndex, idleAnimationInterval);
        }
        
        async UniTask<bool> PlayIdleAnimation(int animationIndex, float animationInterval) {
            if (_animator == null) {
                return false;
            }
            
            _animator.SetInteger(RandomIdleParameterName, animationIndex);
            if (!await AsyncUtil.DelayTime(_mount, animationInterval)) {
                return false;
            }
            
            return true;
        }

        public void Update(float deltaTime) {
            UpdateMidairTimer(deltaTime);
            
            bool modelGrounded = _midairTime <= GroundedAnimationBufferTime;
            _animator.SetBool(Grounded, modelGrounded);

            _animator.SetFloat(Forward, _mount.RunningVelocity * ForwardMovementAnimatorScalar);
            _animator.SetFloat(Horizontal, _mount.TurningVelocity * TurningMovementAnimatorScalar);

            if (_mount.InWater) {
                UpdateState(State.Swim);
            } else if (!_mount.IsInJump()) {
                if (!modelGrounded) {
                    UpdateState(State.Fall);
                } else if (_mount.BreakingAheadOfWall) {
                    UpdateState(State.Neigh);
                } else if (!_mount.IsMostlyStill()) {
                    UpdateState(State.Locomotion);
                } else {
                    UpdateState(State.Idle);
                }
            }
        }

        public void UpdateState(State newState) {
            int newStateInt = (int)newState;
            int lastState = _animator.GetInteger(AnimState);
            if (newStateInt != lastState) {
                _animator.SetInteger(LastAnimState, lastState);
                _animator.SetInteger(AnimState, newStateInt);
            }
        }

        void UpdateMidairTimer(float deltaTime) {
            if (_mount.Grounded) {
                _midairTime = 0.0f;
            } else {
                _midairTime += deltaTime;
            }
        }
    }
}