using System;
using System.Runtime.InteropServices;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Pathfinding;
using Pathfinding.Collections;
using Pathfinding.Graphs.Navmesh;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public static class AStarMemoryInfo {
        static readonly uint PtrSize = (uint)UIntPtr.Size;
        static readonly uint IntSize = (uint)Marshal.SizeOf<int>();
        static readonly uint Int3Size = (uint)Marshal.SizeOf<Int3>();
        static readonly uint ConnectionSize = (uint)(PtrSize + IntSize + Marshal.SizeOf<byte>());

        static readonly FrameOnDemandCache<NavGraph, (ulong allocated, ulong used)> SizeCache = new(static graph => {
            if (graph is NavmeshBase navmeshBase) {
                return NavmeshBaseSize(navmeshBase);
            }
            return (0, 0);
        }, static () => 120);

        public static void DrawOnGUI(NavGraph graph) {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Graph: {graph.name} - {graph.GetType().Name}:");
            if (graph is NavmeshBase navmeshBase) {
                DrawOnGUI(navmeshBase);
            } else {
                GUILayout.Label($"Cannot calculate memory footprint");
            }
            GUILayout.EndVertical();
        }

        public static void Clear() {
            SizeCache.Clear();
        }

        static void DrawOnGUI(NavmeshBase navmeshBase) {
            var tiles = navmeshBase.GetTiles();
            var (allocated, used) = SizeCache[navmeshBase];
            GUILayout.Label($"Tiles: allocated({M.HumanReadableBytes(allocated)}), used({M.HumanReadableBytes(used)}) (in {tiles.Length})");
        }

        static (ulong allocated, ulong used) NavmeshBaseSize(NavmeshBase navmeshBase) {
            var tiles = navmeshBase.GetTiles();
            ulong allocated = 0;
            ulong used = 0;
            foreach (var tile in tiles) {
                var (tileAllocated, tileUsed) = TileSize(tile);
                allocated += tileAllocated;
                used += tileUsed;
            }
            return (allocated, used);
        }

        static (ulong allocated, ulong used) TileSize(NavmeshTile tile) {
            ulong size = 0;
            size += (ulong)(tile.tris.Length * IntSize);
            size += (ulong)(tile.verts.Length * Int3Size);
            size += (ulong)(tile.vertsInGraphSpace.Length * Int3Size);
            size += (ulong)(4 * IntSize);
            size += (ulong)(tile.nodes.Length * PtrSize); // Pointers size
            foreach (var node in tile.nodes) {
                size += TriangleNodeSize(node);
                
            }
            var (treeAllocated, treeUsed) = BBTree.MemoryInfo.GetSize(tile.bbTree);
            size += 6 * PtrSize;
            return (treeAllocated + size, treeUsed + size);
        }
        
        static ulong TriangleNodeSize(TriangleMeshNode meshNode) {
            ulong size = 0;
            // TriangleMeshNode
            size += 3 * IntSize;
            // MeshNode
            size += PtrSize;
            size += (ulong)(meshNode.connections.Length * ConnectionSize);
            // GraphNode
            size += IntSize;
            size += IntSize;
#if !ASTAR_NO_PENALTY
            size += IntSize;
#endif
            size += Int3Size;
            return size;
        }
    }
}
