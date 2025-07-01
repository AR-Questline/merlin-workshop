using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.Kandra.VFXs {
    [AddComponentMenu("VFX/Property Binders/Kandra Renderer Bounds Binder")]
    [VFXBinder("AR/Kandra Renderer Bounds")]
    public class VFXKandraRendererBoundsBinder : VFXBinderBase {
        public KandraRenderer kandraRenderer;

        [SerializeField, VFXPropertyBinding("UnityEditor.VFX.AABox", "UnityEditor.VFX.Sphere")]
        protected ExposedProperty _boundsProperty;
        ExposedProperty _center;
        ExposedProperty _size;
        ExposedProperty _radius;

        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)_boundsProperty;
            set {
                _boundsProperty = value;
                UpdateSubProperties();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }

        public override bool IsValid(VisualEffect component) {
            return kandraRenderer &&
                   KandraRendererManager.Instance.IsRegistered(kandraRenderer.RenderingId) &&
                   component.HasVector3(_center);
        }

        public override void UpdateBinding(VisualEffect component) {
            KandraRendererManager.Instance.GetBoundsAndRootBone(kandraRenderer.RenderingId, out var worldBoundingSphere, out var rootBoneMatrix);
            if (component.HasFloat(_radius)) {
                component.SetVector3(_center, worldBoundingSphere.xyz);
                component.SetFloat(_radius, worldBoundingSphere.w);
            }
            if (component.HasVector3(_size)) {
                var bounds = kandraRenderer.rendererData.mesh.meshLocalBounds;
                bounds = bounds.Transform(rootBoneMatrix);
                component.SetVector3(_center, bounds.center);
                component.SetVector3(_size, bounds.size);
            }
        }

        void UpdateSubProperties() {
            var mainProperty = _boundsProperty.ToString();
            _center = mainProperty + "_center";
            _size = mainProperty + "_size";
            _radius = mainProperty + "_radius";
        }
    }
}
