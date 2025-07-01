using Awaken.Utility.Profiling;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace Awaken.TG.Editor.Utility.Profiling {
    [ProfilerModuleMetadata("World - MVC")]
    public class WorldProfilerModule : ProfilerModule {
        static readonly ProfilerCounterDescriptor[] Counters = {
            // -- Models
            new(ProfilerValues.ModelsCountName, ProfilerCategory.Scripts),
            new(ProfilerValues.AddedModelsName, ProfilerCategory.Scripts),
            new(ProfilerValues.RemovedModelsName, ProfilerCategory.Scripts),
            // -- Views
            new(ProfilerValues.SpawnedViewsCountName, ProfilerCategory.Scripts),
            new(ProfilerValues.BoundViewsCountName, ProfilerCategory.Scripts),
            new(ProfilerValues.SpawnedViewsName, ProfilerCategory.Scripts),
            new(ProfilerValues.DestroyedViewsName, ProfilerCategory.Scripts),
            new(ProfilerValues.BoundViewsName, ProfilerCategory.Scripts),
            new(ProfilerValues.UnboundViewsName, ProfilerCategory.Scripts),
        };

        public WorldProfilerModule() : base(Counters) {}

        public override ProfilerModuleViewController CreateDetailsViewController() {
            return new WorldProfilerDetailsController(ProfilerWindow);
        }
    }
}
