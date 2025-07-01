namespace Awaken.TG.Main.Locations.Deferred {
    public abstract partial class DeferredStepExecution {
        public abstract ushort TypeForSerialization { get; }
        public abstract void Execute();
    }
}
