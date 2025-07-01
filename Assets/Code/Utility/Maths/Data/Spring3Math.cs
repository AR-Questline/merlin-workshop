using System;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public class Spring3Math {
        public Vector3 value;
        public Vector3 targetValue;
        public Vector3 velocity;
        public float strength;
        public float damping;
        
        public void Update(float deltaTime) {
            var delta = targetValue - value;
            velocity += delta * strength * deltaTime;
            velocity *= (1f - damping * deltaTime);
            value += velocity * deltaTime;
        }

        public void Set(Vector3 v) {
            value = targetValue = v;
            velocity = Vector3.zero;
        }
    }
}