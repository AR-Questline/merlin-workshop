using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public class ManualAudioZone : AudioBiome, IListenerOwner {
        [SerializeField] public Collider zone; // Could also maybe be a spline setup
        
        bool _wasPlayerWithinZone;
        
        void Start() {
            SceneLifetimeEvents lifetime = SceneLifetimeEvents.Get;
            if (lifetime.EverythingInitialized) {
                Init();
            } else {
                World.EventSystem.LimitedListenTo(lifetime.ID, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, _ => Init(), 1);
            }
        }

        void Init() {
            if (this == null) {
                return;
            }
            PlayerWithinZoneChanged();
            World.Services.Get<UnityUpdateProvider>().RegisterAudioZone(this);
        }

        public void UnityUpdate() {
            if (_wasPlayerWithinZone == IsPlayerWithinZone()) {
                return;
            }
            _wasPlayerWithinZone = !_wasPlayerWithinZone;
            PlayerWithinZoneChanged();
        }

        void PlayerWithinZoneChanged() {
            // Activate volume and audio biome
            if (_wasPlayerWithinZone) {
                ActivateBiome();
            } else {
                DeactivateBiome();
            }
        }

        bool IsPlayerWithinZone() {
            Hero hero = Hero.Current;
            return hero != null && ColliderUtil.IsPointWithinCollider(zone, hero.Coords);
        }

        void OnDisable() {
            _wasPlayerWithinZone = false;
            PlayerWithinZoneChanged();
        }
        
        void OnDestroy() {
            World.Services.TryGet<UnityUpdateProvider>()?.UnregisterAudioZone(this);
        }
    }
}