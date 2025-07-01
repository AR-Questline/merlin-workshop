using Awaken.TG.Main.Heroes.Combat;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.VisualGraphUtils {
    public class VfxWrapperWithPositionAndTimer {
        [UnityEngine.Scripting.Preserve] public readonly MagicVFXWrapper vfx;
        [UnityEngine.Scripting.Preserve] public readonly Vector3 position;

        [UnityEngine.Scripting.Preserve] public VfxWrapperWithPositionAndTimer() { }
        public float LifeTime { [UsedImplicitly, UnityEngine.Scripting.Preserve] get; private set; }
        
        [UnityEngine.Scripting.Preserve]
        public VfxWrapperWithPositionAndTimer(MagicVFXWrapper vfx, Vector3 position, float lifeTime = 0) {
            this.vfx = vfx;
            this.position = position;
            this.LifeTime = lifeTime;
        }

        [UnityEngine.Scripting.Preserve]
        public void IncreaseLifeTime(float value) {
            LifeTime += value;
        }
    }
}