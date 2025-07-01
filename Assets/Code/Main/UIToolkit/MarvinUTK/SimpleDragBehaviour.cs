using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public class SimpleDragBehaviour {
        public bool Enabled {
            [UnityEngine.Scripting.Preserve] get => _enabled;
            set => SetDragState(value);
        }
        
        readonly VisualElement _target;
        readonly VisualElement _draggable;
        
        bool _enabled;
        bool _resetPosOnDisable;
        bool _isDragging;
        Vector2 _startPosition;
        Vector2 _dragOffset;
        
        public SimpleDragBehaviour(VisualElement target, VisualElement draggable, bool resetPosOnDisable = false, bool enabled = true) {
            _target = target;
            _draggable = draggable;
            _resetPosOnDisable = resetPosOnDisable;
            Enabled = enabled;
        }
        
        public void ResetPosition() {
            _target.transform.position = Vector3.zero;
        }
        
        void SetDragState(bool state) {
            _enabled = state;

            if (_enabled) {
                RegisterCallbacks();
            } else {
                if (_resetPosOnDisable) {
                    ResetPosition();
                }
                UnregisterCallbacks();
            }
        }

        void StartDrag(MouseDownEvent evt) {
            _isDragging = true;
            _dragOffset = evt.mousePosition;
            _startPosition = _target.transform.position;
            _draggable.CaptureMouse();
        }

        void Drag(MouseMoveEvent evt) {
            if (_isDragging == false) return;

            Vector2 mousePosition = evt.mousePosition;
            Vector2 newPosition = new(_startPosition.x + mousePosition.x - _dragOffset.x, _startPosition.y + mousePosition.y - _dragOffset.y);
            _target.SetPosition(newPosition);
        }

        void EndDrag(MouseUpEvent evt) {
            _isDragging = false;
            _draggable.ReleaseMouse();
        }
        
        void RegisterCallbacks() {
            _draggable.RegisterCallback<MouseDownEvent>(StartDrag);
            _draggable.RegisterCallback<MouseMoveEvent>(Drag);
            _draggable.RegisterCallback<MouseUpEvent>(EndDrag);
        }
        
        void UnregisterCallbacks() {
            _draggable.UnregisterCallback<MouseDownEvent>(StartDrag);
            _draggable.UnregisterCallback<MouseMoveEvent>(Drag);
            _draggable.UnregisterCallback<MouseUpEvent>(EndDrag);
        }
    }
}