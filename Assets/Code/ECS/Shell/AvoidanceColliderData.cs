using System;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    public struct AvoidanceColliderData : IComponentData {
        public AvoidanceColliderData(LayerMask mask, float radius, float vectorLenghtOnRightAxis,
            float vectorLenghtOnForwardAxis) {
            throw new NotImplementedException();
        }
    }
}