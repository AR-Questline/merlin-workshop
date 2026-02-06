using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public class HitboxDestroyable : ViewComponent<Location>, IHitboxMultiplier {
        [Range(0, 100), SerializeField] float destroyWhenPercentOfMaxHpConsumed = 25;
        [SerializeField] bool explodeOnOwnerDeath = true;
        [SerializeField] bool enterPoiseBreakOnExplode;
        [SerializeField, ShowIf(nameof(enterPoiseBreakOnExplode))] NpcStateType poiseBreakDirection = NpcStateType.PoiseBreakBack;
        [SerializeField] GameObject objectToDestroy;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)] 
        ShareableARAssetReference destroyVFX;
        
        float _destroyWhenDamageConsumedAbove;
        float _damageConsumed;
        
        public bool Destroyed { get; private set; }
        
        protected override void OnAttach() {
            IAlive alive = Target.TryGetElement<IAlive>();
            if (alive is null) {
                Log.Minor?.Error("Hitbox attached to location that is not Alive! This is invalid!", gameObject);
                return;
            }

            float maxHp = alive.AliveStats.MaxHealth.ModifiedValue;
            _destroyWhenDamageConsumedAbove = maxHp * destroyWhenPercentOfMaxHpConsumed / 100f;
            
            alive.Element<HealthElement>().ListenTo(HealthElement.Events.OnDamageTaken, OnHitBoxDamageTaken, this);
            if (explodeOnOwnerDeath) {
                alive.ListenTo(IAlive.Events.BeforeDeath, BeforeAliveDeath, this);
            }
        }

        void OnHitBoxDamageTaken(DamageOutcome damageOutcome) {
            if (Destroyed) {
                return;
            }

            var colliderHit = damageOutcome.HitCollider;
            if (colliderHit == null || colliderHit.gameObject != gameObject) {
                return;
            }
            
            _damageConsumed += damageOutcome.FinalAmount;
            if (_damageConsumed >= _destroyWhenDamageConsumedAbove) {
                Explode();
            }
        }

        void BeforeAliveDeath() {
            if (Destroyed) {
                return;
            }

            Explode();
        }

        void Explode() {
            Destroy(objectToDestroy);
            if (destroyVFX is {IsSet: true}) {
                Transform myTransform = transform;
                PrefabPool.InstantiateAndReturn(destroyVFX, myTransform.position, myTransform.rotation).Forget();
            }

            if (enterPoiseBreakOnExplode && Target.TryGetElement(out EnemyBaseClass enemy) &&
                enemy.HasElement<PoiseBreakBehaviour>()) {
                enemy.EnterPoise(poiseBreakDirection, enemy.NpcAI?.InCombat ?? false);
            }

            Destroyed = true;
        }
    }
}
