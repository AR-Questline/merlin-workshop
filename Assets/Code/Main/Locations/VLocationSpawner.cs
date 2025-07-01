using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    [NoPrefab]
    public class VLocationSpawner : View<BaseLocationSpawner> {
        public const float MaxSpawnRangeSq = MaxSpawnRange * MaxSpawnRange;
        const float MaxSpawnRange = 125;
        
        public override Transform DetermineHost() => Target.ParentModel.MainView.transform;

        protected override void OnMount() {
            if (isActiveAndEnabled) {
                Services.Get<UnityUpdateProvider>().RegisterLocationSpawner(Target);
            }
        }

        void OnEnable() {
            if (Target != null) {
                Services.Get<UnityUpdateProvider>().RegisterLocationSpawner(Target);
            }
        }

        void OnDisable() {
            if (!HasBeenDiscarded) {
                Services.TryGet<UnityUpdateProvider>()?.UnregisterLocationSpawner(Target);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            Services.TryGet<UnityUpdateProvider>()?.UnregisterLocationSpawner(Target);
            return base.OnDiscard();
        }
    }
}