using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.Kandra.VFXs {
    [AddComponentMenu("VFX/Property Binders/Kandra Renderer Binder")]
    [VFXBinder("AR/Kandra Renderer")]
    public class VFXKandraRendererBinder : VFXBinderBase {
        public KandraRenderer kandraRenderer;

        KandraMesh _indicesMesh;
        GraphicsBuffer _indexBuffer;

        [SerializeField, VFXPropertyBinding("Awaken.Kandra.KandraVfxProperty")]
        protected ExposedProperty _property = "KandraRenderer";
        ExposedProperty _vertexStart;
        ExposedProperty _additionalDataStart;
        ExposedProperty _vertexCount;
        ExposedProperty _trianglesCount;
        ExposedProperty _indicesBuffer;

        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)_property;
            set {
                _property = value;
                UpdateSubProperties();
            }
        }

        public virtual bool RequiresStitchingRebind => true;

        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }

        protected override void OnDisable() {
            if (_indexBuffer != null) {
#if UNITY_EDITOR
                if (KandraRendererManager.Instance != null) // As always Unity's editor is a mess :(
#endif
                {
                    KandraRendererManager.Instance.KandraVfxHelper.ReleaseIndexBuffer(_indicesMesh);
                }
            }
            _indexBuffer = null;
            _indicesMesh = null;
            base.OnDisable();
        }

        void OnValidate() {
            UpdateSubProperties();
        }

        void UpdateSubProperties() {
            var mainProperty = _property.ToString();
            _vertexStart = mainProperty + "_vertexStart";
            _additionalDataStart = mainProperty + "_additionalDataStart";
            _vertexCount = mainProperty + "_vertexCount";
            _trianglesCount = mainProperty + "_trianglesCount";
            _indicesBuffer = mainProperty + "_Indices";
        }

        public override bool IsValid(VisualEffect component) {
            return kandraRenderer &&
                   KandraRendererManager.Instance.IsRegistered(kandraRenderer.RenderingId) &&
                   component.HasUInt(_vertexStart) &&
                   component.HasUInt(_additionalDataStart) &&
                   component.HasUInt(_vertexCount) &&
                   component.HasUInt(_trianglesCount);
        }

        public override void UpdateBinding(VisualEffect component) {
            if (!KandraRendererManager.Instance.TryGetInstanceData(kandraRenderer, out var instanceData)) {
                return;
            }
            var vertexCount = kandraRenderer.rendererData.mesh.vertexCount;
            var indicesCounts = kandraRenderer.rendererData.mesh.indicesCount;
            var trianglesCount = indicesCounts / 3;

            component.SetUInt(_vertexStart, instanceData.instanceStartVertex);
            component.SetUInt(_additionalDataStart, instanceData.sharedStartVertex);
            component.SetUInt(_vertexCount, vertexCount);
            component.SetUInt(_trianglesCount, trianglesCount);

            if (component.HasGraphicsBuffer(_indicesBuffer)) {
                if (_indexBuffer == null) {
                    _indicesMesh = kandraRenderer.rendererData.mesh;
                    _indexBuffer = KandraRendererManager.Instance.KandraVfxHelper.GetIndexBuffer(_indicesMesh);
                }
                component.SetGraphicsBuffer(_indicesBuffer, _indexBuffer);
            }
        }

        public override string ToString() {
            var hasIndexBuffer = _indexBuffer != null ? " with index buffer" : "no index buffer";
            var kandraName = kandraRenderer ? kandraRenderer.name : "null";
            return $"Kandra Renderer : '{_property}' -> {kandraName} {hasIndexBuffer}";
        }
    }
}
