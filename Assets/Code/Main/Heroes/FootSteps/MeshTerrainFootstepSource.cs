using System;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.FootSteps {
    public class MeshTerrainFootstepSource : MonoBehaviour, IFootstepSource {
        [SerializeField] int[] fmodParameters = Array.Empty<int>();
        [SerializeField] Texture2D[] splatmaps = Array.Empty<Texture2D>();
        [SerializeField] float2 chunkStart;
        [SerializeField] float2 chunkEnd;

        public void GetSampleData(RaycastHit hit, out Texture2D[] splatmaps, out int[] fmodIndices, out Vector2 uv) {
            splatmaps = this.splatmaps;
            fmodIndices = fmodParameters;
            uv = math.unlerp(chunkStart, chunkEnd, hit.point.xz());
        }

        public struct EditorAccessor {
            MeshTerrainFootstepSource _source;
            
            public EditorAccessor(MeshTerrainFootstepSource source) {
                _source = source;
            }

            public ref int[] FmodParameters => ref _source.fmodParameters;
            public ref Texture2D[] Splatmaps => ref _source.splatmaps;
            public ref float2 ChunkStart => ref _source.chunkStart;
            public ref float2 ChunkEnd => ref _source.chunkEnd;
        }
    }
}