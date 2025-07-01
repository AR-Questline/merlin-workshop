using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Archers {
    public static class ArcherUtils {
        const float PositionPredictionMaxVelocity = 8;
        public static Vector3 ShotVelocity(in ShotData shotData) {
            Vector3 distance = shotData.to - shotData.from;
            float time = ParabolicShotTime(distance, shotData.projectileSpeed, shotData.highShot);
            if (time < 0) {
                Vector3 direction = (distance.normalized + Vector3.up) * 0.5f;
                return direction.normalized * shotData.projectileSpeed;
            }
            distance += Vector3.ClampMagnitude(shotData.targetVelocity, PositionPredictionMaxVelocity) * time;
            return (distance - 0.5f * time * time * Physics.gravity) / time;
        }

        public static float ParabolicShotTime(Vector3 distance, float velocity, bool highShot) {
            float a = 0.25f * Physics.gravity.sqrMagnitude;
            float b = -(Vector3.Dot(Physics.gravity, distance) + velocity * velocity);
            float c = distance.sqrMagnitude;

            float delta = b * b - 4 * a * c;
            if (delta < 0) return -1;

            float deltaSqrt = Mathf.Sqrt(delta) * (highShot ? 1 : -1);

            float timeSqr = (-b + deltaSqrt) / (2 * a);
            if (timeSqr < 0) return -1;
            
            return Mathf.Sqrt(timeSqr);
        }
    }

    public struct ShotData {
        public Vector3 from;
        public Vector3 to;
        public Vector3 targetVelocity;
        public float projectileSpeed;
        public bool highShot;

        public ShotData(Vector3 from, Vector3 to, Vector3 targetVelocity, float projectileSpeed, bool highShot) {
            this.from = from;
            this.to = to;
            this.targetVelocity = targetVelocity;
            this.projectileSpeed = projectileSpeed;
            this.highShot = highShot;
        }
        public ShotData(Vector3 from, Vector3 to, float projectileSpeed, bool highShot) : this(from, to, Vector3.zero, projectileSpeed, highShot) { }
    }
}