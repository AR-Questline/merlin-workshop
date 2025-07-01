using System;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    [Serializable]
    public struct CritterPathPointData {
        public Vector3 position;
        public uint normalCompressed;
        public float3 Normal => CompressionUtils.DecodeNormalVectorSpheremap(normalCompressed);
        public CritterPathPointData(Vector3 position, Vector3 normal) {
            this.position = position;
            this.normalCompressed = CompressionUtils.EncodeNormalVectorSpheremap(normal);
        }
        
        public CritterPathPointData(Vector3 position, uint normalCompressed) {
            this.position = position;
            this.normalCompressed = normalCompressed;
        }
    }
}