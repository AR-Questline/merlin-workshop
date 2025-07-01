namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public interface ICullingDistanceModifier {
        public float ModifierValue { get; }
        public bool AllowMultiplierClamp { get; }
    }
}