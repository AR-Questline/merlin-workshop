using System;
using UnityEngine;

namespace Awaken.Utility.UI {
    public class DisableGUIScope : IDisposable {
        bool? _wasEnabled = null;
        
        [UnityEngine.Scripting.Preserve] public DisableGUIScope() : this(true) {}
        
        public DisableGUIScope(bool shouldDisable) {
            if (!shouldDisable) return;
            _wasEnabled = GUI.enabled;
            GUI.enabled = false;
        }
        
        public void Dispose() {
            if (_wasEnabled != null) {
                GUI.enabled = _wasEnabled.Value;
            }
        }
    }
}