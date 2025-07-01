using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Fights {
    public class NoiseStrength : RichEnum {
        public float Value { get; }
        public float TheftValue { get; }
        public float RangeMultiplier { get; }
        protected NoiseStrength(string enumName, float value, float theftValue = 0, float rangeMultiplier = 1) : base(enumName) {
            Value = value;
            TheftValue = theftValue;
            RangeMultiplier = rangeMultiplier;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly NoiseStrength
            Inaudible = new(nameof(Inaudible), 0.08f),
            VeryWeak = new(nameof(VeryWeak), 0.1f),
            Weak = new(nameof(Weak), 0.33f),
            Medium = new(nameof(Medium), 0.66f),
            Strong = new(nameof(Strong), 1f),
            VeryStrong = new(nameof(VeryStrong), 1.5f),
            
            CrouchingMovementLight = new(nameof(CrouchingMovementLight), 0.083333336f, 0.08f, 1),
            CrouchingMovementMedium = new(nameof(CrouchingMovementMedium), 0.33333334f, 0.15f, 1.5f),
            CrouchingMovementHeavy = new(nameof(CrouchingMovementHeavy), 0.8333333f, 0.4f, 2),
            WalkingMovement = new(nameof(WalkingMovement), 0.5f);

        public static implicit operator float(NoiseStrength noiseStrength) => noiseStrength.Value;
        
        public void Deconstruct (out float rangeMultiplier, out float strength, out float theftValue) {
            rangeMultiplier = RangeMultiplier;
            strength = Value;
            theftValue = TheftValue;
        }
    }
}