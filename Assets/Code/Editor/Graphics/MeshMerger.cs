using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Graphics {
    public class MeshMerger : ScriptableWizard {
        [SerializeField] Transform _mergeRoot;

        OnDemandCache<Material, List<CombineInstance>> _combineDataPerMaterial = new(static _ => new());

        void OnWizardCreate() {
            var rootWorldToLocal = _mergeRoot.worldToLocalMatrix;

            var toProcess = new HashSet<MeshRenderer>(_mergeRoot.GetComponentsInChildren<MeshRenderer>());

            var lods = _mergeRoot.GetComponentsInChildren<LODGroup>();

            var lod0Renderers = new HashSet<MeshRenderer>();
            foreach (var lodGroup in lods) {
                var lodLods = lodGroup.GetLODs();
                lod0Renderers.Clear();
                lod0Renderers.AddRange(lodLods[0].renderers.OfType<MeshRenderer>());
                for (int i = 1; i < lodLods.Length; i++) {
                    LOD lodLod = lodLods[i];
                    foreach (var lodRenderer in lodLod.renderers) {
                        if (lodRenderer is MeshRenderer meshRenderer && !lod0Renderers.Contains(meshRenderer)) {
                            toProcess.Remove(meshRenderer);
                        }
                    }
                }
            }

            foreach (var meshRenderer in toProcess) {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                var transform = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;

                var sharedMesh = meshFilter.sharedMesh;
                var materials = meshRenderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++) {
                    Material material = materials[i];
                    var combineInstance = new CombineInstance {
                        mesh = sharedMesh,
                        subMeshIndex = i,
                        transform = transform,
                    };
                    _combineDataPerMaterial[material].Add(combineInstance);
                }
            }

            var combinedData = new List<(Material, Mesh)>();
            foreach (var (material, instances) in _combineDataPerMaterial) {
                var mesh = new Mesh {
                    indexFormat = IndexFormat.UInt32,
                };
                mesh.CombineMeshes(instances.ToArray());

                combinedData.Add((material, mesh));
            }

            var finalCombineInfo = new CombineInstance[combinedData.Count];
            var finalMaterials = new Material[combinedData.Count];
            for (int i = 0; i < combinedData.Count; i++) {
                var (material, mesh) = combinedData[i];
                finalCombineInfo[i] = new CombineInstance {
                    mesh = mesh,
                    transform = Matrix4x4.identity,
                };
                finalMaterials[i] = material;
            }

            var combinedName = $"{_mergeRoot.name}_Combined";
            var finalMesh = new Mesh {
                name = combinedName,
                indexFormat = IndexFormat.UInt32,
            };
            finalMesh.CombineMeshes(finalCombineInfo, false);
            var go = new GameObject(combinedName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_mergeRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = finalMesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = finalMaterials;
        }

        [MenuItem("TG/Assets/Mesh merger")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard<MeshMerger>("Mesh merger", "Merge");
        }
    }
}
