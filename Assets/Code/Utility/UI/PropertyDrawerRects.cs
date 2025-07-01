using UnityEngine;

namespace Awaken.Utility.UI
{
    /// <summary>
    /// Simplifies allocating space for elements of a property drawer.
    /// </summary>
    public ref struct PropertyDrawerRects {

        // === Fields

        Rect _current;

        public Rect Rect => _current;
        // === Constructors

        public PropertyDrawerRects(Rect initial) {
            _current = initial;
        }

        // === Operations
        public Rect AllocateTop(float height) {
            Rect allocated = new(_current.x, _current.y, _current.width, height);
            _current.y += height;
            _current.height -= height;
            return allocated;
        }

        public Rect AllocateLeft(float width) {
            Rect allocated = new(_current.x, _current.y, width, _current.height);
            _current.x += width;
            _current.width -= width;
            return allocated;
        }

        public Rect AllocateLeftWithPadding(float width, float padding) {
            Rect allocated = new(_current.x + padding, _current.y, width - padding*2, _current.height);
            _current.x += width;
            _current.width -= width;
            return allocated;
        }

        [UnityEngine.Scripting.Preserve]
        public Rect AllocateLeftWithMargin(float width, float margin) {
            Rect allocated = new(_current.x + margin, _current.y, width, _current.height);
            _current.x += width + margin * 2;
            _current.width -= width + margin * 2;
            return allocated;
        }

        public Rect AllocateRight(float width) {
            _current.width -= width;
            Rect allocated = new(_current.x + _current.width, _current.y, width, _current.height);
            return allocated;
        }

        public Rect AllocateBottom(float height) {
            _current.height -= height;
            Rect allocated = new(_current.x, _current.y + _current.height, _current.width, height);
            return allocated;
        }

        public bool TryAllocateLeft(float width, out Rect rect) {
            if (_current.width >= width) {
                rect = AllocateLeft(width);
                return true;
            } else {
                rect = new Rect();
                return false;
            }
        }

        public bool TryAllocateTop(float height, out Rect rect) {
            if (_current.height >= height) {
                rect = AllocateTop(height);
                return true;
            } else {
                rect = new Rect();
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public bool TryAllocateRight(float width, out Rect rect) {
            if (_current.width >= width) {
                rect = AllocateRight(width);
                return true;
            } else {
                rect = new Rect();
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public bool TryAllocateBottom(float height, out Rect rect) {
            if (_current.height >= height) {
                rect = AllocateBottom(height);
                return true;
            } else {
                rect = new Rect();
                return false;
            }
        }

        public Rect AllocateLeftNormalized(float percentage) {
            return AllocateLeft(_current.width*Mathf.Clamp01(percentage));
        }

        public Rect AllocateRightNormalized(float percentage) {
            return AllocateRight(_current.width*Mathf.Clamp01(percentage));
        }
        
        /// <summary>
        /// AllocateLeft so that Rect which rest will have the given width.
        /// </summary>
        public Rect AllocateWithRest(float rest) {
            return AllocateLeft(_current.width-rest);
        }

        public void LeaveSpace(float width) => AllocateLeft(width);

        [UnityEngine.Scripting.Preserve] public void MoveDown(float height) => AllocateTop(height);

        // === Operators

        public static explicit operator Rect(PropertyDrawerRects pdr) => pdr._current;
        public static implicit operator PropertyDrawerRects(Rect rect) => new PropertyDrawerRects(rect);
    }
}
