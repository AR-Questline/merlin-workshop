using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class ProximityEggSpawner : Element<Location>, IRefreshedByAttachment<ProximityEggSpawnerAttachment> {
        const int TrySpawnOnBand = 1;
        const float TimeDelayBeforeValidCheck = 0.5f;
        
        public override ushort TypeForSerialization => SavedModels.ProximityEggSpawner;

        ProximityEggSpawnerAttachment _spec;
        float _testBandAfterTime;
        
        public void InitFromAttachment(ProximityEggSpawnerAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
            Hero.Current.ListenTo(Hero.Events.HeroLongTeleported, SetTimeDelay, this);
        }
        
        void OnVisualLoaded(Transform transform) {
            SetTimeDelay();
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChange, this);
        }

        void SetTimeDelay() {
            _testBandAfterTime = Time.time + TimeDelayBeforeValidCheck;
        }

        void OnBeforeDeath(DamageOutcome outcome) {
            Trigger(RandomUtil.WithProbability(_spec.chanceToSpawnOnDeath), RandomUtil.WithProbability(_spec.chanceToSpawnedOnesToBeKilledOnDeath), false);
        }

        void OnDistanceBandChange(int band) {
            if (band > TrySpawnOnBand || Time.time < _testBandAfterTime) {
                return;
            }

            TryTrigger(_spec.chanceToTriggerOnBandChange, _spec.chanceToSpawnOnBandChange, false, true);
        }

        void TryTrigger(float changeToTrigger, float chanceToSpawn, bool killLocationAfterSpawn, bool killSelf) {
            if (RandomUtil.WithProbability(changeToTrigger)) {
                Trigger(RandomUtil.WithProbability(chanceToSpawn), killLocationAfterSpawn, killSelf);
            }
        }

        void Trigger(bool spawn, bool killSpawnedLocation, bool killSelf) {
            var location = ParentModel;
            Discard();
            if (spawn) {
                var spawner = location.TryGetElement<BaseLocationSpawner>();
                if (spawner != null) {
                    if (killSpawnedLocation) {
                        spawner.ListenToLimited(BaseLocationSpawner.Events.LocationSpawned, OnLocationSpawned, spawner);
                    }
                    spawner.TryGetElement<ManualSpawner>()?.TriggerSpawner().Forget();
                }
            }

            if (killSelf) {
                location.TryGetElement<IAlive>()?.Kill();
            }
        }
        
        void OnLocationSpawned(Location spawnedLocation) {
            spawnedLocation.Kill();
        }
    }
}