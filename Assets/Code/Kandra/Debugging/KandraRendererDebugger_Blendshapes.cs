using Awaken.Kandra.Managers;
using Awaken.Utility.LowLevel;
using Awaken.Utility.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        bool _expandedBlendshapes;

        void BlendshapesDebug() {
            _expandedBlendshapes = TGGUILayout.Foldout(_expandedBlendshapes, "Blendshapes:");
            if (!_expandedBlendshapes) {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();

            var blendshapesAccess = BlendshapesManager.EditorAccess.Get();
            var freeMemoryRegions = new MemoryBookkeeper.EditorAccess(blendshapesAccess.BlendshapesMemory).FreeMemoryRegions;

            var capacity = freeMemoryRegions[freeMemoryRegions.Length-1].End;
            var currentReserved = 0u;
            var previousEnd = 0u;

            GUILayout.Label("Free memory:");
            GUILayout.BeginHorizontal();
            foreach (var region in freeMemoryRegions) {
                GUILayout.Label(region.ToString());

                var gap = region.start - previousEnd;
                currentReserved += gap;
                previousEnd = region.End;
            }
            GUILayout.EndHorizontal();

            var indices = blendshapesAccess.Indices;
            var weights = blendshapesAccess.Weights;
            var usedIndices = new UnsafeHashSet<uint>(120, Allocator.Temp);
            for (var i = 0u; i < indices.Length; i++) {
                var subIndices = indices[i];
                if (!subIndices.IsCreated) {
                    continue;
                }
                var subWeights = weights[i];
                for (var j = 0u; j < subIndices.Length; j++) {
                    var weight = subWeights[j];
                    if (math.abs(weight) < 0.0001f) {
                        continue;
                    }
                    usedIndices.Add(subIndices[j]);
                }
            }

            var blendshapes = blendshapesAccess.Blendshapes;
            var usedBlendshapes = new UnsafeList<MemoryBookkeeper.MemoryRegion>(12, Allocator.Temp);
            foreach (var kvp in blendshapes) {
                var subBlendshapes = kvp.Value;
                for (var i = 0u; i < subBlendshapes.Length; i++) {
                    if (usedIndices.Contains(subBlendshapes.blendshapesMemory[i].start)) {
                        usedBlendshapes.Add(subBlendshapes.blendshapesMemory[i]);
                    }
                }
            }
            usedIndices.Dispose();

            var currentInUse = 0u;
            previousEnd = 0u;
            foreach (var region in usedBlendshapes) {
                var gap = region.start - previousEnd;
                currentInUse += gap;
                previousEnd = region.End;
            }
            usedBlendshapes.Dispose();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Capacity:");
            GUILayout.Label("Current reserved:");
            GUILayout.Label("Current in use:");
            GUILayout.Label("Reserved usage:");
            GUILayout.Label("In use usage:");
            GUILayout.Label("Overall overshoot:");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label(capacity.ToString());
            GUILayout.Label(currentReserved.ToString());
            GUILayout.Label(currentInUse.ToString());
            GUILayout.Label($"{(float)currentReserved / capacity:P2}");
            GUILayout.Label($"{(float)currentInUse / capacity:P2}");
            GUILayout.Label($"{1f-((currentReserved-currentInUse) / (float)capacity):P2}");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
