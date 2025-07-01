using System;
using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace Awaken.TG.Main.Utility.Animations.Blendshapes {
    public abstract class CopyBlendshapes : MonoBehaviour {
        [ShowInInspector, ReadOnly] KandraRenderer _renderer;
        [ShowInInspector, ReadOnly] KandraRenderer _parent;

        UnsafeList<ushort2> _blendshapesMapping;
        
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly] int BlendShapeCount => _blendshapesMapping.Length;
        [ShowInInspector, ReadOnly] List<string> _notFoundBlendshapes = new();
#endif

        protected bool CanProcess => _blendshapesMapping.Length > 0 && _parent != null;
        
        void Awake() {
            _renderer = transform.GetComponent<KandraRenderer>();
            _blendshapesMapping = new UnsafeList<ushort2>(32, Allocator.Persistent);
        }

        void OnDestroy() {
            _blendshapesMapping.Dispose();
        }

        protected void RefreshParent() {
            _parent = GetBlendShapesSource();
        }
        
        protected void RefreshBlendShapesMapping() {
#if UNITY_EDITOR
            _notFoundBlendshapes.Clear();
#endif
            _blendshapesMapping.Clear();
            if (_renderer == null || _parent == null) {
                return;
            }
            // TODO: Analyse if we could only copy not constant blendshapes
            var blendshapesCount = _renderer.BlendshapesCount;
            for (ushort i = 0; i < blendshapesCount; i++) {
                var blendshapeName = _renderer.GetBlendshapeName(i);
                var mappedIndex = _parent.GetBlendshapeIndex(blendshapeName);

                if (mappedIndex != -1) {
                    _blendshapesMapping.Add(new ushort2(i, (ushort)mappedIndex));
                }
#if UNITY_EDITOR
                else {
                    _notFoundBlendshapes.Add(blendshapeName);
                }
#endif
            }
        }

        protected void Process() {
            if (KandraRendererManager.IsInvalidId(_renderer.RenderingId) ||
                KandraRendererManager.Instance.IsCameraVisible(_renderer.RenderingId) == false) {
                return;
            }
#if UNITY_EDITOR
            try {
                List<ushort2> missingBlendshapes = null;
                for (int i = 0; i < _blendshapesMapping.Length; i++) {
                    var mapping = _blendshapesMapping[i];
                    var sourceWeight = _parent.GetBlendshapeWeight(mapping.y);
                    if (!_renderer.SetBlendshapeWeightChecked(mapping.x, sourceWeight)) {
                        missingBlendshapes ??= new List<ushort2>();
                        missingBlendshapes.Add(mapping);
                    }
                }
                if (missingBlendshapes != null) {
                    Debug.LogError($"Missing blendshapes in {_renderer.rendererData.mesh} for {_parent.rendererData.mesh} (instance: {_renderer}):\n[{string.Join(", ", missingBlendshapes)}]", this);
                }
            } catch (Exception e) {
                Debug.LogException(e, this);
            }
#else
            for (int i = 0; i < _blendshapesMapping.Length; i++) {
                var mapping = _blendshapesMapping[i];
                _renderer.SetBlendshapeWeight(mapping.x, _parent.GetBlendshapeWeight(mapping.y));
            }
#endif
        }

        KandraRenderer GetBlendShapesSource() {
            var animator = GetComponentInParent<Animator>();
            if (!animator) {
                return null;
            }

            var blendShapedParent = animator.gameObject.FindChildWithTagRecursively("BlendShapesParent");
            return !blendShapedParent ? null : blendShapedParent.GetComponent<KandraRenderer>();

        }
    }
}