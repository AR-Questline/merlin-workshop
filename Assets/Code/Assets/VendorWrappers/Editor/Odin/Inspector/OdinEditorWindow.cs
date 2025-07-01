using System.Collections.Generic;
using System.Linq;

namespace Sirenix.OdinInspector.Editor
{
    public class OdinEditorWindow : UnityEditor.EditorWindow
    {
        protected virtual void Initialize() { }
        protected virtual void OnBeginDrawEditors() { }
        protected virtual void OnImGUI() { }
        
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        
        protected virtual IEnumerable<object> GetTargets() { return Enumerable.Empty<object>(); }
        protected virtual void DrawEditors() { }
    }
}