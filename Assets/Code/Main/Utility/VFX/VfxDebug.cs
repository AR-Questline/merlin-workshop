using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.VFX {
    public class VfxDebug : MonoBehaviour {
        public VisualEffect effect;
        public VfxData[] allEffects = Array.Empty<VfxData>();
        [UnityEngine.Scripting.Preserve] public VfxData[] culledEffects = Array.Empty<VfxData>();
        [UnityEngine.Scripting.Preserve]  public VfxData[] pausedEffects = Array.Empty<VfxData>();
        [UnityEngine.Scripting.Preserve] public VfxData[] disabledEffects = Array.Empty<VfxData>();

        [ShowInInspector] bool Culled => effect.culled;
        [ShowInInspector] float PlayRate => effect.playRate;
        [ShowInInspector] bool Paused => effect.pause;
        [ShowInInspector] int AliveParticles => effect.aliveParticleCount;
        [ShowInInspector] bool IsEffectEnabled => effect.enabled;

        void Start() {
#if !UNITY_EDITOR
            Destroy(this);
            return;
#endif
            effect = GetComponent<VisualEffect>();
            allEffects = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None).Select(ve => new VfxData(ve)).ToArray();
            Refresh();
        }

        [Button]
        void Refresh() {
            foreach (var data in allEffects) {
                data.Refresh();
            }
            Array.Sort(allEffects, (a, b) => b.aliveParticles.CompareTo(a.aliveParticles));
            culledEffects = allEffects.Where(d => d.culled).ToArray();
            disabledEffects = allEffects.Where(d => !d.enabled).ToArray();
            pausedEffects = allEffects.Where(d => d.paused).ToArray();
        }
        
        [Serializable]
        public class VfxData {
            public VisualEffect effect;
            public bool culled;
            [UnityEngine.Scripting.Preserve] public float playRate;
            public bool paused;
            public int aliveParticles;
            public bool enabled;
            
            public VfxData(VisualEffect effect) {
                this.effect = effect;
                Refresh();
            }
            
            public void Refresh() {
                culled = effect.culled;
                playRate = effect.playRate;
                paused = effect.pause;
                aliveParticles = effect.aliveParticleCount;
                enabled = effect.enabled;
            }
        }
    }
}
