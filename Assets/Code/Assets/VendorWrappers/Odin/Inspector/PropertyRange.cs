using System;

namespace Sirenix.OdinInspector
{
    public class PropertyRangeAttribute : Attribute
    {
        public double Min;
        public double Max;
        public string MinGetter;
        public string MaxGetter;

        public string MinMember
        {
          get => this.MinGetter;
          set => this.MinGetter = value;
        }

        public string MaxMember
        {
          get => this.MaxGetter;
          set => this.MaxGetter = value;
        }

        public PropertyRangeAttribute(double min, double max) { }
        public PropertyRangeAttribute(string minGetter, double max) { }
        public PropertyRangeAttribute(double min, string maxGetter) { }
        public PropertyRangeAttribute(string minGetter, string maxGetter) { }
    }
}