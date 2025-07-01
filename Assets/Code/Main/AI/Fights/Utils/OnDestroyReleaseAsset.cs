using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Utils {
    [DisallowMultipleComponent]
    public class OnDestroyReleaseAsset : MonoBehaviour {
        ARAssetReference _assetReference;
        public void Init(ARAssetReference assetReference) {
            _assetReference = assetReference;
        }
        
        void OnDestroy() {
            _assetReference?.ReleaseAsset();
        }
    }
}