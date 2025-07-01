using System.Collections.Generic;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public class CustomDeathAnimations : PostponedRagdollBehaviourBase, IDeathAnimationProvider {
        [SerializeField] bool useRagdollIfNoCustomDeathAnimationFound = true;
        [SerializeField] List<CustomDeathAnimation> customDeathAnimations = new();

        bool _useCustomDeathAnimation;
        CustomDeathAnimation _currentDeathAnimation;
        
        public override bool UseDeathAnimation => _useCustomDeathAnimation;
        public override NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Custom;
        protected override RagdollEnableData RagdollData => (_currentDeathAnimation?.UseCustomRagdollData ?? false) ? _currentDeathAnimation.CustomRagdollData : base.RagdollData;
        public bool CanPlayAnimationAfterLoad => !useRagdollIfNoCustomDeathAnimationFound;

        public override void OnDeath(DamageOutcome damageOutcome, Location dyingLocation) {
            _useCustomDeathAnimation = CheckDeathAnimations(damageOutcome);
            if (_useCustomDeathAnimation) {
                base.OnDeath(damageOutcome, dyingLocation);
            } else if (useRagdollIfNoCustomDeathAnimationFound) {
                _ragdollDeathBehaviour?.EnableDeathRagdoll(damageOutcome);
            }
        }

        public void AddCustomDeathAnimation(CustomDeathAnimation animation) {
            customDeathAnimations.Add(animation);
        }

        protected override void OnRagdollEnabled() {
            _currentDeathAnimation?.UnloadAndClear();
        }
        
        bool CheckDeathAnimations(DamageOutcome damageOutcome) {
            foreach (var deathAnimation in customDeathAnimations) {
                if (deathAnimation.CheckConditions(damageOutcome)) {
                    _currentDeathAnimation = deathAnimation;
                    LoadAnimations(deathAnimation).Forget();
                    return true;
                }
            }
            return false;
        }

        async UniTaskVoid LoadAnimations(CustomDeathAnimation deathAnimation) {
            var animancer = gameObject.GetComponentInChildren<ARNpcAnimancer>(true);
            deathAnimation.Load(animancer);
            while (!await AsyncUtil.WaitUntil(gameObject, deathAnimation.CheckIfLoaded)) {
                return;
            }
            deathAnimation.Apply();
            animancer.ForceGetAnimancerNode(NpcStateType.CustomDeath, OnNodeLoaded, null);
            
            void OnNodeLoaded(ITransition transition) {
                if (animancer != null) {
                    animancer.Play(transition, 0);
                    animancer.Evaluate();
                }
            }
        }

        void OnDestroy() {
            _currentDeathAnimation?.UnloadAndClear();
        }
    }
}
