using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Authoring.LinkedEntities {
    public class LinkedEntityLifetime : MonoBehaviour, ILinkedEntityController {
        public LinkedEntitiesAccess linkedEntitiesAccess;

        public static LinkedEntityLifetime GetOrCreate(GameObject gameObject) {
            throw new NotImplementedException();
        }

        public void Init() {
            throw new NotImplementedException();
        }

        public void OnAddedEntities(in UnsafeArray<Entity>.Span linkedEntities) {
            throw new NotImplementedException();
        }

        public void OnDestroyUnity(in UnsafeArray<Entity> linkedEntities) {
            throw new NotImplementedException();
        }
    }
}