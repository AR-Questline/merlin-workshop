using System;

namespace Unity.Rendering {
    public readonly struct FilterSettingsIndex : IEquatable<FilterSettingsIndex>, IComparable<FilterSettingsIndex> {
        public static readonly FilterSettingsIndex Empty = new FilterSettingsIndex(-1);

        readonly int _filterSettingsIndex;
#if UNITY_EDITOR
        readonly byte _hasEditorRenderData;
        readonly int _editorRenderDataIndex;
#endif

        public FilterSettingsIndex(int filterSettingsIndex) : this() {
            _filterSettingsIndex = filterSettingsIndex;
        }

#if UNITY_EDITOR
        public FilterSettingsIndex(int filterSettingsIndex, int editorRenderDataIndex) {
            _filterSettingsIndex = filterSettingsIndex;
            _hasEditorRenderData = 1;
            _editorRenderDataIndex = editorRenderDataIndex;
        }

        public FilterSettingsIndex WithEditorData(int editorDataIndex) {
            return new FilterSettingsIndex(_filterSettingsIndex, editorDataIndex);
        }
#endif

        public bool Equals(FilterSettingsIndex other) {
            return _filterSettingsIndex == other._filterSettingsIndex
#if UNITY_EDITOR
                   && _editorRenderDataIndex == other._editorRenderDataIndex && _editorRenderDataIndex == other._editorRenderDataIndex
#endif
                ;
        }

        public override bool Equals(object obj) {
            return obj is FilterSettingsIndex other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
#if UNITY_EDITOR
                return (((_filterSettingsIndex * 397) ^ _editorRenderDataIndex) * 397) ^ _hasEditorRenderData;
#else
                    return _filterSettingsIndex;
#endif
            }
        }

        public static bool operator ==(FilterSettingsIndex left, FilterSettingsIndex right) {
            return left.Equals(right);
        }

        public static bool operator !=(FilterSettingsIndex left, FilterSettingsIndex right) {
            return !left.Equals(right);
        }

        public int CompareTo(FilterSettingsIndex other) {
#if UNITY_EDITOR
            int filterSettingsIndexComparison = _filterSettingsIndex.CompareTo(other._filterSettingsIndex);
            if (filterSettingsIndexComparison != 0) {
                return filterSettingsIndexComparison;
            }
            int hasEditorRenderDataComparison = _hasEditorRenderData.CompareTo(other._hasEditorRenderData);
            if (hasEditorRenderDataComparison != 0) {
                return hasEditorRenderDataComparison;
            }
            return _editorRenderDataIndex.CompareTo(other._editorRenderDataIndex);
#else
            return _filterSettingsIndex.CompareTo(other._filterSettingsIndex);
#endif
        }

        public static bool operator <(FilterSettingsIndex left, FilterSettingsIndex right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(FilterSettingsIndex left, FilterSettingsIndex right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(FilterSettingsIndex left, FilterSettingsIndex right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(FilterSettingsIndex left, FilterSettingsIndex right) {
            return left.CompareTo(right) >= 0;
        }
    }
}
