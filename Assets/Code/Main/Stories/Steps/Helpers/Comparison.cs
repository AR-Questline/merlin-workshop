using System;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public enum Comparison {
        LessThan = -1,
        Equal = 0,
        GreaterThan = 1
    }

    public static class ComparisonUtils {
        [UnityEngine.Scripting.Preserve]
        public static bool Fulfilled<TL, TR>(this Comparison comparison, TL lhs, TR rhs) where TL : IComparable<TR> {
            var result = lhs.CompareTo(rhs);
            return comparison switch {
                Comparison.LessThan => result < 0,
                Comparison.Equal => result == 0,
                Comparison.GreaterThan => result > 0,
                _ => false
            };
        }
    }

    public class ComparisonOperator : RichEnum {
        readonly Func<float, float, bool> _operatorFunc;

        public LocString DisplayName { get; }
        public bool Compare(float value, float reference) => _operatorFunc(value, reference);
        
        protected ComparisonOperator(string enumName, string displayName, Func<float, float, bool> operatorFunc, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            this._operatorFunc = operatorFunc;
            DisplayName = new LocString {ID = displayName};
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly ComparisonOperator
            LessOrEqual = new(nameof(LessOrEqual), LocTerms.LessOrEqual, (value, reference) => value <= reference),
            Equal = new(nameof(Equal), LocTerms.Equal, (value, reference) => Math.Abs(value - reference) < 0.0005),
            GreaterOrEqual = new(nameof(GreaterOrEqual), LocTerms.GreaterOrEqual, (value, reference) => value >= reference);
    }
}