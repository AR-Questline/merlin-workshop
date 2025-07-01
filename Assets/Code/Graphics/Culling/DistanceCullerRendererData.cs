using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.Culling {
    public struct DistanceCullerRendererData {
        public Renderer Renderer;
        VisualEffect _vfx;
        bool _hasVFX;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnabled(bool state) {
            Renderer.enabled = state;
            if (_hasVFX) {
                _vfx.enabled = state;
            }
        }
        
        public static DistanceCullerRendererData Create(Renderer renderer) {
            var vfx = renderer.GetComponent<VisualEffect>();
            return new DistanceCullerRendererData() {
                Renderer = renderer,
                _vfx = vfx,
                _hasVFX = vfx != null
            };
        }
    }
}
