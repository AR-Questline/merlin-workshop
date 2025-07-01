using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using System;
using Awaken.TG.Main.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    public class CharacterDefaultClothes : MonoBehaviour {
        [SerializeField, PrefabAssetReference(AddressableGroup.NPCs)] ARAssetReference[] clothes = Array.Empty<ARAssetReference>();

        MeshFeature[] _meshes;

        public UniTask AddTo(BodyFeatures features, bool cacheForFutureRemoval = false) {
            if (_meshes != null) {
                Log.Important?.Error("Adding clothes that was added");
                return UniTask.CompletedTask;
            }

            if (cacheForFutureRemoval) {
                _meshes = new MeshFeature[clothes.Length];
            }

            var tasks = new UniTask[clothes.Length];
            for (int i = 0; i < clothes.Length; i++) {
                var mesh = new MeshFeature(clothes[i]);
                if (cacheForFutureRemoval) {
                    _meshes[i] = mesh;
                }
                tasks[i] = features.AddAdditionalFeatureTask(mesh);
            }
            return UniTask.WhenAll(tasks);
        }

        public void RemoveFrom(BodyFeatures features) {
            if (_meshes == null) {
                Log.Important?.Error("Removing clothes that were not added or cached: " + (gameObject != null ? gameObject.name : "null" + " '" + LogUtils.GetDebugName(features)) + "'", this);
                return;
            }
            
            for (int i = 0; i < _meshes.Length; i++) {
                features.RemoveAdditionalFeature(_meshes[i]);
            }
            _meshes = null;
        }
    }
}