using System.IO;
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.Utility.Graphics {
    public static class SmallObjectsShadows {
        const float LargeShadowsMaxDistance = 1000;

        static float s_shadowsBias = 1;
        static UnsafeArray<ShadowVolumeDatum> s_shadowVolumeData;

        public static event OnShadowsDataChangedDelegate OnShadowsDataChanged;

        public static string ConfigPath => Path.Combine(Application.streamingAssetsPath, "ShadowsConfig.bin");
        public static float ShadowsBias => s_shadowsBias;

        public static void ChangedShadowsBias(float bias) {
            var oldData = s_shadowVolumeData;

            ReadDataFromFile();

            s_shadowsBias = bias;
            for (var i = 0u; i < s_shadowVolumeData.Length; i++) {
                ref var datum = ref s_shadowVolumeData[i];
                datum.distance *= bias;
            }

            OnShadowsDataChanged?.Invoke(s_shadowVolumeData);

            if (oldData.IsCreated) {
                oldData.Dispose();
            }
        }

        public static ref readonly UnsafeArray<ShadowVolumeDatum> LoadData(OnShadowsDataChangedDelegate onShadowsBiasChanged = null) {
            if (onShadowsBiasChanged != null) {
                OnShadowsDataChanged += onShadowsBiasChanged;
            }

            if (s_shadowVolumeData.IsCreated) {
                return ref s_shadowVolumeData;
            }
            ReadDataFromFile();
            return ref s_shadowVolumeData;
        }

        public static void ReleaseData(OnShadowsDataChangedDelegate onShadowsBiasChanged = null) {
            if (onShadowsBiasChanged != null) {
                OnShadowsDataChanged -= onShadowsBiasChanged;
            }
            if (s_shadowVolumeData.IsCreated) {
                s_shadowVolumeData.Dispose();
            }
        }

        public static bool ShouldDisableShadows(in LODRange lodRange, in AABB worldAABB, in UnsafeArray<ShadowVolumeDatum>.Span shadowVolumeData) {
            var shadowsMaxDistance = ShadowsMaxDistance(worldAABB, shadowVolumeData);
            if (lodRange.MinDist >= shadowsMaxDistance) {
                return true;
            }
            return false;
        }

        static float ShadowsMaxDistance(in AABB worldAABB, in UnsafeArray<ShadowVolumeDatum>.Span shadowVolumeData) {
            var volume = BoundsUtils.VolumeOfAverage(worldAABB.Size);
            for (var i = 0u; i < shadowVolumeData.Length; i++) {
                if (volume < shadowVolumeData[i].maxVolume) {
                    return shadowVolumeData[i].distance;
                }
            }
            return LargeShadowsMaxDistance;
        }

        static void ReadDataFromFile() {
            s_shadowVolumeData = FileRead.ToNewBuffer<ShadowVolumeDatum>(ConfigPath, Allocator.Domain);
        }

        public delegate void OnShadowsDataChangedDelegate(in UnsafeArray<ShadowVolumeDatum> data);

        public struct ShadowVolumeDatum {
            [HorizontalGroup] public float maxVolume;
            [HorizontalGroup] public float distance;
        }
    }
}
