using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class NestedScrollRect : ScrollRect {
        public bool routeToParent = true;
        bool _routeDragToParent = false;

        /// <summary>
        /// Do action for all parents
        /// </summary>
        void DoForParents<T>(Action<T> action) where T : IEventSystemHandler {
            Transform parent = transform.parent;
            while (parent != null) {
                foreach (var component in parent.GetComponents<Component>()) {
                    if (component is T)
                        action((T) (IEventSystemHandler) component);
                }

                parent = parent.parent;
            }
        }

        /// <summary>
        /// Always route initialize potential drag event to parents
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData) {
            DoForParents<IInitializePotentialDragHandler>((parent) => { parent.OnInitializePotentialDrag(eventData); });
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// Drag event
        /// </summary>
        public override void OnDrag(PointerEventData eventData) {
            if (_routeDragToParent)
                DoForParents<IDragHandler>((parent) => { parent.OnDrag(eventData); });
            else
                base.OnDrag(eventData);
        }

        /// <summary>
        /// Begin drag event
        /// </summary>
        public override void OnBeginDrag(PointerEventData eventData) {
            if(!horizontal && Math.Abs (eventData.delta.x) > Math.Abs (eventData.delta.y))
                _routeDragToParent = true;
            else if(!vertical && Math.Abs (eventData.delta.x) < Math.Abs (eventData.delta.y))
                _routeDragToParent = true;
            else
                _routeDragToParent = false;
            
            if (_routeDragToParent)
                DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(eventData); });
            else
                base.OnBeginDrag(eventData);
        }

        /// <summary>
        /// End drag event
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData) {
            if (_routeDragToParent)
                DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(eventData); });
            else
                base.OnEndDrag(eventData);
            _routeDragToParent = false;
        }

        public override void OnScroll(PointerEventData data) {
            if (routeToParent) {
                DoForParents<IScrollHandler>(parent => { parent.OnScroll(data);});
            } else {
                base.OnScroll(data);
            }
        }
    }
}
