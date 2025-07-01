using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    [NoPrefab]
    public class VLockpickingAnimations : View<LockpickingAnimations> {
        const int MainLayer = 0;
        const int DamageShakeLayer = 1;
        static readonly int EmptyAnimationId = Animator.StringToHash("Empty");
        static readonly int StartAnimationId = Animator.StringToHash("Start");
        static readonly int PicklockBrokenAnimationId = Animator.StringToHash("PicklockBroken");
        static readonly int NoPicklockAnimationId = Animator.StringToHash("NoPicklock");
        static readonly int NextLevelAnimationId = Animator.StringToHash("NextLevel");
        static readonly int LockOpenedAnimationId = Animator.StringToHash("LockOpened");
        static readonly int PicklockDamagedAnimationId = Animator.StringToHash("PicklockDamaged");

        Animator _animator;

        public bool IsBlocked { get; private set; }

        public override Transform DetermineHost() => Target.AnimationsParent;

        protected override void OnMount() {
            _animator = GetComponentInParent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        void Update() {
            if (!IsBlocked) {
                return;
            }
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(MainLayer);
            if (stateInfo.shortNameHash == EmptyAnimationId || stateInfo.normalizedTime < 1) {
                return;
            }
            IsBlocked = false;
            _animator.Play(EmptyAnimationId, MainLayer);
        }

        public void PlayStartAnimation() {
            PlayAnimation(StartAnimationId);
        }

        public void PlayPicklockBrokenAnimation() {
            PlayAnimation(PicklockBrokenAnimationId);
        }

        public void PlayNoPicklockAnimation() {
            PlayAnimation(NoPicklockAnimationId);
        }

        public void PlayNextLevelAnimation() {
            PlayAnimation(NextLevelAnimationId);
        }

        public void PlayLockOpenedAnimation() {
            PlayAnimation(LockOpenedAnimationId);
        }

        public void PlayPicklockDamageAnimation() {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(DamageShakeLayer);
            if (stateInfo.shortNameHash == PicklockDamagedAnimationId) {
                return;
            }
            _animator.Play(PicklockDamagedAnimationId, DamageShakeLayer);
        }

        void PlayAnimation(int animation) {
            IsBlocked = true;
            _animator.Play(animation, MainLayer);
        }
    }
}
