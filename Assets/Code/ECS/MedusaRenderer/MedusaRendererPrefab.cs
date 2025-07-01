using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.ECS.MedusaRenderer {
    [RequireComponent(typeof(LODGroup))]
    [InfoBox("Medusa have offset objects between LodGroup and MeshRenderer", InfoMessageType.Error, nameof(HaveOffsetObjects))]
    public class MedusaRendererPrefab : MonoBehaviour, IRenderingOptimizationSystem, IRenderingOptimizationSystemTarget {
#if UNITY_EDITOR
        // === Validation
        void OnValidate() {
            EnsureStatic();
        }

        void Reset() {
            EnsureStatic();
        }

        void EnsureStatic() {
            if (Application.isPlaying) {
                return;
            }
            var lodGroup = GetComponent<LODGroup>();
            var meshRenderers = lodGroup.GetLODs()
                .SelectMany(l => l.renderers)
                .OfType<MeshRenderer>()
                .ToArray();

            var nonStatics = meshRenderers
                .Where(static mr => !mr.gameObject.isStatic)
                .ToArray();

            if (nonStatics.Length < 1) {
                return;
            }

            var location = string.Empty;
            var parents = new List<Transform>();
            parents.Add(transform);
            while (true) {
                var parent = parents.Last().parent;
                if (parent == null) {
                    break;
                }
                parents.Add(parent);
            }
            parents.Reverse();
            location = this.gameObject.scene.name + "/" + string.Join("/", parents.Select(static t => t.name));
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) {
                location += "\n" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
            }

            var convertToStatic = UnityEditor.EditorUtility.DisplayDialog("Medusa Renderer",
                $"At: {location}\nThe following MeshRenderers are not static:\n{string.Join("\n", nonStatics.Select(static mr => mr.name))}",
                "Convert to static", "Remove medusa renderer");

            if (convertToStatic) {
                foreach (var nonStatic in nonStatics) {
                    var nonStaticGameObject = nonStatic.gameObject;
                    nonStaticGameObject.isStatic = true;
                    UnityEditor.EditorUtility.SetDirty(nonStatic);
                    UnityEditor.EditorUtility.SetDirty(nonStaticGameObject);
                }
            } else {
                DestroyImmediate(this);
            }
        }
#endif

        bool HaveOffsetObjects => GetComponentsInChildren<MeshRenderer>().Any(mr => mr.transform.parent != transform);

        public bool Has(UnityEngine.Renderer renderer) {
            // ReSharper disable once CoVariantArrayConversion
            return GetComponent<LODGroup>().GetLODs().SelectMany(l => l.renderers).Contains(renderer);
        }
    }
}
