using System;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra.Data {
    public struct PackedBonesWeights {
        public uint2 boneIndices;
        public uint packedWeights;

        public ushort Index0 {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ushort Index1 {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ushort Index2 {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ushort Index3 {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public float Weight0 {
            get => throw new NotImplementedException();
        }

        public float Weight1 {
            get => throw new NotImplementedException();
        }

        public float Weight2 {
            get => throw new NotImplementedException();
        }

        public float Weight3 {
            get => throw new NotImplementedException();
        }

        public PackedBonesWeights(BoneWeight unityBoneWeight) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            throw new NotImplementedException();
        }

        static ushort LoadLowUshort(uint value) {
            throw new NotImplementedException();
        }

        static uint StoreLowUshort(uint packed, ushort value) {
            throw new NotImplementedException();
        }

        static ushort LoadHighUshort(uint value) {
            throw new NotImplementedException();
        }

        static uint StoreHighUshort(uint packed, ushort value) {
            throw new NotImplementedException();
        }
    }
}