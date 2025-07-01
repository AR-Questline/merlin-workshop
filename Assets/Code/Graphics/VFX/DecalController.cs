using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    public class DecalController : MonoBehaviour, IVFXOnStopEffects {
        /// <summary>
        /// If Enabled will cause changes to scene that should be discarded
        /// </summary>
        public static bool EditorPreviewUpdates { get; set; }
        
        [SerializeField] AnimationCurve fadeCurve;
        [SerializeField] float animationDuration = 1.0f;
        [SerializeField] bool loop;
        [SerializeField, ShowIf(nameof(loop))] bool pingPong;
        [SerializeField] bool listenForVFXStopped = true;
        [SerializeField, ShowIf(nameof(listenForVFXStopped))] bool reverseStopEffect = true;
        [SerializeField] bool resetReverseOnEnable;

        DecalProjector _decalProjector;
        float _effectTime;
        bool _reverse;

        void Awake() {
            _decalProjector = GetComponent<DecalProjector>();
        }

        void Start() {
            _effectTime = 0f;
        }
        
        void OnEnable() {
            if (resetReverseOnEnable) {
                _reverse = false;
            }
            
            _effectTime = 0f;
        }

        void LateUpdate() {
            if (!Application.isPlaying && !EditorPreviewUpdates) return;
            if (_effectTime > animationDuration) return;
            
            _effectTime += Time.deltaTime;
            float timePercentage = _effectTime / animationDuration;
            if (timePercentage >= 1.0f)
                if (loop) {
                    if (pingPong) {
                        _reverse = !_reverse;
                    }
                    _effectTime = 0f;
                    timePercentage = 0f;
                } else {
                    timePercentage = 1.0f;
                }

            float fadeFactor = _reverse ? fadeCurve.Evaluate(1f - timePercentage) : fadeCurve.Evaluate(timePercentage);
            _decalProjector.fadeFactor = fadeFactor;
        }

        public void VFXStopped() {
            if (!listenForVFXStopped) {
                return;
            }
            
            if (reverseStopEffect) {
                _reverse = true;
            }

            _effectTime = 0f;
        }
    }
}
