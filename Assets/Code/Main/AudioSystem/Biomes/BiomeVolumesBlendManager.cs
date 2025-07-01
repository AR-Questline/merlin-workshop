using Awaken.TG.Main.AudioSystem.Biomes;
using AwesomeTechnologies.VegetationSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Biomes {
    public class BiomeVolumesBlendManager : MonoBehaviour {
        [SerializeField, Required] VSPBiomeManager _biomeManager;
        [SerializeField, Min(0)] float _transitionTime = 0.5f;
        [SerializeField,
         Tooltip("Controls for how much time player should stay in the biome to start transition from previous volume to the new biome volume. Should be greater or equal to transition time to ensure that only two volumes are active at one time")]
        public float _delayToChangeBiome = 0.5f;
#if UNITY_EDITOR
        [ShowInInspector, Tooltip("Allows to test Volumes' transitions by assigning biome directly in editor")]
        bool _testTransitionsMode;

        [ShowInInspector, ShowIf(nameof(_testTransitionsMode))]
        BiomeMaskArea _debugForceSelectedBiome;
#endif
        Volume _prevActiveBiomeVolume;
        Volume _activeBiomeVolume;
        BiomeMaskArea _activeBiome;
        BiomeMaskArea _waitingForTransitionBiome;
        float _activeBiomeTransitionElapsedTime;
        float _waitingForTransitionBiomeStayTime;

        void Start() {
            Reinitialize();
        }

        void Update() {
            BiomeMaskArea thisFrameBiome = GetCurrentBiome();
            if (thisFrameBiome == _activeBiome) {
                if (_activeBiomeTransitionElapsedTime > _transitionTime) {
                    return;
                }
            } else {
                if (thisFrameBiome != _waitingForTransitionBiome) {
                    _waitingForTransitionBiomeStayTime = 0;
                    _waitingForTransitionBiome = thisFrameBiome;
                }
                _waitingForTransitionBiomeStayTime += Time.deltaTime;
                if (_waitingForTransitionBiomeStayTime < _delayToChangeBiome) {
                    return;
                }
                if (_prevActiveBiomeVolume != null) {
                    _prevActiveBiomeVolume.weight = 0;
                }
                _prevActiveBiomeVolume = _activeBiomeVolume;
                _activeBiome = _waitingForTransitionBiome;
                _activeBiomeVolume = _activeBiome == null ? null : _activeBiome.GetComponent<Volume>();
                _activeBiomeTransitionElapsedTime = 0;
            }

            _activeBiomeTransitionElapsedTime += Time.deltaTime;
            var activeBiomeVolumeWeight = _transitionTime <= 0
                ? 1
                : Mathf.Min(_activeBiomeTransitionElapsedTime / _transitionTime, 1);
            BlendVolumes(_prevActiveBiomeVolume, _activeBiomeVolume, activeBiomeVolumeWeight);
        }

        void Reinitialize() {
            if (_prevActiveBiomeVolume != null) {
                _prevActiveBiomeVolume.weight = 0;
            }
            _prevActiveBiomeVolume = null;
            _activeBiomeVolume = null;
            _activeBiome = null;
            _activeBiomeTransitionElapsedTime = _transitionTime;
            _waitingForTransitionBiome = null;
            _waitingForTransitionBiomeStayTime = 0;
        }

        static void BlendVolumes(Volume prevVolume, Volume newVolume, float newVolumeWeight) {
            if (newVolume != null) {
                newVolume.priority = 2;
                newVolume.weight = newVolumeWeight;
                if (prevVolume != null) {
                    prevVolume.priority = 1;
                    prevVolume.weight = 1;
                }
                
            } else if (prevVolume != null) {
                prevVolume.priority = 1;
                prevVolume.weight = 1 - newVolumeWeight;
            }
        }

        BiomeMaskArea GetCurrentBiome() {
#if UNITY_EDITOR
            return _testTransitionsMode ? _debugForceSelectedBiome : _biomeManager.CurrentBiomeArea;
#else
            return _biomeManager.CurrentBiomeArea;
#endif
        }
        
#if UNITY_EDITOR
        void OnValidate() {
            ValidateDelayToChangeBiome();
        }
        void ValidateDelayToChangeBiome() {
            if (_delayToChangeBiome < _transitionTime) {
                _delayToChangeBiome = _transitionTime;
            }
        }
#endif
    }
}