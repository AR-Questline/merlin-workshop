using System;

namespace Sirenix.OdinInspector
{
    public class MinMaxSliderAttribute : Attribute
    {
        public float MinValue;
        public float MaxValue;
        public string MinValueGetter;
        public string MaxValueGetter;
        public string MinMaxValueGetter;
        public bool ShowFields;

        public string MinMember
        {
          get => this.MinValueGetter;
          set => this.MinValueGetter = value;
        }
        public string MaxMember
        {
          get => this.MaxValueGetter;
          set => this.MaxValueGetter = value;
        }
        public string MinMaxMember
        {
          get => this.MinMaxValueGetter;
          set => this.MinMaxValueGetter = value;
        }
        public MinMaxSliderAttribute(float minValue, float maxValue, bool showFields = false) { }
        public MinMaxSliderAttribute(string minValueGetter, float maxValue, bool showFields = false) { }
        public MinMaxSliderAttribute(float minValue, string maxValueGetter, bool showFields = false) { }
        public MinMaxSliderAttribute(string minValueGetter, string maxValueGetter, bool showFields = false) { }
        public MinMaxSliderAttribute(string minMaxValueGetter, bool showFields = false) { }
    }
}