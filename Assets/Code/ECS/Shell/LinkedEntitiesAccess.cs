using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Authoring.LinkedEntities {
    public class LinkedEntitiesAccess : MonoBehaviour {
        public ref readonly UnsafeArray<Entity> LinkedEntities => throw new NotImplementedException();

        public static LinkedEntitiesAccess GetOrCreate(GameObject gameObject) {
            throw new NotImplementedException();
        }

        public void Link(in UnsafeArray<Entity>.Span linkedEntities) {
            throw new NotImplementedException();
        }

        public void AddController(ILinkedEntityController controller) {
            throw new NotImplementedException();
        }
    }
}