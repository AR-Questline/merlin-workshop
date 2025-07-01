using System;
using System.Buffers;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.Utility.Debugging.MemorySnapshots {
    public static class MemorySnapshotMemoryInfo {
        const int SizeLabelWidth = 60;
        const int PercentageLabelWidth = 40;
        static OnDemandCache<string, bool> s_foldoutCache = new OnDemandCache<string, bool>(static _ => false);

        public static void DrawOnGUI(IMainMemorySnapshotProvider mainMemorySnapshotProvider, bool foldZeros = true) {
            int bufferSize = mainMemorySnapshotProvider.PreallocationSize;
            var memoryBuffer = ArrayPool<MemorySnapshot>.Shared.Rent(bufferSize);

            Memory<MemorySnapshot> childrenMemory = memoryBuffer;
            var target = childrenMemory.Slice(0, 1);
            mainMemorySnapshotProvider.GetMemorySnapshot(childrenMemory.Slice(1), target);

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.ExpandWidth(true));
            GUILayout.Label("Self:", GUILayout.Width(SizeLabelWidth));
            GUILayout.Label("Used:", GUILayout.Width(SizeLabelWidth));
            GUILayout.Label("%", GUILayout.Width(PercentageLabelWidth));
            GUILayout.Label("Total:", GUILayout.Width(SizeLabelWidth));
            GUILayout.Label("Total used:", GUILayout.Width(SizeLabelWidth));
            GUILayout.Label("%", GUILayout.Width(PercentageLabelWidth));
            GUILayout.EndHorizontal();
            DrawMemorySummary(memoryBuffer[0], 0, foldZeros);
            GUILayout.EndVertical();

            ArrayPool<MemorySnapshot>.Shared.Return(memoryBuffer);
        }

        static void DrawMemorySummary(MemorySnapshot memorySnapshot, int indentLevel, bool foldZeros) {
            GUILayout.BeginHorizontal();
            var isExpanded = s_foldoutCache[memorySnapshot.name];
            var hasChildren = !memorySnapshot.children.IsEmpty;
            if (hasChildren && GUILayout.Button(isExpanded ? "\u25be" : "\u25b8", GUILayout.Width(18))) {
                isExpanded = !isExpanded;
                s_foldoutCache[memorySnapshot.name] = isExpanded;
            }

            if (string.IsNullOrEmpty(memorySnapshot.additionalInfo)) {
                GUILayout.Label(memorySnapshot.name, GUILayout.ExpandWidth(true));
            } else {
                GUILayout.Label($"{memorySnapshot.name} {memorySnapshot.additionalInfo}", GUILayout.ExpandWidth(true));
            }
            GUILayout.Label(memorySnapshot.HumanReadableSelfByteSize, GUILayout.Width(SizeLabelWidth));
            GUILayout.Label(memorySnapshot.HumanReadableSelfUsedByteSize, GUILayout.Width(SizeLabelWidth));
            GUILayout.Label(memorySnapshot.SelfUsedPercentage, GUILayout.Width(PercentageLabelWidth));
            GUILayout.Label(memorySnapshot.HumanReadableTotalByteSize, GUILayout.Width(SizeLabelWidth));
            GUILayout.Label(memorySnapshot.HumanReadableTotalUsedByteSize, GUILayout.Width(SizeLabelWidth));
            GUILayout.Label(memorySnapshot.TotalUsedPercentage, GUILayout.Width(PercentageLabelWidth));
            GUILayout.EndHorizontal();

            ++indentLevel;
            if (!isExpanded) {
                return;
            }

            var hasEmpty = false;
            var hasFilled = false;
            foreach (var child in memorySnapshot.children.Span) {
                if (!foldZeros || child.selfByteSize > 0 || child.TotalByteSize > 0) {
                    hasFilled = true;
                    DrawChild(child, indentLevel, foldZeros);
                } else {
                    hasEmpty = true;
                }
            }
            if (hasEmpty) {
                if (hasFilled) {
                    var zerosName = memorySnapshot.name + "_zeros";
                    isExpanded = s_foldoutCache[zerosName];
                    if (GUILayout.Button(isExpanded ? "\u25be" : "\u25b8" + " zeros")) {
                        isExpanded = !isExpanded;
                        s_foldoutCache[zerosName] = isExpanded;
                    }
                    if (isExpanded) {
                        foreach (var child in memorySnapshot.children.Span) {
                            if (!(child.selfByteSize > 0 || child.TotalByteSize > 0)) {
                                DrawChild(child, indentLevel, foldZeros);
                            }
                        }
                    }
                } else {
                    foreach (var child in memorySnapshot.children.Span) {
                        DrawChild(child, indentLevel, foldZeros);
                    }
                }
            }
        }

        static void DrawChild(MemorySnapshot child, int indentLevel, bool foldZeros) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8 * indentLevel);
            GUILayout.BeginVertical();
            DrawMemorySummary(child, indentLevel, foldZeros);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
