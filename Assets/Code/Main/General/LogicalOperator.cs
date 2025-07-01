using System;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.General {
    public class LogicalOperator : RichEnum {
        Func<bool, bool, bool> _operation;

        [UnityEngine.Scripting.Preserve] 
        public static readonly LogicalOperator And = new LogicalOperator( nameof(And), (left, right) => left && right );
        [UnityEngine.Scripting.Preserve] 
        public static readonly LogicalOperator Or = new LogicalOperator( nameof(Or), (left, right) => left || right );
        [UnityEngine.Scripting.Preserve] 
        public static readonly LogicalOperator XOr = new LogicalOperator( nameof(XOr), (left, right) => left != right );
        [UnityEngine.Scripting.Preserve] 
        public static readonly LogicalOperator Implication = new LogicalOperator( nameof(Implication), (left, right) => !left || right );
        [UnityEngine.Scripting.Preserve] 
        public static readonly LogicalOperator Equality = new LogicalOperator( nameof(Equality), (left, right) => left == right );

        protected LogicalOperator(string enumName, Func<bool, bool, bool> operation, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            _operation = operation;
        }

        [UnityEngine.Scripting.Preserve]
        public bool Operate(bool left, bool right) => _operation(left, right);
    }
}