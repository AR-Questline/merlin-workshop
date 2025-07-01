using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.Culling {
    [DisallowMultipleComponent]
    public class DistanceCullerGroup : DistanceCullerEntity {
        [SerializeField] Renderer[] renderers = Array.Empty<Renderer>();
        [SerializeField] VisualEffect[] effects = Array.Empty<VisualEffect>();
        [SerializeField] bool manualVolume;
        [SerializeField] float volume;
        [SerializeField] float3 min;
        [SerializeField] float3 max;

        public float Volume => volume;
        public float3 Min => min;
        public float3 Max => max;
        public int RenderersCount => renderers.Length;
        public bool ManualVolume => manualVolume;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnabled(bool state, bool outsideMainLoop, bool nullChecks = false) {
            if (nullChecks) {
                for (var i = 0; i < renderers.Length; i++) {
                    if (renderers[i]) {
                        renderers[i].enabled = state;
                    }
                }
                for (int i = 0; i < effects.Length; i++) {
                    if (effects[i]) {
                        effects[i].enabled = state;
                    } else {
                        Debug.DebugBreak();
                    }
                }
            } else {
                for (var i = 0; i < renderers.Length; i++) {
                    renderers[i].enabled = state;
                }
                for (int i = 0; i < effects.Length; i++) {
                    effects[i].enabled = state;
                }
            }
#if DEBUG
            if (!outsideMainLoop) {
                DebugChangePerformed(state);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Renderer renderer) {
            return Array.IndexOf(renderers, renderer) > -1;
        }

        [Button]
        public bool EDITOR_BakeData() {
#if UNITY_EDITOR
            if (!manualVolume) {
                volume = 0;
            }
            min = 0;
            max = 0;

            var boundsRenderers = GetComponentsInChildren<Renderer>()
                .Where(static r => r.gameObject is { hideFlags: HideFlags.None })
                .Where(r => r.GetComponentInParent<DistanceCullerGroup>() == this && !r.TryGetComponent<IRenderingOptimizationSystemTarget>(out _))
                .ToArray();

            renderers = boundsRenderers.Where(static r => r.gameObject.isStatic).ToArray();

            var worldBounds = new Bounds(transform.position, 0.01f.UniformVector3());
            if (boundsRenderers.IsNotNullOrEmpty()) {
                worldBounds = boundsRenderers[0].bounds;
                for (var i = 1; i < boundsRenderers.Length; i++) {
                    worldBounds.Encapsulate(boundsRenderers[i].bounds);
                }
            }

            worldBounds = AddLightsToBounds(worldBounds);

            BakedBounds = worldBounds;
            if (!manualVolume) {
                volume = worldBounds.VolumeOfAverage();
            }
            min = worldBounds.min;
            max = worldBounds.max;

            effects = renderers.Select(static r => r.GetComponent<VisualEffect>()).Where(static ve => ve).ToArray();
            if (!renderers.IsNotEmpty()) {
                DestroyImmediate(this);
                return false;
            }
            return true;
#else
            return false;
#endif
        }

        Bounds AddLightsToBounds(Bounds worldBounds) {
            var lights = GetComponentsInChildren<HDAdditionalLightData>()
                .Where(static r => r.gameObject is { hideFlags: HideFlags.None })
                .Where(r => r.GetComponentInParent<DistanceCullerGroup>() == this)
                .ToArray();
            if (!lights.IsNotNullOrEmpty()) {
                return worldBounds;
            }
            foreach (var lightData in lights) {
                Bounds bounds = LightUtils.CalculateApproximateLightBounds(lightData);
                worldBounds.Encapsulate(bounds);
            }
            return worldBounds;
        }

        // === DEBUG
#if DEBUG
        [FoldoutGroup(DebugFoldout), ShowInInspector] protected override bool RenderingState => renderers.IsNotNullOrEmpty() && renderers[0] != null && renderers[0].enabled;
        
        [FoldoutGroup(DebugFoldout), ShowInInspector] protected override Bounds CurrentBounds {
            get {
                var allRenderers = GetComponentsInChildren<Renderer>()
                    .Where(static r => r.gameObject is { hideFlags: HideFlags.None })
                    .Where(r => r.GetComponentInParent<DistanceCullerGroup>() == this)
                    .ToArray();

                var worldBounds = new Bounds(transform.position, 0.01f.UniformVector3());
                if (allRenderers.IsNotNullOrEmpty()) {
                    worldBounds = allRenderers[0].bounds;
                    for (var i = 1; i < allRenderers.Length; i++) {
                        worldBounds.Encapsulate(allRenderers[i].bounds);
                    }
                }

                worldBounds = AddLightsToBounds(worldBounds);
                return worldBounds;
            }
        }

        [field: FoldoutGroup(DebugFoldout), ShowInInspector, ReadOnly, SerializeField] Bounds BakedBounds { [UnityEngine.Scripting.Preserve] get; set; }
#endif
    }
}
