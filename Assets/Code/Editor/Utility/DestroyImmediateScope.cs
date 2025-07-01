using System;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public struct DestroyImmediateScope : IDisposable {
        Object _object;
        
        public DestroyImmediateScope(Object obj) {
            _object = obj;
        }

        public void Dispose() {
            if (_object) {
                Object.DestroyImmediate(_object);
            }
        }
    }
    
    public static class DestroyImmediateScopeExtensions {
        public static DestroyImmediateScope DestroyImmediateScope(this Object obj) {
            return new DestroyImmediateScope(obj);
        }
    }
}