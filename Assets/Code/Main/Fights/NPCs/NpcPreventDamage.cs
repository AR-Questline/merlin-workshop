using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcPreventDamage : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        bool _showEnviroVfx;
        
        public NpcPreventDamage(bool showEnviroVfx) { 
            _showEnviroVfx = showEnviroVfx;
        }

        public NpcPreventDamage() {
            _showEnviroVfx = false;
        }

        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
        }
        
        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            hook.Prevent();
            if (_showEnviroVfx) {
                var damage = hook.Value;
                var hitPoint =  damage.Position ?? damage.HitCollider?.transform.position;
                if (!hitPoint.HasValue || damage.DamageDealer == null) {
                    return;
                }
                var rbHit = damage.HitCollider != null ? damage.HitCollider.GetComponentInParent<Rigidbody>() : null;
                EnvironmentHitData hitData = new() { Location = ParentModel.ParentModel.LocationView, Item = damage.Item, Position = hitPoint.Value, Direction = damage.Direction ?? Vector3.zero, Rigidbody = rbHit, RagdollForce = 0};
                damage.DamageDealer.Trigger(ICharacter.Events.HitEnvironment, hitData);
            }
        }
    }
}