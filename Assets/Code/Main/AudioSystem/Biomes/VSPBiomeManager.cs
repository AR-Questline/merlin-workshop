using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using AwesomeTechnologies.VegetationSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public class VSPBiomeManager : MonoBehaviour, IListenerOwner {
        static AudioCore AudioCore => World.Services.Get<AudioCore>();
#if DEBUG
        [SerializeField] bool logChange;
#endif

        [ShowInInspector] BiomeMaskArea _currentBiomeArea;
        [ShowInInspector] VSPAudioBiome _currentBiomeSounds;
        bool _isInitialized;
        public BiomeMaskArea CurrentBiomeArea => _currentBiomeArea;
        public BiomeType BiomeType => _currentBiomeArea ? _currentBiomeArea.BiomeType : BiomeType.Default;
        CameraHandle _camera;
        Vector3 CameraPosition => _camera.Camera.transform.position;
        
        void Awake() {
            ModelUtils.DoForFirstModelOfType<Hero>(Init, this);
        }

        void Init(Hero hero) {
            _camera = World.Only<CameraStateStack>().MainHandle;
            _isInitialized = true;
        }

        void OnDestroy() {
            World.EventSystem.RemoveAllListenersOwnedBy(this);
        }

        void Update() {
            if (!_isInitialized) return;

            // BiomeMaskArea biomeMaskArea = VegetationStudioManager.GetBiomeMaskArea(CameraPosition);
            // if (biomeMaskArea == null) return;
            // if (_currentBiomeArea == null || biomeMaskArea != _currentBiomeArea) {
            //     RemoveOldBiomeSounds();
            //     _currentBiomeArea = biomeMaskArea;
            //     _currentBiomeSounds = _currentBiomeArea.GetComponent<VSPAudioBiome>();
            //     UpdateBiomeSounds();
            // }
        }

        void UpdateBiomeSounds() {
#if DEBUG
            if (logChange) {
                Log.Important?.Info("Changed biome to: " + _currentBiomeArea.BiomeType, _currentBiomeArea);
            }
#endif
            if (_currentBiomeSounds != null) {
                var audioCore = AudioCore;
                audioCore.RegisterAudioSources(_currentBiomeSounds.GetAmbientSources, AudioType.Ambient);
                audioCore.RegisterAudioSources(_currentBiomeSounds.GetMusicSources, AudioType.Music);
                audioCore.RegisterAudioSources(_currentBiomeSounds.GetSnapshotSources, AudioType.Snapshot);
            }

            AudioCore.ForceRecalculateSoundPriority();
        }

        void RemoveOldBiomeSounds() {
            if (_currentBiomeSounds != null) {
                var audioCore = AudioCore;
                audioCore.UnregisterAudioSources(_currentBiomeSounds.GetAmbientSources, AudioType.Ambient);
                audioCore.UnregisterAudioSources(_currentBiomeSounds.GetMusicSources, AudioType.Music);
                audioCore.UnregisterAudioSources(_currentBiomeSounds.GetSnapshotSources, AudioType.Snapshot);
            }
        }
    }
}