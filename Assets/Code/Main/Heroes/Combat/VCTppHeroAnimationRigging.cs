using Animancer;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.Utility.Maths.Data;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UniversalProfiling;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCTppHeroAnimationRigging : ViewComponent<Hero> {
        public static readonly UniversalProfilerMarker RebindAnimatorMarker = new (Color.yellow, $"{nameof(VCTppHeroAnimationRigging)}.Animator.Rebind");

        const float RigUpdateSpeed = 2;
        
        [SerializeField] Rig rightHandRig;
        [SerializeField] Rig leftHandRig;
        
        DelayedValue _desiredRightHandRigWeight;
        DelayedValue _desiredLeftHandRigWeight;
        MagicMainHandFSM _magicMainHandFSM;
        MagicOffHandFSM _magicOffHandFSM;
        
        MagicMainHandFSM MagicMainHandFSM => Target.CachedElement(ref _magicMainHandFSM);
        MagicOffHandFSM MagicOffHandFSM => Target.CachedElement(ref _magicOffHandFSM);

        protected override void OnAttach() {
            _desiredRightHandRigWeight = new DelayedValue();
            _desiredRightHandRigWeight.SetInstant(0);
            _desiredLeftHandRigWeight = new DelayedValue();
            _desiredLeftHandRigWeight.SetInstant(0);

            Target.GetOrCreateTimeDependent()?.WithLateUpdate(OnUpdate);
        }

        void OnUpdate(float deltaTime) {
            bool isChaingunCastingMainHand =
                MagicMainHandFSM.CurrentAnimatorState?.GeneralType == HeroGeneralStateType.MagicCastHeavy
                && (MagicMainHandFSM.Item?.Template.IsChaingun ?? false);
            _desiredRightHandRigWeight.Set(isChaingunCastingMainHand ? 1 : 0);
            _desiredRightHandRigWeight.Update(deltaTime, RigUpdateSpeed);

            bool isChainGunCastingOffHand =
                MagicOffHandFSM.CurrentAnimatorState?.GeneralType == HeroGeneralStateType.MagicCastHeavy
                && (MagicOffHandFSM.Item?.Template.IsChaingun ?? false);
            _desiredLeftHandRigWeight.Set(isChainGunCastingOffHand ? 1 : 0);
            _desiredLeftHandRigWeight.Update(deltaTime, RigUpdateSpeed);
            
            UpdateRigWeights();
        }

        void UpdateRigWeights() {
            if (rightHandRig != null) {
                rightHandRig.weight = _desiredRightHandRigWeight.Value;
            }

            if (leftHandRig != null) {
                leftHandRig.weight = _desiredLeftHandRigWeight.Value;
            }
        }

        public void RebindAnimationRigging(Animator animator, AnimancerPlayable playable) {
            if (rightHandRig.weight == 0 && leftHandRig.weight == 0) {
                // --- Rigging not enabled so there is no need to rebind.
                return;
            }
            RebindAnimatorMarker.Begin();
            animator.Rebind();
            RebindAnimatorMarker.End();
            UpdateRigWeights();
            playable.Evaluate(0);
        }

        protected override void OnDiscard() {
            Target.GetTimeDependent()?.WithoutLateUpdate(OnUpdate);
        }
    }
}