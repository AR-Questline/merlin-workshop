namespace Awaken.TG.Main.Memories {
    public class LimitedMemoryVariable {
        
        // === State
        MemoryVariable<float> _current;
        MemoryVariable<float> _max;

        // === Public Accessors
        public float Max {
            get => _max.Get;
            [UnityEngine.Scripting.Preserve] set => _max.Set(value);
        }
        public float Current {
            get => _current.Get;
            [UnityEngine.Scripting.Preserve] set => _current.Set(value);
        }
        public float Available => Max - Current;
        [UnityEngine.Scripting.Preserve] public bool Any => Available > 0;
        
        // === Constructor
        public LimitedMemoryVariable(ContextualFacts facts, string current, string max, float defaultMax) {
            _current = new MemoryVariable<float>(facts, current);
            _max = new MemoryVariable<float>(facts, max, defaultMax);
        }
    }
}