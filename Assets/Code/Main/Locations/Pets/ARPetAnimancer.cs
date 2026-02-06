using System;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    public class ARPetAnimancer : AnimancerComponent {
        const float DegreesPerMixerUnit = 90.0f;
        
        [SerializeField, Required]
        [ARAssetReferenceSettings(new[] { typeof(ARPetAnimationMapping) }, group: AddressableGroup.Animations)]
        ShareableARAssetReference animations;
        
        ARAssetReference _animationsReference;
        ARPetAnimationMapping _animations;
        AnimancerState _currentAnimancerState;
        MixerState<Vector2> _currentMovementState;
        bool _animationsLoaded;
        
        public State CurrentState { get; private set; }

        public bool CanRotate => _animationsLoaded && (CurrentState is State.Idle or State.Movement);
        public bool CanMove => _animationsLoaded && (CurrentState is State.Idle or State.Movement or State.Pet);

        Action _onAnimationsLoaded;
        public event AnimatorMoved OnAnimatorMoved;
        public delegate void AnimatorMoved(Animator animator);
        
        protected override void OnEnable() {
            base.OnEnable();
            InitializePetAnimancer().Forget();
        }
        
        protected override void OnDisable() {
            UnloadAnimations();
            base.OnDisable();
        }
        
        async UniTaskVoid InitializePetAnimancer() {
            _animationsReference = animations.Get();
            if (_animationsReference is not { IsSet: true }) {
                Log.Important?.Error("Pet does not have base animations set!", gameObject);
                return;
            }
            
            var result = await _animationsReference.LoadAsset<ARPetAnimationMapping>();
            
            if (_animationsReference == null || this == null || Hero.Current.HasBeenDiscarded) {
                _animationsReference?.ReleaseAsset();
                _animationsReference = null;
                return;
            }
            
            if (result == null) {
                Log.Important?.Error("Failed to load base animations for Animancer! Pet will be broken!", gameObject);
                return;
            }
            
            _animations = result;
            _animationsLoaded = true;
            _onAnimationsLoaded?.Invoke();
            _onAnimationsLoaded = null;
        }

        void OnAnimatorMove() {
            OnAnimatorMoved?.Invoke(Animator);
        }

        public void UpdateToPet(VCPetController petController, float deltaTime) {
            if (!_animationsLoaded) {
                return;
            }
            
            if (petController.IsMoving()) {
                if (_currentMovementState == null) {
                    PlayAnimationState(State.Movement);
                }
                UpdateMovementStateParam(petController, deltaTime);
                return;
            }

            if (!petController.IsMoving() && _currentMovementState != null) {
                PlayAnimationState(State.Idle);
            }
            
            if (_currentAnimancerState is not { IsPlaying: true, NormalizedTime: < 1.0f }) {
                PlayAnimationState(State.Idle);
            }
        }

        public void PlayAnimationState(State state) {
            if (!_animationsLoaded) {
                _onAnimationsLoaded += () => PlayAnimationState(state);
                return;
            }
            
            var clip = _animations.GetAnimation(state);

            if (clip == null) {
                return;
            }
            
            _currentAnimancerState = Play(clip, clip.FadeDuration, FadeMode.FromStart);
            _currentMovementState = _currentAnimancerState as MixerState<Vector2>;
            CurrentState = state;
        }

        public void SyncAnimationWithState(AnimancerState clipState, State animatorState) {
            if (clipState is not { Clip: not null }) {
                return;
            }
            
            if (!_animationsLoaded) {
                _onAnimationsLoaded += () => SyncAnimationWithState(clipState, animatorState);
                return;
            }
            
            _currentAnimancerState = Play(clipState.Clip, 0.1f);
            _currentAnimancerState.Time = clipState.Time;
            _currentAnimancerState.Speed = clipState.Speed;
            _currentAnimancerState.NormalizedTime = clipState.NormalizedTime;
            
            _currentMovementState = _currentAnimancerState as MixerState<Vector2>;
            CurrentState = animatorState;
        }
        
        public void SyncAnimationWith(ARPetAnimancer other) {
            if (!other._animationsLoaded) {
                other._onAnimationsLoaded += () => SyncAnimationWith(other);
                return;
            }
            
            if (other._currentAnimancerState != null) {
                SyncAnimationWithState(other._currentAnimancerState, other.CurrentState);
            }
        }
        
        void UpdateMovementStateParam(VCPetController petController, float deltaTime) {
            if (_currentMovementState == null) {
                return;
            }
            
            float yParameter = petController.WalkingSpeed;
            float xParameter = petController.AngularVelocity;
            
            if (petController.WalkingSpeed > math.EPSILON) {
                Vector3 movementDirection = petController.WalkingVelocity.normalized;
                Vector3 lookDirection = petController.DirectionToTarget;
                xParameter = Vector2.SignedAngle(movementDirection.ToHorizontal2(), lookDirection.ToHorizontal2());
            }
            
            Vector2 desiredParameter = new(xParameter / DegreesPerMixerUnit, yParameter);
            Vector2 currentParameter = _currentMovementState.Parameter;
            float followSpeedDelta = _animations.movementMixerFollowSpeed * deltaTime;
            _currentMovementState.Parameter = Vector2.MoveTowards(currentParameter, desiredParameter, followSpeedDelta);
        }

        public void UnloadAnimations() {
            _currentAnimancerState = null;
            _currentMovementState = null;
            
            foreach (var layer in Layers) {
                layer.DestroyStates();
            }

            _animationsReference?.ReleaseAsset();
            _animationsReference = null;
            _animations = null;
            _animationsLoaded = false;
            _onAnimationsLoaded = null;
        }
        
        public enum State : byte {
            Idle = 0,
            Movement = 1,
            Taunt = 2,
            Pet = 3,
            Feed = 4,
            Transition = 5,
            TransitionLarge = 6,
        }
    }
}