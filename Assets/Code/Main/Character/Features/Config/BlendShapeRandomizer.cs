using Awaken.Kandra;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    [RequireComponent(typeof(KandraRenderer))]
    public class BlendShapeRandomizer : MonoBehaviour {
        [SerializeField, InlineEditor] BlendShapeGroupSO config;

        [Button, PropertyOrder(-3)]
        public void Apply() {
            var kandraRenderer = GetComponent<KandraRenderer>();
            var blendShapeCount = kandraRenderer.BlendshapesCount;
            for (ushort i = 0; i < blendShapeCount; i++) {
                if (config.ShouldSkipBlendshape(i)) {
                    continue;
                }
                kandraRenderer.SetBlendshapeWeight(i, 0);
            }

            config.ApplyBlendshapes(kandraRenderer);
        }
        
#if UNITY_EDITOR
        void Awake() {
            if (config == null) {
                return;
            }
            ResetConfigWithValuesFromSkinnedMeshRender();
        }
        
        [Button, GUIColor(1, 0, 0)]
        void ResetConfigWithValuesFromSkinnedMeshRender() {
            var access = new BlendShapeGroupSO.EditorAccess(config);
            access.ResetFromSkinnedMeshRenderer(GetComponent<KandraRenderer>());
        }
#endif
    }
}