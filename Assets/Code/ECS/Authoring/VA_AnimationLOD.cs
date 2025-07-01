using Awaken.ECS.DrakeRenderer.Authoring;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    public class VA_AnimationLOD : MonoBehaviour, IDrakeMeshRendererBakingModificationStep, IDrakeLODFinishBakingListener {
        [Min(0)] public int LastLODWithAnimation = 1;
        [Min(1)] public float LocalBoundsScale = 1;
        public Material VAMaterial;
        public Material NoVAMaterial;
        public bool IsNotPlaying => Application.isPlaying == false;
        [Button, ShowIf(nameof(IsNotPlaying))]
        public void SynchronizeNoVAMaterials() {
            var lodGroup = GetComponent<LODGroup>();
            var lods = lodGroup.GetLODs();
            UpdateMaterial(NoVAMaterial,
                VAMaterial.GetTexture("_BaseMap"),
                VAMaterial.GetVector("_TilingAndOffset"),
                VAMaterial.IsKeywordEnabled("USE_NORMALMAP_ON"),
                VAMaterial.GetTexture("_NormalMap"),
                VAMaterial.GetFloat("_NormalStrength"),
                VAMaterial.GetFloat("_Metalness"),
                VAMaterial.GetFloat("_Smoothness"),
                VAMaterial.GetColor("_EmissionColor"));

            for (int i = 0; i < lods.Length; i++) {
                var lodRenderers = lods[i].renderers;
                if (lodRenderers.Length > 1) {
                    Debug.LogError("There should be only 1 mesh renderer per LOD in vertex animation");
                }
                if (lodRenderers.Length == 0) {
                    continue;
                }
                var lodRenderer = lodRenderers[0];
                lodRenderer.sharedMaterial = i > LastLODWithAnimation ? NoVAMaterial : VAMaterial;
            }
        }

        static void UpdateMaterial(Material material, Texture baseMap, Vector4 tilingAndOffset,
            bool useNormalMap, Texture normalMap, float normalStrength, float metalness, float smoothness,
            Color emissionColor) {
            material.SetTexture("_BaseMap", baseMap);
            material.SetVector("_TilingAndOffset", tilingAndOffset);
            material.SetTexture("_NormalMap", normalMap);
            material.SetFloat("_NormalStrength", normalStrength);
            material.SetFloat("_Metalness", metalness);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_EmissionColor", emissionColor);
            if (useNormalMap) {
                material.EnableKeyword("USE_NORMALMAP_ON");
            } else {
                material.DisableKeyword("USE_NORMALMAP_ON");
            }
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            drakeMeshRenderer.EnsureBakingAABBExtents(drakeMeshRenderer.AABB.Extents * LocalBoundsScale);
        }

        public void OnDrakeLodGroupBakingFinished() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            Destroy(this);
        }
    }
}