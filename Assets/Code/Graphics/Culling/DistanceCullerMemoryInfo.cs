using System.Runtime.InteropServices;
using Awaken.TG.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.Culling {
    public partial class DistanceCuller {
        public static class DistanceCullerMemoryInfo {
            static readonly uint Float3Size = (uint)Marshal.SizeOf<float3>();
            static readonly uint BoolSize = (uint)Marshal.SizeOf<bool>();
            static readonly uint FloatSize = (uint)Marshal.SizeOf<float>();
            static readonly uint IntSize = (uint)Marshal.SizeOf<int>();
            static readonly uint ToRegisterRendererSize = IntSize+2*Float3Size+IntSize;
            static readonly uint DistanceCullerEntitySize = IntSize;
            static readonly uint BoundsCornersSize = (uint)Marshal.SizeOf<BoundsCorners>();
            static readonly uint DistanceCullerDataSize = (uint)Marshal.SizeOf<DistanceCullerData>();
            
            public static void DrawOnGUI(DistanceCuller culler) {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Distance Culler: {culler.name}");
                GUILayout.Label($"Size: {M.HumanReadableBytes(Size(culler))}");
                GUILayout.EndVertical();
            }

            static ulong Size(DistanceCuller culler) {
                ulong size = 0;
                size += (ulong)(culler.sceneStaticMeshes.Length * ToRegisterRendererSize);
                size += (ulong)(culler.renderersGroups.Length * IntSize);
                foreach (var group in culler.renderersGroups) {
                    size += DistanceCullerGroupSize(group);
                }
                size += (ulong)(culler._renderers.Length * IntSize);
                size += (ulong)(culler._distanceCullerRenderers.Length * IntSize * 2);
                size += DistanceCullerImplSize(culler._renderersCuller);
                size += DistanceCullerImplSize(culler._renderersGroupsCuller);
                return size;
            }

            static ulong DistanceCullerGroupSize(DistanceCullerGroup group) {
                ulong size = 0;
                size += DistanceCullerEntitySize;
                size += (ulong)(group.RenderersCount*IntSize);
                size += BoolSize;
                size += FloatSize;
                size += 2 * Float3Size;
                return size;
            }

            static ulong DistanceCullerImplSize(DistanceCullerImpl impl) {
                ulong size = 0;
                size += (ulong)(impl.Corners.Length * BoundsCornersSize);
                size += (ulong)(impl.CullDistanceIndices.Length * IntSize);
                size += (ulong)(impl.States.Length * DistanceCullerDataSize);
                size += (ulong)(impl.ChangedCapacity * IntSize);
                return size;
            }
        }
    }
}
