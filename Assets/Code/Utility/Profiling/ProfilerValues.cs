using Unity.Profiling;

namespace Awaken.Utility.Profiling {
    public static class ProfilerValues {
        // === Profiler names
        // -- Models
        public const string ModelsCountName = "Models count";
        public const string AddedModelsName = "Added models";
        public const string RemovedModelsName = "Removed models";
        // -- Views
        public const string SpawnedViewsCountName = "Spawned views count";
        public const string BoundViewsCountName = "Bound views count";
        public const string SpawnedViewsName = "Spawned views";
        public const string DestroyedViewsName = "Destroyed views";
        public const string BoundViewsName = "Bound views";
        public const string UnboundViewsName = "Unbound views";
        // -- Events
        public const string EventsListenersCountName = "Events listeners count";
        public const string AddedEventsListenersName = "Added events listaners";
        public const string RemovedEventsListenersName = "Removed events listeners";
        public const string EventsCalledName = "Events called";
        public const string EventsSelectorsCalledName = "Events selectors called";
        public const string EventsCallbacksCalledName = "Events callbacks called";
        public const string EventsCallsMaxDepthName = "Events calls max depth";

        // === Profiler counters
        // -- Models
        public static readonly ProfilerCountCounter ModelsCounters =
            new(ModelsCountName, AddedModelsName, RemovedModelsName);
        // -- Views
        public static readonly ProfilerCountCounter SpawnedViewsCounters =
            new(SpawnedViewsCountName, SpawnedViewsName, DestroyedViewsName);
        public static readonly ProfilerCountCounter BoundViewsCounters =
            new(BoundViewsCountName, BoundViewsName, UnboundViewsName);
        // -- Events
        public static readonly ProfilerCountCounter EventsListenersCounters =
            new(EventsListenersCountName, AddedEventsListenersName, RemovedEventsListenersName);
        public static readonly ProfilerCounterValue<int> EventsCalled =
            new(ProfilerCategory.Scripts, EventsCalledName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        public static readonly ProfilerCounterValue<int> EventsSelectorsCalled =
            new(ProfilerCategory.Scripts, EventsSelectorsCalledName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        public static readonly ProfilerCounterValue<int> EventsCallbacksCount =
            new(ProfilerCategory.Scripts, EventsCallbacksCalledName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        public static readonly ProfilerCounterValue<int> EventsCallsMaxDepth =
            new(ProfilerCategory.Scripts, EventsCallsMaxDepthName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static void StartNewSession() {
            ModelsCounters.Clear();
            SpawnedViewsCounters.Clear();
            BoundViewsCounters.Clear();
            EventsListenersCounters.Clear();
        }
    }
}
