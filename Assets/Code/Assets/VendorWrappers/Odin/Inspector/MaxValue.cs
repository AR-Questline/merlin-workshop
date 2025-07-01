using System;

namespace Sirenix.OdinInspector
{
    public class MaxValueAttribute : Attribute
    {
        public double MaxValue;
        public string Expression;

        public MaxValueAttribute(double maxValue) { }
        public MaxValueAttribute(string expression) { }
    }
}