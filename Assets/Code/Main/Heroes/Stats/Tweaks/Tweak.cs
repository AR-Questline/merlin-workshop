using System;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    /// <summary>
    /// Serializable tweak object, managed by TweakSystem.
    /// Tweaks objects selected by Selector with Method of TweakOwner.
    /// Priority is used for sorting tweaks in correct way (i.e. multiplications before additions)
    /// </summary>
    public class Tweak : IComparable<Tweak> {
        public TweakSelector Selector { get; private set; }
        public ITweaker TweakOwner { get; private set; }
        public TweakPriority Priority { get; private set; }
        public OperationType OperationType { get; private set; }

        public Tweak(TweakSelector selector, ITweaker tweakOwner, TweakPriority priority) {
            Selector = selector;
            TweakOwner = tweakOwner;
            Priority = priority;
            OperationType = tweakOwner.OperationType;
        }

        public float Apply(float val) {
            return TweakOwner.TweakFn(val, this);
        }

        public int CompareTo(Tweak other) {
            return Priority.CompareTo(other.Priority);
        }
    }
}