using System;
using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Maths;
using Awaken.Utility.Previews;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public class DrakeMeshRendererPreview : IEquatable<DrakeMeshRendererPreview>, IARRendererPreview, IDisposable {
        readonly DrakeMeshRenderer _drakeMeshRenderer;
        Material[] _previewMaterials = Array.Empty<Material>();
        
        public Material[] Materials => GetMaterials(_drakeMeshRenderer);
        public Mesh Mesh => GetMesh(_drakeMeshRenderer);
        public Bounds WorldBounds => Mesh.bounds.ToWorld(_drakeMeshRenderer.transform);

        public Matrix4x4 Matrix => Matrix4x4.TRS(_drakeMeshRenderer.transform.position,
            _drakeMeshRenderer.transform.rotation, _drakeMeshRenderer.transform.localScale);

        public bool IsValid => _drakeMeshRenderer && Mesh;
        
        public DrakeMeshRendererPreview(DrakeMeshRenderer drakeMeshRenderer) {
            this._drakeMeshRenderer = drakeMeshRenderer;
        }

        public void Dispose() {
            var meshReference = _drakeMeshRenderer.MeshReference;
            if (meshReference?.IsValid() == true) {
                meshReference.ReleaseAsset();
            }
            foreach (var materialReference in _drakeMeshRenderer.MaterialReferences) {
                if (materialReference?.IsValid() == true) {
                    materialReference.ReleaseAsset();
                }
            }
            
            _previewMaterials = Array.Empty<Material>();
        }
        
        static Mesh GetMesh(DrakeMeshRenderer drakeMeshRenderer) {
            if (drakeMeshRenderer.MeshReference == null ||
                string.IsNullOrWhiteSpace(drakeMeshRenderer.MeshReference.AssetGUID)) {
                return null;
            }
            if (drakeMeshRenderer.MeshReference.IsValid() && drakeMeshRenderer.MeshReference.IsDone) {
                return drakeMeshRenderer.MeshReference.OperationHandle.Convert<Mesh>().Result;
            }
            return drakeMeshRenderer.MeshReference.LoadAssetAsync<Mesh>().WaitForCompletion();
        }
        
        Material[] GetMaterials(DrakeMeshRenderer drakeMeshRenderer) {
            if (drakeMeshRenderer.MaterialReferences == null) {
                _previewMaterials = Array.Empty<Material>();
                return _previewMaterials;
            }

            if (_previewMaterials.Length != drakeMeshRenderer.MaterialReferences.Length) {
                _previewMaterials = new Material[drakeMeshRenderer.MaterialReferences.Length];
            }
            _previewMaterials = new Material[drakeMeshRenderer.MaterialReferences.Length];
            for (int index = 0; index < drakeMeshRenderer.MaterialReferences.Length; index++) {
                AssetReference materialReference = drakeMeshRenderer.MaterialReferences[index];
                if (materialReference == null || string.IsNullOrWhiteSpace(materialReference.AssetGUID)) {
                    _previewMaterials[index] = new Material(Shader.Find("Hidden/InternalErrorShader"));
                } else if (materialReference.IsValid() && materialReference.IsDone) {
                    _previewMaterials[index] = materialReference.OperationHandle.Convert<Material>().Result;
                } else {
                    _previewMaterials[index] = materialReference.LoadAssetAsync<Material>().WaitForCompletion();
                }
            }

            return _previewMaterials;
        }

        // === Equality
        public bool Equals(DrakeMeshRendererPreview other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return _drakeMeshRenderer.Equals(other._drakeMeshRenderer);
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((DrakeMeshRendererPreview)obj);
        }
        public override int GetHashCode() {
            return _drakeMeshRenderer.GetHashCode();
        }
        public static bool operator ==(DrakeMeshRendererPreview left, DrakeMeshRendererPreview right) {
            return Equals(left, right);
        }
        public static bool operator !=(DrakeMeshRendererPreview left, DrakeMeshRendererPreview right) {
            return !Equals(left, right);
        }

        // === Preview creator
        [InitializeOnLoadMethod]
        static void RegisterPreview() {
            DrakeLodGroup.PreviewCreator = GetPreviews;
        }

        static IEnumerable<IARRendererPreview> GetPreviews(DrakeLodGroup drakeLodGroup) {
            foreach (var drakeMeshRenderer in drakeLodGroup.Renderers) {
                if ((drakeMeshRenderer.LodMask & 1) == 1) {
                    yield return new DrakeMeshRendererPreview(drakeMeshRenderer);
                }
            }
        }
    }
}
