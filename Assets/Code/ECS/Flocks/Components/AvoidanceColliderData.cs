using System;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    public struct AvoidanceColliderData : IComponentData {
        public LayerMask mask;
        public float radius;
        public float vectorLenghtOnRightAxis;
        public float vectorLenghtOnForwardAxis;
        public AvoidanceColliderData(LayerMask mask, float radius, float vectorLenghtOnRightAxis, float vectorLenghtOnForwardAxis) {
            this.mask = mask;
            this.radius = radius;
            this.vectorLenghtOnRightAxis = vectorLenghtOnRightAxis;
            this.vectorLenghtOnForwardAxis = vectorLenghtOnForwardAxis;
        }
    }
}