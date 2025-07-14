using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// This behaviour ensures that scroll view is scrolled to correct value, when navigating with gamepad.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class SelectionAutoScroll : ViewComponent<IModel> {
        [Range(0.001f, 1)]
        public float roughness = 0.5f;
        public bool allowMouseScroll;

        //useful when some elements are beyond bounds of rect transform and you want to artificially enlarge the size of the parent rect transform
        public Vector2 additionalFakeSize = Vector2.zero;
        
        RectTransform _selectedRect;
        Vector2 _destinedPosition;
        Vector2 _normalizedOffset;
        
        ScrollRect Rect => GetComponent<ScrollRect>();

        protected override void OnAttach() {
            var focus = World.Only<Focus>();
            focus.ListenTo(Focus.Events.FocusChanged, Refresh, this);
            Refresh(new FocusChange{current = focus.Focused, previous = null});
        }

        void Update() {
            if (RewiredHelper.IsGamepad || allowMouseScroll) {
                if (CheckUpdate()) {
                    Rect.normalizedPosition = Vector2.Lerp(Rect.normalizedPosition, _destinedPosition, roughness);
                }
            }
        }

        bool CheckUpdate() {
            bool offsetOutsideOfView = _normalizedOffset.sqrMagnitude > 0.000002f;
            if (offsetOutsideOfView) {
                RecalculatePosition();
                return true;
            }

            return false;
        }

        void Refresh(FocusChange change) {
            FindRectAndRecalculate(change.current).Forget();
        }

        public void Interrupt() {
            // Used to stop selection auto scroll from continuous scrolling towards selected object. Useful when you start scrolling with right analog
            _normalizedOffset = Vector2.zero;
        }

        public async UniTaskVoid FindRectAndRecalculate(Component selected) {
            await UniTask.DelayFrame(2);

            if (selected == null || this == null) return;
            _selectedRect = FindSelectedRect(selected);

            RecalculatePosition();
        }

        void RecalculatePosition() {
            if (_selectedRect == null) {
                _normalizedOffset = Vector2.zero;
                return;
            }
            
            // get border coords of selected Rect and viewport
            Vector4 selectedBorder = WorldBorder(_selectedRect);
            Vector4 viewportBorder = WorldBorder(Rect.viewport);
            selectedBorder += new Vector4(-additionalFakeSize.x, -additionalFakeSize.y, additionalFakeSize.x, additionalFakeSize.y);

            // find out how much selected rect protrude beyond viewport
            Vector2 offset = Vector2.zero;
            // horizontally
            if (selectedBorder.x < viewportBorder.x) {
                offset.x = selectedBorder.x - viewportBorder.x;
            } else if (selectedBorder.z > viewportBorder.z) {
                offset.x = selectedBorder.z - viewportBorder.z;
            }

            // vertically
            if (selectedBorder.w > viewportBorder.w) {
                offset.y = selectedBorder.w - viewportBorder.w;
            } else if (selectedBorder.y < viewportBorder.y) {
                offset.y = selectedBorder.y - viewportBorder.y;
            }

            // normalize offset
            if (Rect.content.rect.width - Rect.viewport.rect.width > 0) {
                _normalizedOffset.x = offset.x / (Rect.content.rect.width - Rect.viewport.rect.width);
            }

            if (Rect.content.rect.height - Rect.viewport.rect.height > 0) {
                _normalizedOffset.y = offset.y / (Rect.content.rect.height - Rect.viewport.rect.height);
            }

            _destinedPosition.x = Mathf.Clamp01(Rect.normalizedPosition.x + _normalizedOffset.x);
            _destinedPosition.y = Mathf.Clamp01(Rect.normalizedPosition.y + _normalizedOffset.y);
        }
        
        public void SnapToComponent(Component selected) {
            _selectedRect = selected.transform as RectTransform;
            RecalculatePosition();
            Rect.normalizedPosition = _destinedPosition;
        }

        RectTransform FindSelectedRect(Component selected) {
            Transform selectedTransform = selected.transform;
            return ScrollRectContains(selectedTransform) ? selectedTransform as RectTransform : null;
        }

        bool ScrollRectContains(Transform t) {
            var parent = t.parent;
            return parent != null && ( parent == transform || ScrollRectContains(parent) );
        }

        /// <summary>
        /// Calculate Vector4 which xy are coords of bottom left-hand corner and zw are coords of top right-hand corner
        /// </summary>
        static Vector4 WorldBorder(RectTransform rectTransform) {
            rectTransform.GetWorldCorners2D(out Vector2 min, out Vector2 max);
            return new Vector4(min.x, min.y, max.x, max.y);
        }
    }
}