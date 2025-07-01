using System;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Authoring.LinkedEntities {
    // TODO: Change this to producer for global manager which does bulk operations on all changed entities
    [DisallowMultipleComponent]
    public class LinkedEntityLifetime : MonoBehaviour, ILinkedEntityController {
        public LinkedEntitiesAccess linkedEntitiesAccess;

        public static LinkedEntityLifetime GetOrCreate(GameObject gameObject) {
            var sharedAccess = gameObject.GetComponentInParent<SharedLinkedEntitiesLifetime>();
            if (sharedAccess != null && sharedAccess.gameObject != gameObject) {
                return sharedAccess.LinkedEntityLifetime;
            }
            if (gameObject.TryGetComponent<LinkedEntityLifetime>(out var linkedEntityLifetime) == false) {
                linkedEntityLifetime = gameObject.AddComponent<LinkedEntityLifetime>();
                linkedEntityLifetime.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                linkedEntityLifetime.Init();
            }
            return linkedEntityLifetime;
        }

        public void Init() {
            linkedEntitiesAccess = LinkedEntitiesAccess.GetOrCreate(gameObject);
            linkedEntitiesAccess.AddController(this);
            SetEntitiesEnabled(isActiveAndEnabled);
        }

#if UNITY_EDITOR
        async void Reset() {
            if (!Application.isPlaying) {
                await UniTask.NextFrame();
                if (this == null) {
                    return;
                }
                if (hideFlags.HasFlagFast(HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor) == false) {
                    DestroyImmediate(this);
                    throw new Exception("LinkedEntityLifetime is only runtime component, MUST NOT be added in manually");
                }
            }
        }
#endif

        void OnEnable() {
            SetEntitiesEnabled(true);
        }

        void OnDisable() {
            SetEntitiesEnabled(false);
        }

        public void OnAddedEntities(in UnsafeArray<Entity>.Span linkedEntities) {
            var isEnabled = isActiveAndEnabled;
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            foreach (var linkedEntity in linkedEntities) {
                entityManager.SetEnabled(linkedEntity, isEnabled);
            }
        }

        public void OnDestroyUnity(in UnsafeArray<Entity> linkedEntities) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            foreach (var linkedEntity in linkedEntities) {
                if (entityManager.Exists(linkedEntity)) {
                    entityManager.DestroyEntity(linkedEntity);
                }
            }
        }

        void SetEntitiesEnabled(bool state) {
            if (linkedEntitiesAccess?.LinkedEntities.IsCreated != true) {
                return;
            }
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            foreach (var linkedEntity in linkedEntitiesAccess.LinkedEntities) {
                if (entityManager.Exists(linkedEntity)) {
                    entityManager.SetEnabled(linkedEntity, state);
                }
            }
        }
    }
}
