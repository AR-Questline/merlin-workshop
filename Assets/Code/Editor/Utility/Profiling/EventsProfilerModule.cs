using Awaken.Utility.Profiling;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace Awaken.TG.Editor.Utility.Profiling {
    [ProfilerModuleMetadata("Events - MVC")]
    public class EventsProfilerModule : ProfilerModule {
        static readonly ProfilerCounterDescriptor[] Counters = {
            // -- Events
            new(ProfilerValues.EventsListenersCountName, ProfilerCategory.Scripts),
            new(ProfilerValues.AddedEventsListenersName, ProfilerCategory.Scripts),
            new(ProfilerValues.RemovedEventsListenersName, ProfilerCategory.Scripts),
            new(ProfilerValues.EventsCalledName, ProfilerCategory.Scripts),
            new(ProfilerValues.EventsSelectorsCalledName, ProfilerCategory.Scripts),
            new(ProfilerValues.EventsCallbacksCalledName, ProfilerCategory.Scripts),
            new(ProfilerValues.EventsCallsMaxDepthName, ProfilerCategory.Scripts),
        };
    
        public EventsProfilerModule() : base(Counters) {}
        
        public override ProfilerModuleViewController CreateDetailsViewController() {
            return new WorldProfilerDetailsController(ProfilerWindow);
        }
    }
}
