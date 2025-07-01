using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Assets {
    public static class PrefabUtil {
        public static GameObject InstantiateDisabled(GameObject asset) {
            GameObject instance;
            if (asset.activeSelf) {
                asset.SetActive(false);
                instance = Object.Instantiate(asset);
                asset.SetActive(true);
            } else {
                instance = Object.Instantiate(asset);
            }
            return instance;
        }
        
        public static async UniTask<GameObject> InstantiateAsync(ARAssetReference assetRef, Vector3 position, Quaternion rotation, Transform parent = null) {
            var asset = await assetRef.LoadAsset<GameObject>().ToUniTask();
            return asset != null ? Object.Instantiate(asset, position, rotation, parent) : null;
        } 
    }
}
