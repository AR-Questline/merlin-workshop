using UnityEngine;

namespace Awaken.Utility.Animations {
    [RequireComponent(typeof(Animator))]
    public class BowAnimatorSynchronize : MonoBehaviour {
        public bool CanTransitionToIdle => _bowState != BowPullState.None && _bowState != BowPullState.Pull;
        Animator _animator;
        BowPullState _bowState;
        static readonly int Idle = Animator.StringToHash("Idle");
        static readonly int Pull = Animator.StringToHash("Pull");
        static readonly int Hold = Animator.StringToHash("Hold");
        static readonly int Release = Animator.StringToHash("Release");
        static readonly int Equip = Animator.StringToHash("Equip");
        static readonly int UnEquip = Animator.StringToHash("UnEquip");
        static readonly int Cancel = Animator.StringToHash("Cancel");
        static readonly int BowDrawSpeed = Animator.StringToHash("BowDrawSpeed");

        void Awake() {
            _animator = GetComponent<Animator>();
            _bowState = BowPullState.Idle;
        }

        public void NoneState() {
            _bowState = BowPullState.None;
        }

        public void OnPullBow(float? bowDrawSpeed = null) {
            if (bowDrawSpeed.HasValue) {
                SetBowDrawSpeed(bowDrawSpeed.Value);
            }
            _bowState = BowPullState.Pull;
            _animator.CrossFade(Pull, 0.01f);
        }

        public void SetBowDrawSpeed(float value) {
            _animator.SetFloat(BowDrawSpeed, value);
        }

        public void OnHoldBow() {
            _bowState = BowPullState.Hold;
            _animator.CrossFade(Hold, 0.01f);
        }

        public void OnReleaseBow() {
            _bowState = BowPullState.Release;
            _animator.CrossFade(Release, 0.01f);
        }

        public void OnBowIdle() {
            _bowState = BowPullState.Idle;
            _animator.CrossFade(Idle, 0.01f);
        }

        public void OnEquipBow() {
            _bowState = BowPullState.Equip;
            _animator.CrossFade(Equip, 0.01f);
        }
        
        public void OnUnEquipBow() {
            _bowState = BowPullState.UnEquip;
            _animator.CrossFade(UnEquip, 0.01f);
        }

        public void OnBowCancel(float normalizedTimeOffset) {
            _bowState = BowPullState.Cancel;
            _animator.CrossFade(Cancel, 0.01f, -1, normalizedTimeOffset);
        }

        public void UpdateAnimatorParam(int param, float value) {
            _animator.SetFloat(param, value);
        }

        public enum BowPullState {
            Idle = 0,
            Pull = 1,
            Hold = 2,
            Release = 3,
            None = 4,
            Equip = 5,
            Cancel = 6,
            UnEquip = 7,
        }
    }
}
