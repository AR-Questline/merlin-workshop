using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class PotentialDuplicationDetectorWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Potential duplication detector (in scene)", priority = 100)]
        internal static void ShowEditor() {
            var window = EditorWindow.GetWindow<PotentialDuplicationDetectorWindow>();
            window.Show();
        }

        [SerializeField] SameMeshTest sameMesh = SameMeshTest.Default;
        [SerializeField] RendererSimilarityTest rendererSimilarity = RendererSimilarityTest.Default;
        [SerializeField] ColliderTest colliders = ColliderTest.Default;

        [Button]
        void CollectAll() {
            sameMesh.Collect();
            rendererSimilarity.Collect();
            colliders.Collect();
        }
        
        static List<T> GetRendererTestItems<T>(Func<LODGroup, MeshRenderer, T> fromMeshRenderer, Func<DrakeMeshRenderer, T> fromDrakeMeshRenderer) {
            List<T> result = new();
            result.AddRange(FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.GetComponentInParent<LODGroup>() == null)
                .Select(renderer => fromMeshRenderer(null, renderer))
            );
            result.AddRange(FindObjectsByType<LODGroup>(FindObjectsSortMode.None)
                .SelectMany(group => GetFromLODGroup(group, fromMeshRenderer))
            );
            result.AddRange(FindObjectsByType<DrakeMeshRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.GetComponentInParent<DrakeLodGroup>() == null)
                .Select(fromDrakeMeshRenderer)
            );
            result.AddRange(FindObjectsByType<DrakeLodGroup>(FindObjectsSortMode.None)
                .Where(group => group.Renderers.Length > 0)
                .SelectMany(group => {
                    int maxLod = group.Renderers.Max(MaxLodOf);
                    return group.Renderers.Where(renderer => MaxLodOf(renderer) == maxLod);
                })
                .Select(fromDrakeMeshRenderer)
            );
            return result;
        
            static IEnumerable<T> GetFromLODGroup(LODGroup group, Func<LODGroup, MeshRenderer, T> fromMeshRenderer) {
                foreach (var renderer in group.GetLODs()[^1].renderers) {
                    if (renderer is MeshRenderer meshRenderer) {
                        yield return fromMeshRenderer(group, meshRenderer);
                    }
                }
            }
            
            static int MaxLodOf(DrakeMeshRenderer renderer) {
                int mask = renderer.LodMask;
                if ((mask & (1 << 8)) != 0) return 8;
                if ((mask & (1 << 7)) != 0) return 7;
                if ((mask & (1 << 6)) != 0) return 6;
                if ((mask & (1 << 5)) != 0) return 5;
                if ((mask & (1 << 4)) != 0) return 4;
                if ((mask & (1 << 3)) != 0) return 3;
                if ((mask & (1 << 2)) != 0) return 2;
                if ((mask & (1 << 1)) != 0) return 1;
                if ((mask & (1 << 0)) != 0) return 0;
                return -1;
            }
        }
        
        static void CheckBoundsForIntersection(GameObject left, Vector3 leftPosition, Bounds leftBounds, float leftVolume, GameObject right, Vector3 rightPosition, Bounds rightBounds, float rightVolume, float minimalIntersection, HashSet<PotentialDuplication> duplicationTarget) {
            if ((leftPosition - rightPosition).sqrMagnitude > 1) {
                return;
            }
            
            if (!leftBounds.Intersects(rightBounds)) {
                return;
            }

            var intersection = leftBounds.Intersection(rightBounds);
            var intersectionArea = intersection.Volume();

            if ((rightVolume - intersectionArea)/rightVolume < minimalIntersection) {
                duplicationTarget.Add(new(right, left));
            }

            if ((leftVolume - intersectionArea)/leftVolume < minimalIntersection) {
                duplicationTarget.Add(new(left, right));
            }
        }

        [Serializable]
        struct SameMeshTest {
            [SerializeField, ListDrawerSettings(IsReadOnly = true)] List<Result> results;

            public static SameMeshTest Default => new() { };
            
            [Button]
            public void Collect() {
                var items = GetRendererTestItems(static (lodGroup, meshRenderer) => {
                        var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                        return new TestItem(lodGroup?.gameObject ?? meshRenderer.gameObject,
                            meshRenderer.transform,
                            meshFilter ? meshFilter.sharedMesh : null);
                    },
                    drakeRenderer => new TestItem(
                        drakeRenderer.gameObject, 
                        drakeRenderer.transform, 
                        drakeRenderer.MeshReference.LoadAssetAsync<Mesh>().WaitForCompletion()
                    )
                );

                results.Clear();
                
                for (int i = 0; i < items.Count; i++) {
                    var lhs = items[i];
                    if (lhs.duplicated) {
                        continue;
                    }

                    var result = new Result(lhs.mesh);
                    for (int j = i + 1; j < items.Count; j++) {
                        var rhs = items[j];
                        if (Identical(lhs, rhs)) {
                            rhs.duplicated = true;
                            result.Add(rhs.gameObject);
                            items[j] = rhs;
                        }
                    }
                    
                    if (result.IsValid) {
                        lhs.duplicated = true;
                        result.Add(lhs.gameObject);
                        results.Add(result);
                        items[i] = lhs;
                    }
                }
            }

            static bool Identical(in TestItem lhs, in TestItem rhs) {
                return lhs.mesh == rhs.mesh && 
                       lhs.position.EqualsApproximately(rhs.position, 0.05f) &&
                       lhs.rotation.eulerAngles.EqualsApproximately(rhs.rotation.eulerAngles, 1f);
            }
            
            struct TestItem {
                public readonly GameObject gameObject;
                public readonly Vector3 position;
                public readonly Quaternion rotation;
                public readonly Mesh mesh;
                public bool duplicated;

                public TestItem(GameObject go, Transform transform, Mesh mesh) {
                    gameObject = go;
                    transform.GetPositionAndRotation(out position, out rotation);
                    this.mesh = mesh;
                    duplicated = false;
                }
            }
            
            [Serializable]
            struct Result {
                [SerializeField] InspectorReadonly<Mesh> mesh;
                [SerializeField, Space(5f), ListDrawerSettings(IsReadOnly = true)] 
                List<InspectorReadonly<GameObject>> duplicates;

                public readonly bool IsValid => duplicates != null;
                
                public Result(Mesh mesh) {
                    this.mesh = mesh;
                    duplicates = null;
                }
                
                public void Add(GameObject gameObject) {
                    duplicates ??= new List<InspectorReadonly<GameObject>>();
                    duplicates.Add(gameObject);
                }
            }
        }

        [Serializable]
        struct RendererSimilarityTest {
            [ShowInInspector, Range(0f, 1f), Tooltip("Lower value means meshes needs bigger similarity to be taken into account")]
            float _minimalIntersections;

            [ShowInInspector, ListDrawerSettings(ListElementLabelName = "Label"), HideReferenceObjectPicker]
            HashSet<PotentialDuplication> _potentialDuplicates;

            public static RendererSimilarityTest Default => new() {
                _minimalIntersections = 0.1f,
                _potentialDuplicates = new(PotentialDuplication.PotentialDuplicationComparer),
            };

            [Button]
            public void Collect() {
                
                var items = GetRendererTestItems(
                    (lodGroup, meshRenderer) => new TestItem(
                        lodGroup?.gameObject ?? meshRenderer.gameObject, 
                        meshRenderer.transform, 
                        meshRenderer.bounds
                    ),
                    drakeRenderer => new TestItem(
                        drakeRenderer.gameObject, 
                        drakeRenderer.transform, 
                        drakeRenderer.WorldBounds.ToBounds()
                    )
                );
                
                _potentialDuplicates.Clear();
                for (int i = 0; i < items.Count; i++) {
                    for (int j = i + 1; j < items.Count; j++) {
                        CheckBoundsForIntersection(
                            items[i].gameObject, items[i].position, items[i].bounds, items[i].volume, 
                            items[j].gameObject, items[j].position, items[j].bounds, items[j].volume, 
                            _minimalIntersections, _potentialDuplicates
                        );
                    }
                }
            }

            struct TestItem {
                public readonly GameObject gameObject;
                public Vector3 position;
                public readonly Bounds bounds;
                public readonly float volume;

                public TestItem(GameObject gameObject, Transform transform, Bounds bounds) : this() {
                    this.gameObject = gameObject;
                    this.position = transform.position;
                    this.bounds = bounds;
                    volume = bounds.Volume();
                }
            }
        }

        [Serializable]
        struct ColliderTest {
            [ShowInInspector, Range(0f, 1f), Tooltip("Lower value means meshes needs bigger similarity to be taken into account")]
            float _minimalIntersection;
            
            [ShowInInspector, ListDrawerSettings(ListElementLabelName = "Label"), HideReferenceObjectPicker]
            HashSet<PotentialDuplication> _potentialDuplicates;
            
            public static ColliderTest Default => new() {
                _minimalIntersection = 0.1f,
                _potentialDuplicates = new(PotentialDuplication.PotentialDuplicationComparer),
            };
            
            [Button]
            public void Collect() {
                var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
                _potentialDuplicates.Clear();
                var duplicatedMeshColliders = colliders.OfType<MeshCollider>()
                    .GroupBy(mc => mc.gameObject)
                    .Where(g => {
                        var count = g.Count();
                        return count > 1 && count != g.DistinctBy(mc => mc.sharedMesh).Count();
                    })
                    .SelectMany(g => g)
                    .ToArray();
            
                _potentialDuplicates.AddRange(
                    duplicatedMeshColliders.Select(mc => new PotentialDuplication(mc.gameObject, mc.gameObject)));
            
                for (int i = 0; i < colliders.Length; i++) {
                    for (int j = i + 1; j < colliders.Length; j++) {
                        CheckBoundsForIntersection(
                            colliders[i].gameObject, colliders[i].transform.position, colliders[i].bounds, colliders[i].bounds.Volume(),
                            colliders[j].gameObject, colliders[j].transform.position, colliders[j].bounds, colliders[j].bounds.Volume(),
                            _minimalIntersection, _potentialDuplicates
                        );
                    }
                }
            }
        }

        class PotentialDuplication : IEquatable<PotentialDuplication> {
            public readonly GameObject potentialDuplication;
            public readonly GameObject potentialDuplicatedIn;
            
            public PotentialDuplication(GameObject potentialDuplication, GameObject potentialDuplicatedIn) {
                this.potentialDuplication = potentialDuplication;
                this.potentialDuplicatedIn = potentialDuplicatedIn;
            }

            public string Label => $"{potentialDuplication} in {potentialDuplicatedIn}";

            public bool Equals(PotentialDuplication other) {
                if (ReferenceEquals(null, other)) {
                    return false;
                }
                if (ReferenceEquals(this, other)) {
                    return true;
                }
                return Equals(potentialDuplication, other.potentialDuplication) && Equals(potentialDuplicatedIn, other.potentialDuplicatedIn);
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
                return Equals((PotentialDuplication)obj);
            }
            
            public override int GetHashCode() {
                unchecked {
                    return ((potentialDuplication != null ? potentialDuplication.GetHashCode() : 0)*397) ^ (potentialDuplicatedIn != null ? potentialDuplicatedIn.GetHashCode() : 0);
                }
            }
            
            public static bool operator ==(PotentialDuplication left, PotentialDuplication right) {
                return Equals(left, right);
            }
            
            public static bool operator !=(PotentialDuplication left, PotentialDuplication right) {
                return !Equals(left, right);
            }

            public static IEqualityComparer<PotentialDuplication> PotentialDuplicationComparer { get; } = new PotentialDuplicationEqualityComparer();

            sealed class PotentialDuplicationEqualityComparer : IEqualityComparer<PotentialDuplication> {
                public bool Equals(PotentialDuplication x, PotentialDuplication y) {
                    if (ReferenceEquals(x, y)) {
                        return true;
                    }
                    if (ReferenceEquals(x, null)) {
                        return false;
                    }
                    if (ReferenceEquals(y, null)) {
                        return false;
                    }
                    if (x.GetType() != y.GetType()) {
                        return false;
                    }
                    return Equals(x.potentialDuplication, y.potentialDuplication) && Equals(x.potentialDuplicatedIn, y.potentialDuplicatedIn);
                }
                public int GetHashCode(PotentialDuplication obj) {
                    unchecked {
                        return ((obj.potentialDuplication != null ? obj.potentialDuplication.GetHashCode() : 0)*397) ^ (obj.potentialDuplicatedIn != null ? obj.potentialDuplicatedIn.GetHashCode() : 0);
                    }
                }
            }
        }
    }
}
