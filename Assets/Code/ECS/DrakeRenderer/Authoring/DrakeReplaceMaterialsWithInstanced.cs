using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeReplaceMaterialsWithInstanced : MonoBehaviour, IDrakeMeshRendererBakingModificationStep {
        static readonly IWithUnityRepresentation.Options RequiresEntitiesAccess = new IWithUnityRepresentation.Options {
            requiresEntitiesAccess = true
        };

        [InfoBox("This is only for API demonstration purposes. Should not be used in production.", InfoMessageType.Error)]
        public Material[] materials = Array.Empty<Material>();
        [ShowInInspector] Material[] _instancedMaterials = Array.Empty<Material>();
        [ShowInInspector] string[] _originalKeys;

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            drakeMeshRenderer.SetUnityRepresentation(RequiresEntitiesAccess);
        }

        [Button]
        public void ReplaceMaterials() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            var linkedEntitiesAccess = GetComponent<LinkedEntitiesAccess>();
            if (linkedEntitiesAccess == null) {
                Log.Critical?.Error($"LinkedEntitiesAccess is not found on {gameObject}", this);
                return;
            }

            _instancedMaterials = new Material[materials.Length];
            for (var i = 0; i < materials.Length; i++) {
                _instancedMaterials[i] = new Material(materials[i]);
            }
            _originalKeys = new string[materials.Length];

            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;

            var entityManager = world.EntityManager;

            var entitiesAccess = linkedEntitiesAccess.LinkedEntities;
            for (var i = 0u; i < entitiesAccess.Length; i++) {
                var entity = entitiesAccess[i];
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                _originalKeys[meshMaterial.submesh] = loadingManager.GetMaterialKey(meshMaterial.materialIndex);
            }

            DrakeReplaceMaterialsUtils.ReplaceDrakeMaterials(world, linkedEntitiesAccess, _instancedMaterials);
        }

        [Button]
        public async UniTaskVoid RestoreMaterials() {
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

            for (var i = 0u; i < materials.Length; i++) {
                loadingTasks[i] = Addressables.LoadAssetAsync<Material>(_originalKeys[i]);
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
                DrakeReplaceMaterialsUtils.ReplaceDrakeMaterials(world, linkedEntitiesAccess, _originalKeys);
            }

            foreach (var loadingTask in loadingTasks) {
                Addressables.Release(loadingTask);
            }

            _originalKeys = Array.Empty<string>();
            DrakeReplaceMaterialsUtils.DestroyDrakeRuntimeMaterials(_instancedMaterials, true);
            _instancedMaterials = Array.Empty<Material>();
        }
    }
}
