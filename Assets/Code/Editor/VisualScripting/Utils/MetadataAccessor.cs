using System;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Utils {
    public readonly struct MetadataAccessor<T> {
        readonly Inspector _inspector;
        readonly string _name;
        readonly Action _onChange;

        public MetadataAccessor(Inspector inspector, string name, Action onChange = null) {
            _inspector = inspector;
            _name = name;
            _onChange = onChange;
        }

        public Metadata Metadata => _inspector.metadata[_name];
        
        public T Get() {
            return (T) Metadata.value;
        }

        public void Set(T value) {
            if (!Equals(Get(), value)) {
                Metadata.RecordUndo();
                Metadata.value = value;
                _onChange?.Invoke();
            }
        }
    }
}