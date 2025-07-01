using System;
using UnityEngine;

namespace Awaken.Utility.UI {
    public struct ColorGUIScope : IDisposable {
        Color? _previous;
        
        public ColorGUIScope(Color color) : this(true, color) {}
        
        public ColorGUIScope(bool condition, Color color) {
            if (condition) {
                _previous = GUI.color;
                GUI.color = color;
            } else {
                _previous = null;
            }
        }
        
        public void Dispose() {
            if (_previous != null) {
                GUI.color = _previous.Value;
            }
        }
    }
}