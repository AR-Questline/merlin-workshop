using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroRagdollElement : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        DeathRagdollHeroBehaviour _behaviour;

        protected override void OnInitialize() {
            this.ListenTo(Events.AfterFullyInitialized, Init, this);
        }

        void Init() {
            _behaviour = new DeathRagdollHeroBehaviour();
            _behaviour.CacheRigidBody(ParentModel);
        }

        public void OnDeath(DamageOutcome damageOutcome) {
            _behaviour.OnDeath(damageOutcome);
        }

        public void OnRevive() {
            _behaviour.DisableRagdoll();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _behaviour = null;
            base.OnDiscard(fromDomainDrop);
        }
    }
}