using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteInEditMode, RequireComponent(typeof(VisualEffect))]
    public class VFXGraphPlayRateController : MonoBehaviour {
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly] int _particleCount;
#endif
        VisualEffect _visualEffect;
        [SerializeField] float playRate = 1.0f;
        
        [ButtonGroup("A"), Button("STOP")]
        void Stop() {
            playRate = 0.0f;
        }
        [ButtonGroup("A"), Button("1.0")]
        void Reset() {
            playRate = 1.0f;
        }
        [ButtonGroup("B"), Button("-10.0")]
        void A() {
            playRate -= 10.0f;
        }

        [ButtonGroup("B"), Button("-1.0")]
        void B() {
            playRate -= 1.0f;
        }
        
        [ButtonGroup("B"), Button("-0.1")]
        void C() {
            playRate -= 0.1f;
        }

        [ButtonGroup("B"), Button("-0.01")]
        void D() {
            playRate -= 0.01f;
        }

        [ButtonGroup("B"), Button("+0.01")]
        void E() {
            playRate += 0.01f;
        }

        [ButtonGroup("B"), Button("+0.1")]
        void F() {
            playRate += 0.1f;
        }
        
        [ButtonGroup("B"), Button("+1.0")]
        void G() {
            playRate += 1.0f;
        }
        
        [ButtonGroup("B"), Button("+10.0")]
        void H() {
            playRate += 10.0f;
        }

        void Awake() {
            _visualEffect = GetComponent<VisualEffect>();
            if (!_visualEffect) {
                Log.Important?.Warning($"{gameObject.name} has no VisualEffect attached");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
                GameObjects.DestroySafely(this);
                return;
            }
            _visualEffect.playRate = playRate;
        }

#if UNITY_EDITOR
        void Update() {
            if (_visualEffect == null) { return; }
            
            _particleCount = _visualEffect.aliveParticleCount;
            _visualEffect.playRate = playRate;
        }
#endif
    }
}