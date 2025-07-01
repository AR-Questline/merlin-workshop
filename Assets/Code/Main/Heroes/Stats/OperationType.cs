using System;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Stats {
    public class OperationType : RichEnum {
        public Func<float, float, float> Calculate { get; private set; }
        public readonly TweakPriority priority;
            
        public static readonly OperationType
            Add = new OperationType(nameof(Add), (a,b) => a+b, TweakPriority.Add),
            Multi = new OperationType(nameof(Multi), (a,b) => a*b, TweakPriority.Multiply),
            Override = new OperationType(nameof(Override), (a,b) => b, TweakPriority.Override),
            AddPreMultiply = new OperationType(nameof(AddPreMultiply), (a,b) => a+b, TweakPriority.AddPreMultiply);

        OperationType(string enumName, Func<float, float, float> operation, TweakPriority priority) : base(enumName) {
            Calculate = operation;
            this.priority = priority;
        }
        
        public static OperationType GetDefaultOperationTypeFor(TweakPriority priority) {
            return priority switch {
                TweakPriority.PreSet => Override,
                TweakPriority.AddPreMultiply => AddPreMultiply,
                TweakPriority.Multiply => Multi,
                TweakPriority.Add => Add,
                TweakPriority.Override => Override,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            };
        }
    }
}