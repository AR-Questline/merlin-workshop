using System;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra.Data {
    [Serializable]
    public struct PackedBonesWeights {
        const uint LowMask = 0x0000_FFFF;
        const uint HighMask = 0xFFFF_0000;

        public uint2 boneIndices;
        public uint packedWeights;

        public ushort Index0 {
            get => LoadLowUshort(boneIndices.x);
            set => boneIndices.x = StoreLowUshort(boneIndices.x, value);
        }

        public ushort Index1 {
            get => LoadHighUshort(boneIndices.x);
            set => boneIndices.x = StoreHighUshort(boneIndices.x, value);
        }

        public ushort Index2 {
            get => LoadLowUshort(boneIndices.y);
            set => boneIndices.y = StoreLowUshort(boneIndices.y, value);
        }

        public ushort Index3 {
            get => LoadHighUshort(boneIndices.y);
            set => boneIndices.y = StoreHighUshort(boneIndices.y, value);
        }

        public float Weight0 {
            get => ((packedWeights >> 0) & 0xFF) / (float)byte.MaxValue;
        }

        public float Weight1 {
            get => ((packedWeights >> 8) & 0xFF) / (float)byte.MaxValue;
        }

        public float Weight2 {
            get => ((packedWeights >> 16) & 0xFF) / (float)byte.MaxValue;
        }

        public float Weight3 {
            get => ((packedWeights >> 24) & 0xFF) / (float)byte.MaxValue;
        }

        public PackedBonesWeights(BoneWeight unityBoneWeight) {
            boneIndices = default;
            packedWeights = default;

            var weight3 = (byte)(byte.MaxValue * unityBoneWeight.weight3);
            var weight2 = (byte)(byte.MaxValue * unityBoneWeight.weight2);
            var weight1 = (byte)(byte.MaxValue * unityBoneWeight.weight1);
            var weight0 = byte.MaxValue - (weight1 + weight2 + weight3);

            boneIndices.x = (uint)(unityBoneWeight.boneIndex0 | (unityBoneWeight.boneIndex1 << 16));
            boneIndices.y = (uint)(unityBoneWeight.boneIndex2 | (unityBoneWeight.boneIndex3 << 16));

            packedWeights = (uint)(weight0 | (weight1 << 8) | (weight2 << 16) | (weight3 << 24));
        }

        public override string ToString() {
            return $"({Index0}[{Weight0:P1}], {Index1}[{Weight1:P1}], {Index2}[{Weight2:P1}], {Index3}[{Weight3:P1}])";
        }

        static ushort LoadLowUshort(uint value) {
            return (ushort)(value & LowMask);
        }

        static uint StoreLowUshort(uint packed, ushort value) {
            return (packed & HighMask) | value;
        }

        static ushort LoadHighUshort(uint value) {
            return (ushort)(value >> 16);
        }

        static uint StoreHighUshort(uint packed, ushort value) {
            return (packed & LowMask) | (uint)(value << 16);
        }
    }
}