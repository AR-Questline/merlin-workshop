using Awaken.Kandra.Managers;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        bool _expandedIndices;
        Vector2 _indicesScrollPosition;
        OnDemandCache<uint, bool> _expandedIndicesCache = new OnDemandCache<uint, bool>(static _ => false);

        void IndicesDebug() {
            _expandedIndices = TGGUILayout.Foldout(_expandedIndices, "Indices:");
            if (!_expandedIndices) {
                return;
            }

            var meshBrokerAccess = MeshBroker.EditorAccess.Get();
            var freeMemoryRegions = new MemoryBookkeeper.EditorAccess(meshBrokerAccess.IndicesMemory).FreeMemoryRegions;

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();

            GUILayout.Label("Free memory:");
            GUILayout.BeginHorizontal();
            foreach (var region in freeMemoryRegions) {
                GUILayout.Label(region.ToString());
            }
            GUILayout.EndHorizontal();

            _indicesScrollPosition = GUILayout.BeginScrollView(_indicesScrollPosition, GUILayout.ExpandHeight(false));

            DrawOriginalMeshes(meshBrokerAccess);
            DrawCulledMeshes(meshBrokerAccess);

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void DrawOriginalMeshes(MeshBroker.EditorAccess meshBrokerAccess) {
            var originalMeshes = meshBrokerAccess.OriginalMeshes;
            GUILayout.Label("Original meshes:");
            foreach (var (hash, data) in originalMeshes) {
                var mesh = Resources.InstanceIDToObject(hash);

                GUILayout.BeginHorizontal();

                var uHash = unchecked((uint)hash);
                var isExpanded = _expandedIndicesCache[uHash];
                isExpanded = TGGUILayout.Foldout(isExpanded, "");

#if UNITY_EDITOR
                UnityEditor.EditorGUILayout.ObjectField(mesh, typeof(Mesh), false, GUILayout.Width(200));
#else
                GUILayout.Label(mesh ? mesh.name : "null", GUILayout.Width(200));
#endif

                DrawMemory(data.renderingMesh);
                GUILayout.Label(data.indicesMemory.ToString());
                GUILayout.Label($"Ref count:{data.referenceCount}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (isExpanded) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(Indent);
                    GUILayout.Label(data.renderingMesh.ToString());
                    GUILayout.EndHorizontal();
                }
                _expandedIndicesCache[uHash] = isExpanded;
            }
        }

        void DrawCulledMeshes(MeshBroker.EditorAccess meshBrokerAccess) {
            var culledMeshes = meshBrokerAccess.CulledMeshes;
            GUILayout.Label("Culled meshes:");
            foreach (var (hash, data) in culledMeshes) {
                GUILayout.BeginHorizontal();

                var isExpanded = _expandedIndicesCache[hash];
                isExpanded = TGGUILayout.Foldout(isExpanded, hash.ToString());

                DrawMemory(data.renderingMesh);
                GUILayout.Label(data.indicesMemory.ToString());
                GUILayout.Label($"Ref count:{data.referenceCount}");
                GUILayout.EndHorizontal();

                if (isExpanded) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    GUILayout.Label(data.renderingMesh.ToString());
                    GUILayout.EndHorizontal();
                }
                _expandedIndicesCache[hash] = isExpanded;
            }
        }

        static void DrawMemory(in KandraRenderingMesh data) {
            var size = 0L;
            foreach (var submesh in data.submeshes) {
                size += submesh.indexCount * sizeof(ushort);
            }
            GUILayout.Label(M.HumanReadableBytes(size));
        }
    }
}
