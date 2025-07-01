using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroPushForce : Element<Hero> {
        const float BaseForceModifier = 0.00025f;
        const float ForceMultiplierWhenNotGrounded = 0.25f;
        const float ForceDuration = 0.3f;
        const int EnableCameraShakeAtForceAbove = 20;

        public sealed override bool IsNotSaved => true;

        Force _pushForce;
        
        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(AfterFullyInitialized);
        }

        void AfterFullyInitialized() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
        }

        void ProcessUpdate(float deltaTime) {
            if (_pushForce == null || _pushForce.duration <= 0) {
                return;
            }

            _pushForce.duration -= deltaTime;
            PushHero();
        }

        void PushHero() {
            float magnitude = Mathf.Log((ForceDuration - _pushForce.duration) + 0.1f, 0.6f) * 0.75f - 0.5f;
            magnitude = Mathf.Clamp(magnitude, 0.25f, magnitude);
            ParentModel.VHeroController?.MoveTowards(_pushForce.direction * magnitude);
        }
        
        void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.Damage.ForceDirection == null) {
                return;
            }

            float ragdollForce = damageOutcome.Damage.RagdollForce;
            if (damageOutcome.Damage.DamageDealer is NpcElement npc) {
                ragdollForce = npc.NpcStats.HeroKnockBack;
            }
            
            Vector3 direction = damageOutcome.Damage.ForceDirection.Value;
            bool isControllerGrounded = ParentModel.VHeroController.Controller.isGrounded;
            float strength = ragdollForce * BaseForceModifier * (isControllerGrounded ? 1 : ForceMultiplierWhenNotGrounded);
            _pushForce = new Force(direction * strength, ForceDuration);
            if (strength > EnableCameraShakeAtForceAbove) {
                ParentModel.Element<HeroCameraShakes>().CustomShake(false, 10, 10).Forget();
            }
            PushHero();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
        }
    }
}
