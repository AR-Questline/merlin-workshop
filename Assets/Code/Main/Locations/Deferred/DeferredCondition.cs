namespace Awaken.TG.Main.Locations.Deferred {
    public abstract partial class DeferredCondition {
        public abstract ushort TypeForSerialization { get; }
        public abstract bool Fulfilled();
    }
}