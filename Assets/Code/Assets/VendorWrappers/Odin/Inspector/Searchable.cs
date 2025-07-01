using System;

namespace Sirenix.OdinInspector
{
    public class SearchableAttribute : Attribute
    {
        public bool FuzzySearch = true;
        public SearchFilterOptions FilterOptions = SearchFilterOptions.All;
        public bool Recursive = true;
    }
    
    [Flags]
    public enum SearchFilterOptions
    {
        PropertyName = 1,
        PropertyNiceName = 2,
        TypeOfValue = 4,
        ValueToString = 8,
        ISearchFilterableInterface = 16, // 0x00000010
        All = -1, // 0xFFFFFFFF
    }
}