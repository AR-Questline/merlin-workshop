using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;
// ReSharper disable UseArrayEmptyMethod
// ReSharper disable CoVariantArrayConversion

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public class WyrdnessAudioProvider : MonoBehaviour, IListenerOwner {
        [InfoBox("Activates when player enters wyrdness.")]
        [SerializeField] public WyrdnessAudioSource[] musicToRegister = new WyrdnessAudioSource[0];
        [SerializeField] public WyrdnessAudioSource[] alertMusicToRegister = new WyrdnessAudioSource[0];
        [SerializeField] public WyrdnessAudioSource[] combatMusicToRegister = new WyrdnessAudioSource[0];
        [SerializeField] public WyrdnessAudioSource[] ambientsToRegister = new WyrdnessAudioSource[0];
        [SerializeField] public WyrdnessAudioSource[] snapshotsToRegister = new WyrdnessAudioSource[0];

        bool _initialized;
        bool _wasPlayerWithinZone;
        
        void Start() {
            SceneLifetimeEvents lifetime = SceneLifetimeEvents.Get;
            if (lifetime.EverythingInitialized) {
                Init();
            } else {
                World.EventSystem.LimitedListenTo(lifetime.ID, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, this, _ => Init(), 1);
            }
        }

        void Init() {
            _wasPlayerWithinZone = IsPlayerWithinZone();
            PlayerWithinZoneChanged();
            _initialized = true;
        }

        void Update() {
            if (!_initialized) {
                return;
            }
            
            if (_wasPlayerWithinZone == IsPlayerWithinZone()) {
                return;
            }
            
            _wasPlayerWithinZone = !_wasPlayerWithinZone;
            PlayerWithinZoneChanged();
        }

        void OnDestroy() {
            _initialized = false;
            DeactivateBiome();
        }

        void PlayerWithinZoneChanged() {
            // Activate volume and audio biome
            if (_wasPlayerWithinZone) {
                ActivateBiome();
            } else {
                DeactivateBiome();
            }
        }
        
        void ActivateBiome() {
            var audioCore = World.Services.Get<AudioCore>();
            audioCore.RegisterAudioSources(musicToRegister, AudioType.Music);
            audioCore.RegisterAudioSources(alertMusicToRegister, AudioType.MusicAlert, false);
            audioCore.RegisterAudioSources(combatMusicToRegister, AudioType.MusicCombat, false);
            audioCore.RegisterAudioSources(ambientsToRegister, AudioType.Ambient);
            audioCore.RegisterAudioSources(snapshotsToRegister, AudioType.Snapshot);
        }
        
        void DeactivateBiome() {
            var audioCore = World.Services.Get<AudioCore>();
            audioCore.UnregisterAudioSources(musicToRegister, AudioType.Music);
            audioCore.UnregisterAudioSources(alertMusicToRegister, AudioType.MusicAlert);
            audioCore.UnregisterAudioSources(combatMusicToRegister, AudioType.MusicCombat);
            audioCore.UnregisterAudioSources(ambientsToRegister, AudioType.Ambient);
            audioCore.UnregisterAudioSources(snapshotsToRegister, AudioType.Snapshot);
        }
        
        static bool IsPlayerWithinZone() {
            Hero hero = Hero.Current;
            return hero is { IsFullyInitialized: true } && hero.HeroWyrdNight.IsHeroInWyrdness;
        }
    }
}