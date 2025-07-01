namespace Awaken.TG.Main.Memories {
    public class MemoryVariable<T> {
        public string Name { get; private set; }
        ContextualFacts _facts;
        T _defaultValue;

        [UnityEngine.Scripting.Preserve]
        public T Value {
            get => Get;
            set => Set(value);
        }
        public T Get => _facts.Get(Name, _defaultValue);
        public void Set(T value) => _facts.Set(Name, value);

        public MemoryVariable(ContextualFacts facts, string name, T defaultValue = default) {
            Name = name;
            _facts = facts;
            _defaultValue = defaultValue;
        }

        public static implicit operator T(MemoryVariable<T> variable) => variable.Get;
    }
}