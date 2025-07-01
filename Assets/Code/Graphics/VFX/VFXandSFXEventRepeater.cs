using Awaken.TG.Main.General;
using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class VFXandSFXEventRepeater : MonoBehaviour {
        [SerializeField] string vfxEventName = "OnPlay";
        [SerializeField] FloatRange interval;
        
        VisualEffect[] _vfx;
        StudioEventEmitter[] _sfx;
        float _nextEventTime;

        void Awake() {
            _vfx = GetComponentsInChildren<VisualEffect>(true);
            _sfx = GetComponentsInChildren<StudioEventEmitter>(true);
        }
        
        void OnEnable() {
            _nextEventTime = Time.time + interval.RandomPick();
        }

        void Update() {
            if (Time.time < _nextEventTime) {
                return;
            }
            _nextEventTime = Time.time + interval.RandomPick();
            Play();
        }

        void Play() {
            foreach (var vfx in _vfx) {
                if (vfx != null && vfx.gameObject.activeInHierarchy) {
                    vfx.SendEvent(vfxEventName);
                }
            }
            foreach (var sfx in _sfx) {
                if (sfx != null && sfx.gameObject.activeInHierarchy) {
                    //sfx.Play();
                }
            }
        }
    }
}
