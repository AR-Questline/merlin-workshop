using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.Kandra.VFXs {
    [AddComponentMenu("VFX/Property Binders/Kandra Mesh Bounds Binder")]
    [VFXBinder("AR/Kandra Renderer Bounds")]
    public class VFXKandraMeshBoundsBinder : VFXBinderBase {
        public KandraMesh kandraMesh;

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
            return kandraMesh && component.HasVector3(_center);
        }

        public override void UpdateBinding(VisualEffect component) {
            if (component.HasVector3(_size)) {
                var bounds = kandraMesh.meshLocalBounds;
                component.SetVector3(_center, bounds.center);
                component.SetVector3(_size, bounds.size);
            }
            if (component.HasFloat(_radius)) {
                var sphere = kandraMesh.localBoundingSphere;
                component.SetVector3(_center, sphere.xyz);
                component.SetFloat(_radius, sphere.w);
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
