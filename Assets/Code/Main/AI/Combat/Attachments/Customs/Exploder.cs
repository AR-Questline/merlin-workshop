using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    public partial class Exploder : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Exploder;

        //StatTweak _movementSpeedTweak;
        bool _exploded;
        
        // === Events
        public new static class Events {
            public static readonly Event<EnemyBaseClass, bool> ExplosionExploded = new(nameof(ExplosionExploded));
        }
        
        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            NpcElement.HealthElement.ListenTo(HealthElement.Events.BeforeTakenFinalDamage, OnTakingDamage, this);
            this.ListenTo(Events.ExplosionExploded, _ => _exploded = true, this);
            EquipWeapons(false, out _);
        }

        protected override void OnEnterCombat() { }
        protected override void OnExitCombat() { }
        
        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (_exploded) {
                return;
            }
            
            bool isExploding = CurrentBehaviour.Get() is ExplodeBehaviour;
            bool weakSpotHit = hook.Value.WeakSpotHit;
            bool hitWillKillButNotInWeakSpot = !weakSpotHit && hook.Model.Health.ModifiedValue - hook.Value.RawData.CalculatedValue <= 0f;
            if (weakSpotHit && hook.Model.Health.ModifiedValue - hook.Value.Amount <= 0f) {
                hook.Prevent();
                TryGetElement<ExplodeBehaviour>()?.Explode();
            } else if (isExploding) {
                hook.Prevent();
            } else if (hitWillKillButNotInWeakSpot) {
                hook.Prevent();
                TryStartBehaviour<ExplodeBehaviour>();
            }
        }
    }
}
