using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra.Data;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        bool _expandedMeshes;
        OnDemandCache<KandraMesh, bool> _expandedMeshesCache = new OnDemandCache<KandraMesh, bool>(static _ => false);
        Vector2 _meshesScrollPosition;

        void MeshesDebug() {
            _expandedMeshes = TGGUILayout.Foldout(_expandedMeshes, $"Meshes");
            if (!_expandedMeshes) {
                return;
            }

            var registeredRenderers = KandraRendererManager.Instance.ActiveRenderers;
            var meshes = new HashSet<KandraMeshWithMemory>();
            foreach (var renderer in registeredRenderers) {
                if (renderer) {
                    meshes.Add(new(renderer.rendererData.mesh));
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();

            _meshesScrollPosition = GUILayout.BeginScrollView(_meshesScrollPosition, GUILayout.ExpandHeight(false));

            foreach (var kandraMesh in meshes.OrderByDescending(m => m.TotalMemory)) {
                GUILayout.BeginHorizontal();
                var expanded = _expandedMeshesCache[kandraMesh.mesh];

                string arrow = expanded ? "\u25BC" : "\u25B6";
                if (GUILayout.Button($"{arrow} {M.HumanReadableBytes(kandraMesh.TotalMemory)}", GUILayout.Width(120))) {
                    expanded = !expanded;
                }

                _expandedMeshesCache[kandraMesh.mesh] = expanded;
#if UNITY_EDITOR
                UnityEditor.EditorGUILayout.ObjectField(kandraMesh.mesh, typeof(KandraMesh), false, GUILayout.Width(350));
#else
                GUILayout.Label(kandraMesh.mesh.name, GUILayout.Width(350));
#endif
                GUILayout.EndHorizontal();

                if (!expanded) {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(Indent);
                GUILayout.BeginVertical();

                GUILayout.Label($"Vertices count: {kandraMesh.verticesCount}; Bindposes count: {kandraMesh.bindPosesCount}; Indices count: {kandraMesh.indicesCount}");
                GUILayout.Label($"Vertices: {M.HumanReadableBytes(kandraMesh.verticesMemory)}");
                GUILayout.Label($"Additional data: {M.HumanReadableBytes(kandraMesh.additionalDataMemory)}");
                GUILayout.Label($"Bone weights: {M.HumanReadableBytes(kandraMesh.boneWeightsMemory)}");
                GUILayout.Label($"Bindposes: {M.HumanReadableBytes(kandraMesh.bindPosesMemory)}");
                GUILayout.Label($"Indices: {M.HumanReadableBytes(kandraMesh.indicesMemory)}");
                GUILayout.Label("Renderers using this mesh:");
                GUILayout.BeginVertical();
                var renderers = KandraRendererManager.Instance.ActiveRenderers;
                foreach (var renderer in renderers) {
                    if (renderer && renderer.rendererData.mesh == kandraMesh.mesh) {
#if UNITY_EDITOR
                        UnityEditor.EditorGUILayout.ObjectField(renderer, typeof(KandraRenderer), false, GUILayout.Width(450));
#else
                        GUILayout.Label(renderer.name, GUILayout.Width(450));
#endif
                    }
                }
                GUILayout.EndVertical();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }

    readonly unsafe struct KandraMeshWithMemory : IEquatable<KandraMeshWithMemory> {
        public readonly KandraMesh mesh;
        public readonly ushort verticesCount;
        public readonly ushort bindPosesCount;

        public readonly uint indicesCount;

        public readonly long verticesMemory;
        public readonly long additionalDataMemory;
        public readonly long boneWeightsMemory;
        public readonly long bindPosesMemory;
        public readonly long indicesMemory;

        public long TotalMemory => verticesMemory + additionalDataMemory + boneWeightsMemory + bindPosesMemory + indicesMemory;

        public KandraMeshWithMemory(KandraMesh mesh) {
            this.mesh = mesh;
            verticesCount = mesh.vertexCount;
            bindPosesCount = mesh.bindposesCount;
            indicesCount = mesh.indicesCount;

            verticesMemory = verticesCount * sizeof(CompressedVertex);
            additionalDataMemory = verticesCount * sizeof(AdditionalVertexData);
            boneWeightsMemory = verticesCount * sizeof(PackedBonesWeights);
            bindPosesMemory = bindPosesCount * sizeof(float3x4);
            indicesMemory = indicesCount * sizeof(ushort);
        }

        public bool Equals(KandraMeshWithMemory other) {
            return Equals(mesh, other.mesh);
        }
        public override bool Equals(object obj) {
            return obj is KandraMeshWithMemory other && Equals(other);
        }
        public override int GetHashCode() {
            return (mesh != null ? mesh.GetHashCode() : 0);
        }
        public static bool operator ==(KandraMeshWithMemory left, KandraMeshWithMemory right) {
            return left.Equals(right);
        }
        public static bool operator !=(KandraMeshWithMemory left, KandraMeshWithMemory right) {
            return !left.Equals(right);
        }
    }
}