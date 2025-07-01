using System;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Authoring.LinkedEntities {
    public class LinkedEntitiesAccess : MonoBehaviour {
        public ref readonly UnsafeArray<Entity> LinkedEntities => ref _linkedEntities;

        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        UnsafeArray<Entity> _linkedEntities;

        FrugalList<ILinkedEntityController> _controllers = new FrugalList<ILinkedEntityController>();

#if UNITY_EDITOR
        [ShowInInspector] ILinkedEntityController[] Controllers => _controllers.ToArray();
        [ShowInInspector] bool HasLink => _linkedEntities.IsCreated;
        [ShowInInspector, ShowIf(nameof(HasLink))] DebugEntityView[] EntitiesDisplay => _linkedEntities.AsNativeArray().Select(entity => new DebugEntityView(entity)).ToArray();
#endif

        public static LinkedEntitiesAccess GetOrCreate(GameObject gameObject) {
            var sharedAccess = gameObject.GetComponentInParent<SharedLinkedEntitiesLifetime>();
            if (sharedAccess != null && sharedAccess.gameObject != gameObject) {
                return sharedAccess.LinkedEntitiesAccess;
            }
            if (gameObject.TryGetComponent<LinkedEntitiesAccess>(out var linkedEntitiesAccess) == false) {
                linkedEntitiesAccess = gameObject.AddComponent<LinkedEntitiesAccess>();
                linkedEntitiesAccess.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            return linkedEntitiesAccess;
        }

        public void Link(in UnsafeArray<Entity>.Span linkedEntities) {
            if (_linkedEntities.IsCreated) {
                var newEntities = _linkedEntities.Contact(linkedEntities, Allocator.Persistent);
                _linkedEntities.Dispose();
                _linkedEntities = newEntities;
            } else {
                _linkedEntities = linkedEntities.ToUnsafeArray(ARAlloc.Persistent);
            }

            foreach (var controller in _controllers) {
                controller.OnAddedEntities(linkedEntities);
            }
        }

        public void AddController(ILinkedEntityController controller) {
            _controllers.Add(controller);
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
                    throw new Exception("LinkedEntitiesAccess is only runtime component, MUST NOT be added in manually");
                }
            }
        }
#endif

        void OnDestroy() {
            if (!_linkedEntities.IsCreated) {
                return;
            }

            foreach (var controller in _controllers) {
                controller.OnDestroyUnity(_linkedEntities);
            }

            _linkedEntities.Dispose();
        }
    }
}