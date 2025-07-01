using System;

namespace Sirenix.OdinInspector
{
    public class MinValueAttribute : Attribute
    {
        public double MinValue;
        public string Expression;

        public MinValueAttribute(double minValue) { }
        public MinValueAttribute(string expression) { }
    }
}