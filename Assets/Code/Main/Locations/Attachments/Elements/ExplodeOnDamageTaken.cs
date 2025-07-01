using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class ExplodeOnDamageTaken : Element<Location>, IRefreshedByAttachment<ExplodeOnDamageTakenAttachment> {
        public override ushort TypeForSerialization => SavedModels.ExplodeOnDamageTaken;
        
        [Saved] bool _waitingForExplosion;
        [Saved] bool _hasExploded;
        ExplodeOnDamageTakenAttachment _spec;

        public void InitFromAttachment(ExplodeOnDamageTakenAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(Init, this);
        }

        protected override void OnRestore() {
            ParentModel.AfterFullyInitialized(Init, this);
            if (_waitingForExplosion) {
                WaitForExplosion().Forget();
            } else if (_hasExploded) {
                Exploding().Forget();
            }
        }

        void Init() {
            if (!ParentModel.TryGetElement<IAlive>(out var alive) || alive is not {IsInitialized: true, HasBeenDiscarded: false, IsAlive: true }) {
                return;
            }
            alive.HealthElement?.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
        }

        void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.FinalAmount <= 0 || !damageOutcome.Damage.IsPrimary) {
                return;
            }
            
            if (_waitingForExplosion || _spec.DelayExplosion <= 0f) {
                Exploding();
            } else {
                WaitForExplosion().Forget();
            }
        }

        async UniTaskVoid WaitForExplosion() {
            _waitingForExplosion = true;
            if (!await AsyncUtil.DelayTime(this, _spec.DelayExplosion)) {
                return;
            }
            Exploding();
        }
        
        async UniTaskVoid Exploding() {
            _waitingForExplosion = false;
            _hasExploded = true;
            if (_spec.ExplodeVFXReference is { IsSet: true }) {
                await PrefabPool.InstantiateAndReturn(_spec.ExplodeVFXReference, ParentModel.Coords, ParentModel.Rotation);
                if (HasBeenDiscarded) {
                    return;
                }
            }
            ParentModel.AddElement(new DealDamageInSphereOverTime(_spec.SphereDamageParams, ParentModel.Coords, ParentModel.TryGetElement<ICharacter>()));
            if (_spec.SphereDamageParams.duration > 0) {
                if (!await AsyncUtil.DelayTime(this, _spec.SphereDamageParams.duration)) {
                    return;
                }
            }
            _hasExploded = false;
            AfterExplode();
        }

        void AfterExplode() {
            if (_spec.KillOnExplosion && ParentModel.TryGetElement<IAlive>(out var alive)) {
                alive.Kill();
            }
            if (_spec.DiscardElementOnExplosion && !HasBeenDiscarded) {
                Discard();
            }
        }
    }
}