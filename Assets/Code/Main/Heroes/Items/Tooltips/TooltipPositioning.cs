using System;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    [Serializable]
    public struct TooltipPositioning {
        [SerializeField] Transform anchor;
        [SerializeField] RectTransform aligningParent;
        [SerializeField] ViewportFitting viewportFitting;
        
        public TooltipPositionCache CachePosition() {
            return new TooltipPositionCache(anchor.position, aligningParent.pivot);
        }
        
        public void SetPosition(in TooltipPositionCache cache) {
            anchor.position = cache.anchorPosition;
            aligningParent.pivot = cache.alignmentPivot;
            aligningParent.anchoredPosition3D = Vector3.zero;
        }
        
        public void UpdatePosition(TooltipPosition position, out VerticalMove verticalMoved, out HorizontalMove horizontalMoved) {
            SetPosition(position.Position);
            FitViewport(out verticalMoved, out horizontalMoved);
        }
        public void UpdatePosition(TooltipPosition position) {
            UpdatePosition(position, out _, out _);
        }

        void FitViewport(out VerticalMove verticalMoved, out HorizontalMove horizontalMoved) {
            var viewport = anchor.parent as RectTransform;
            if (viewport == null || !viewportFitting.Enabled) {
                verticalMoved = VerticalMove.None;
                horizontalMoved = HorizontalMove.None;
                return;
            }
            
            viewport.GetWorldCorners2D(out var viewportMin, out var viewportMax);
            aligningParent.GetWorldCorners2D(out var contentMin, out var contentMax);

            Vector3 offset = Vector3.zero;
            bool hasOffset = false;

            if (viewportFitting.moveDown && contentMax.y > viewportMax.y) {
                offset.y = viewportMax.y - contentMax.y;
                hasOffset = true;
                verticalMoved = VerticalMove.Down;
            } else if (viewportFitting.moveUp && contentMin.y < viewportMin.y) {
                offset.y = viewportMin.y - contentMin.y;
                hasOffset = true;
                verticalMoved = VerticalMove.Up;
            } else {
                verticalMoved = VerticalMove.None;
            }
            
            if (viewportFitting.moveRight && contentMin.x < viewportMin.x) {
                offset.x = viewportMin.x - contentMin.x;
                hasOffset = true;
                horizontalMoved = HorizontalMove.Right;
            } else if (viewportFitting.moveLeft && contentMax.x > viewportMax.x) {
                offset.x = viewportMax.x - contentMax.x;
                hasOffset = true;
                horizontalMoved = HorizontalMove.Left;
            } else {
                horizontalMoved = HorizontalMove.None;
            }

            if (hasOffset) {
                anchor.position += offset;
            }
        }

        [Serializable]
        struct ViewportFitting {
            public bool moveUp;
            public bool moveDown;
            public bool moveLeft;
            public bool moveRight;

            public readonly bool Enabled => moveUp || moveDown || moveLeft || moveRight;
        }

        public enum VerticalMove : byte { None, Up, Down, }
        public enum HorizontalMove : byte { None, Right, Left }
    }
}