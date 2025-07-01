using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.FootSteps {
    public class UnityTerrainFootstepSource : MonoBehaviour, IFootstepSource {
        Texture2D[] _splatmaps;
        int[] _fmodIndices;

#if UNITY_EDITOR
        void Awake() {
            var map = EditorTextureToFootstepMap.Get;
            var terrain = GetComponent<Terrain>();
            var data = terrain.terrainData;
            _splatmaps = data.alphamapTextures;
            _fmodIndices = ArrayUtils.Select(data.terrainLayers, map.FindFmodParameter);
            if (_splatmaps.Length > 2) {
                Debug.LogError("Terrain has more then 2 splatmaps! This is not allowed!", terrain);
            }
        }
#endif

        public void GetSampleData(RaycastHit hit, out Texture2D[] splatmaps, out int[] fmodIndices, out Vector2 uv) {
            splatmaps = _splatmaps;
            fmodIndices = _fmodIndices;
            uv = hit.textureCoord;
        }
    }
}