using UnityEngine;

namespace Awaken.TG.Main.General {
    public class PIDController {

        // === Fields

        PIDSettings _settings;

        float _integral;
        float _previousError;

        // === Constructor

        public PIDController(PIDSettings settings) {
            UpdateSettings(settings);
        }

        // === Logic

        public void UpdateSettings(PIDSettings settings) {
            _settings = settings;
        }

        [UnityEngine.Scripting.Preserve]
        public float Update(float current, float target) {
            // current function value
            float error = target - current;

            // update variables
            float dt = Time.deltaTime;
            _integral += error * dt;

            float derivative = (error - _previousError) / dt;
            _previousError = error;

            float integralPart = _settings.integralMod * _integral;
            float derivativePart = _settings.derivativeMod * derivative;
            float propPart = _settings.propMod * error;

            // calculate everything and multiply by deltaTime to make not dependent on frame rate
            return (propPart + integralPart + derivativePart) * dt * 60f;
        }

        [UnityEngine.Scripting.Preserve]
        public void Reset() {
            _integral = 0f;
            _previousError = 0f;
        }

        [System.Serializable]
        public class PIDSettings {
            public float integralMod;
            public float derivativeMod;
            public float propMod;
        }
    }
}