using Awaken.Kandra;
using Awaken.TG.Main.Character.Features.Config;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Character.Features {
    public class HairController : MonoBehaviour {
        static readonly int SurfaceType = Shader.PropertyToID("_SurfaceType");

        public bool IsBeard { get; private set; }
        KandraRenderer _kandraRenderer;

        void Awake() {
            EnsureInitialized();
        }

        void OnDestroy() {
            EnsureDeinitialized();
        }
        
        public void EnsureInitialized() {
            if (_kandraRenderer) {
                return;
            }
            _kandraRenderer = GetComponentInChildren<KandraRenderer>();
            if (_kandraRenderer) {
                _kandraRenderer.EnsureInitialized();
                if (_kandraRenderer.rendererData.materialsInstancesRefCount == null) {
                    Debug.LogError($"KandraRenderer is not initialized properly {_kandraRenderer}", _kandraRenderer);
                }
                _kandraRenderer.UseInstancedMaterials();
            }

            if (TryGetComponent<MeshCoverSettings>(out var meshCoverSettings)) {
                IsBeard = meshCoverSettings.IsBeard;
            }
        }

        public void EnsureDeinitialized() {
            if (!_kandraRenderer) {
                return;
            }
            _kandraRenderer.UseOriginalMaterials();
            _kandraRenderer = null;
        }

        public void Refresh(bool transparent, bool blendshapes) {
            foreach (var material in _kandraRenderer.rendererData.RenderingMaterials) {
                material.SetFloat(SurfaceType, transparent ? 1 : 0);
                HDMaterial.ValidateMaterial(material);
            }
            _kandraRenderer.MaterialsTransparencyChanged();
        }

        public void SetHairColor(in CharacterHairColor config) {
            foreach (var material in _kandraRenderer.rendererData.RenderingMaterials) {
                config.ApplyTo(material, IsBeard);
            }
        }
    }
}