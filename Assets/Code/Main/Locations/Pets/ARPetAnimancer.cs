using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
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
            if (result == null) {
                Log.Important?.Error("Failed to load base animations for Animancer! Pet will be broken!", gameObject);
                return;
            }

            if (this == null || Hero.Current.HasBeenDiscarded) {
                _animationsReference?.ReleaseAsset();
                _animationsReference = null;
                return;
            }
            
            _animations = result;
            _animationsLoaded = true;
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
                    PlayMovementAnimation();
                }
                UpdateMovementStateParam(petController, deltaTime);
                return;
            }

            if (!petController.IsMoving() && _currentMovementState != null) {
                PlayIdleAnimation();
            }
            
            if (_currentAnimancerState is not { NormalizedTime: < 1.0f } || !IsPlaying()) {
                PlayIdleAnimation();
            }
        }
        
        public void PlayTauntAnimation() {
            PlayAnimation(RandomUtil.UniformSelect(_animations.tauntClips));
            CurrentState = State.Taunt;
        }

        public void PlayPetAnimation() {
            PlayAnimation(_animations.petClip);
            CurrentState = State.Pet;
        }

        void PlayMovementAnimation() {
            PlayAnimation(_animations.movementMixer);
            CurrentState = State.Movement;
        }

        void PlayIdleAnimation() {
            PlayAnimation(RandomUtil.UniformSelect(_animations.idleClips));
            CurrentState = State.Idle;
        }

        void PlayAnimation(ITransition clip) {
            _currentAnimancerState = Play(clip, clip.FadeDuration);
            _currentMovementState = _currentAnimancerState as MixerState<Vector2>;
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
        }

        public enum State : byte {
            Idle,
            Movement,
            Taunt,
            Pet,
        }
    }
}