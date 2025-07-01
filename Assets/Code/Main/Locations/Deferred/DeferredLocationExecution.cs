using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Deferred {
    public abstract partial class DeferredLocationExecution {
        public abstract ushort TypeForSerialization { get; }
        public abstract void Execute(Location location);
    }
    
    public abstract partial class DeferredLocationExecutionAllowingWait : DeferredLocationExecution {
        public abstract bool ShouldPerformAndWait { get; }
        public abstract UniTask ExecuteAndWait(Location location, Story api);
    }
}