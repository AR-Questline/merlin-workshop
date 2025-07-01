using System;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public class SpringMath {
        public float value;
        public float targetValue;
        public float velocity;
        public float strength;
        public float damping;

        public void Update(float deltaTime) {
            var delta = targetValue - value;
            velocity += delta * strength * deltaTime;
            velocity *= (1f - damping * deltaTime);
            value += velocity * deltaTime;
        }

        public void Set(float v) {
            value = targetValue = v;
            velocity = 0f;
        }
    }
}