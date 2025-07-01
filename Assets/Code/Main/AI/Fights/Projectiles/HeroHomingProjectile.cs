using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Relations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class HeroHomingProjectile : HomingProjectile {
        bool _wasUsingGravity;
        bool _lockedIntoTarget;
        bool _canSetTarget;
        
        protected override void OnSetup(Transform firePoint) {
            base.OnSetup(firePoint);
            AttachToHeroAndFlyUp().Forget();
        }

        async UniTaskVoid AttachToHeroAndFlyUp() {
            _canSetTarget = false;
            _wasUsingGravity = _rb.useGravity;
            _rb.useGravity = false;
            _rb.isKinematic = true;
            transform.SetParent(Hero.Current.VHeroController.FirePoint, false);
            transform.localPosition = new Vector3(0, 0, 2);
            
            if (!await AsyncUtil.DelayFrame(gameObject)) {
                return;
            }
            
            _rb.isKinematic = false;
            _rb.AddForce(Vector3.up + Vector3.right * RandomUtil.UniformFloat(-1f, 1f), ForceMode.Impulse);
            
            if (await AsyncUtil.DelayTime(gameObject, 0.5f)) {
                _rb.isKinematic = true;
                _canSetTarget = true;
                FindTarget();
            }
        }

        void FindTarget() {
            TrySearchForNewTarget();
            
            if (HomingCharacterTarget == null) {
                owner?.ListenTo(AITargetingUtils.Relations.IsTargetedBy.Events.Changed, OnCombatTargetingChanged, this);
            }
        }

        void OnCombatTargetingChanged(RelationEventData data) {
            if (data is { newState: true, to: NpcElement npc }) {
                SetTarget(npc);
            }
        }

        public override void SetTarget(ICharacter target, float aimAtHeight = 0.5f) {
            if (!_canSetTarget) {
                return;
            }
            
            base.SetTarget(target, aimAtHeight);
            
            if (target != null) {
                _rb.useGravity = _wasUsingGravity;
                _rb.isKinematic = false;
                _lockedIntoTarget = true;
                transform.SetParent(null, true);
            }
        }

        protected override void ProcessUpdate(float deltaTime) {
            if (!_lockedIntoTarget) {
                return;
            }
            base.ProcessUpdate(deltaTime);
        }
    }
}