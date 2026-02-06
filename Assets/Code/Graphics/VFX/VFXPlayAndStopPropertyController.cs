using System;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class VFXPlayAndStopPropertyController : MonoBehaviour, IVFXOnPlayEffects, IVFXOnStopEffects {
        [SerializeField] FloatProperty[] floatProperties = Array.Empty<FloatProperty>();
        [SerializeField] float blendDuration = 0.5f;
        CancellationTokenSource _cts;
        
        public void VFXPlayed() {
            if (TryGetComponent(out VisualEffect vfx)) {
                BlendPlay(vfx).Forget();
            }
        }
        public void VFXStopped() {
            if (TryGetComponent(out VisualEffect vfx)) {
                BlendStop(vfx).Forget();
            }
        }

        async UniTaskVoid BlendPlay(VisualEffect vfx) {
            float duration = 0;
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            while (duration < blendDuration) {
                duration += Time.deltaTime;
                float t = duration / blendDuration;
                foreach (var prop in floatProperties) {
                    float value = Mathf.Lerp(prop.stopValue, prop.playValue, t);
                    vfx.SetFloat(prop.name, value);
                }
                if (!await AsyncUtil.DelayFrame(vfx, 1, _cts.Token)) {
                    return;
                }
            }
            foreach (var prop in floatProperties) {
                vfx.SetFloat(prop.name, prop.playValue);
            }
        }

        async UniTaskVoid BlendStop(VisualEffect vfx) {
            float duration = 0;
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            while (duration < blendDuration) {
                duration += Time.deltaTime;
                float t = duration / blendDuration;
                foreach (var prop in floatProperties) {
                    float value = Mathf.Lerp(prop.playValue, prop.stopValue, t);
                    vfx.SetFloat(prop.name, value);
                }
                if (!await AsyncUtil.DelayFrame(vfx, 1, _cts.Token)) {
                    return;
                }
            }
            foreach (var prop in floatProperties) {
                vfx.SetFloat(prop.name, prop.stopValue);
            }
        }

        [Serializable]
        public struct FloatProperty {
            public string name;
            public float playValue;
            public float stopValue;
        }
    }
}