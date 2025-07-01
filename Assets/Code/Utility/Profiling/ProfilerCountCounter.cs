using Unity.Profiling;

namespace Awaken.Utility.Profiling {
    public readonly struct ProfilerCountCounter {
        readonly ProfilerCounterValue<int> _count;
        readonly ProfilerCounterValue<int> _add;
        readonly ProfilerCounterValue<int> _remove;

        public ProfilerCountCounter(string countName, string addName, string removeName) {
            _count = new(ProfilerCategory.Scripts, countName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);
            _add = new(ProfilerCategory.Scripts, addName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
            _remove = new(ProfilerCategory.Scripts, removeName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        }

        public void Add() {
            _count.Value += 1;
            _add.Value += 1;
        }

        public void Remove(int count = 1) {
            _count.Value -= count;
            _remove.Value += count;
        }

        public void Clear() {
            _count.Value = 0;
            _add.Value = 0;
            _remove.Value = 0;
        }
    }
}
