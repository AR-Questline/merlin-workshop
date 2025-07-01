using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeReplaceMaterials : MonoBehaviour, IDrakeMeshRendererBakingModificationStep {
        static readonly IWithUnityRepresentation.Options RequiresEntitiesAccess = new IWithUnityRepresentation.Options {
            requiresEntitiesAccess = true
        };

        [InfoBox("This is only for API demonstration purposes. Should not be used in production.", InfoMessageType.Error)]
        public AssetReferenceT<Material>[] materials = Array.Empty<AssetReferenceT<Material>>();

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            drakeMeshRenderer.SetUnityRepresentation(RequiresEntitiesAccess);
        }

        [Button]
        public async UniTaskVoid ReplaceMaterials() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            var linkedEntitiesAccess = GetComponent<LinkedEntitiesAccess>();
            if (linkedEntitiesAccess == null) {
                Log.Critical?.Error($"LinkedEntitiesAccess is not found on {gameObject}", this);
                return;
            }

            var loadingTasks = new AsyncOperationHandle[materials.Length];
            var keys = new string[materials.Length];

            for (var i = 0u; i < materials.Length; i++) {
                keys[i] = (string)materials[i].RuntimeKey;
                loadingTasks[i] = materials[i].LoadAssetAsync<Material>();
            }

            bool anyLoading;
            do {
                anyLoading = false;
                for (var i = 0u; (!anyLoading) & (i < loadingTasks.Length); i++) {
                    if (!loadingTasks[i].IsDone) {
                        anyLoading = true;
                    }
                }
                if (anyLoading) {
                    await UniTask.NextFrame();
                }
            }
            while (anyLoading);

            if (this) {
                DrakeReplaceMaterialsUtils.ReplaceDrakeMaterials(world, linkedEntitiesAccess, keys);
            }

            foreach (var loadingTask in loadingTasks) {
                Addressables.Release(loadingTask);
            }
        }
    }
}
